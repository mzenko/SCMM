﻿using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using SCMM.Discord.Client.Commands;
using SCMM.Shared.Abstractions.Analytics;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Shared.Data.Models.Statistics;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client.Exceptions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Community.Requests.Json;
using SCMM.Steam.Data.Models.Community.Responses.Json;
using SCMM.Steam.Data.Models.Enums;
using SCMM.Steam.Data.Models.Extensions;
using SCMM.Steam.Data.Store;
using SCMM.Steam.Data.Store.Types;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        [Command("rebuild-market-index-fund-stats")]
        public async Task<RuntimeResult> RebuildMarketIndexFundStats(ulong appId)
        {
            var indexFund = new Dictionary<DateTime, MarketIndexFundStatistic>();
            var appGuid = (await _steamDb.SteamApps.FirstOrDefaultAsync(x => x.SteamId == appId.ToString()))?.Id;
            var start = _steamDb.SteamMarketItemSale.Min(x => x.Timestamp).Date;
            var end = _steamDb.SteamMarketItemSale.Max(x => x.Timestamp).Date;

            try
            {
                var message = await Context.Message.ReplyAsync("Rebuilding market index fund...");
                while (start < end)
                {
                    await message.ModifyAsync(
                        x => x.Content = $"Rebuilding market index fund {start.Date.ToString()}..."
                    );
                    var stats = _steamDb.SteamMarketItemSale
                        .AsNoTracking()
                        .Where(x => x.Item.AppId == appGuid)
                        .Where(x => x.Timestamp >= start && x.Timestamp < start.Date.AddDays(1))
                        .GroupBy(x => x.ItemId)
                        .Select(x => new
                        {
                            TotalSalesVolume = x.Sum(y => y.Quantity),
                            TotalSalesValue = x.Sum(y => y.MedianPrice * y.Quantity),
                            AverageItemValue = x.Average(y => y.MedianPrice)
                        })
                        .ToList()
                        .GroupBy(x => true)
                        .Select(x => new MarketIndexFundStatistic
                        {
                            TotalItems = x.Count(),
                            TotalSalesVolume = x.Sum(y => y.TotalSalesVolume),
                            TotalSalesValue = x.Sum(y => y.TotalSalesValue),
                            AverageItemValue = x.Average(y => y.AverageItemValue)
                        })
                        .FirstOrDefault();

                    if (stats != null)
                    {
                        indexFund[start.Date] = stats;
                    }

                    start = start.AddDays(1);
                }

                await message.ModifyAsync(
                    x => x.Content = $"Rebuilt market index fund"
                );

                return CommandResult.Success();
            }
            finally
            {
                if (indexFund.Any())
                {
                    await _statisticsService.SetDictionaryAsync(
                        String.Format(StatisticKeys.MarketIndexFundByAppId, appId),
                        indexFund
                            .OrderBy(x => x.Key)
                            .ToDictionary(x => x.Key, x => x.Value)
                    );
                }
            }
        }

        [Command("import-market-items")]
        public async Task<RuntimeResult> ImportMarketItems(ulong appId)
        {
            var message = await Context.Message.ReplyAsync("Importing market items...");

            var app = await _steamDb.SteamApps
                .FirstOrDefaultAsync(x => x.SteamId == appId.ToString());

            var usdCurrency = await _steamDb.SteamCurrencies
                .FirstOrDefaultAsync(x => x.Name == Constants.SteamCurrencyUSD);

            var totalCount = 0;
            var paginationStart = 0;
            var paginationSize = SteamMarketSearchPaginatedJsonRequest.MaxPageSize;
            var steamSearchResults = (SteamMarketSearchPaginatedJsonResponse)null;
            do
            {
                await message.ModifyAsync(
                    x => x.Content = $"Importing market items {paginationStart}/{steamSearchResults?.TotalCount.ToString() ?? "???"}..."
                );

                try
                {
                    steamSearchResults = await _steamCommunityClient.GetMarketSearchPaginatedAsync(
                        new SteamMarketSearchPaginatedJsonRequest()
                        {
                            AppId = appId.ToString(),
                            GetDescriptions = true,
                            SortColumn = SteamMarketSearchPaginatedJsonRequest.SortColumnName,
                            Start = paginationStart,
                            Count = paginationSize
                        },
                        useCache: false
                    );

                    // If the request failed or no items were returned, loop back and request the current page again.
                    // NOTE: Steam will sometimes return success, but with no items. If you make the same [page] request again, it then returns then items.
                    //       There must be some kind of caching issue on their end or something...
                    if (steamSearchResults.Success != true || steamSearchResults.TotalCount <= 0 || !(steamSearchResults.Results?.Count > 0))
                    {
                        continue;
                    }

                    // Success, move to the next page for the next request
                    paginationStart += steamSearchResults.Results?.Count ?? 0;
                    totalCount = steamSearchResults.TotalCount;

                    var assetClassIds = steamSearchResults.Results
                        .Select(x => x?.AssetDescription?.ClassId ?? 0)
                        .Where(x => x > 0)
                        .ToArray();

                    var existingAssetClassIds = await _steamDb.SteamAssetDescriptions
                        .Where(x => x.ClassId != null && assetClassIds.Contains(x.ClassId.Value))
                        .Where(x => x.MarketItem != null)
                        .ToArrayAsync();
                    var missingAssetClassIds = assetClassIds
                        .Except(existingAssetClassIds.Select(x => x.ClassId.Value))
                        .ToArray();

                    if (missingAssetClassIds.Any())
                    {
                        // Parse the items we just fetched
                        var importedAssetDescriptions = await _commandProcessor.ProcessWithResultAsync(new ImportSteamAssetDescriptionsRequest()
                        {
                            AppId = appId,
                            AssetClassIds = missingAssetClassIds,
                        });

                        var assetDescriptionIds = importedAssetDescriptions.AssetDescriptions.Select(x => x.Id).ToArray();
                        var dbItems = await _steamDb.SteamMarketItems
                            .Where(x => x.DescriptionId != null && assetDescriptionIds.Contains(x.DescriptionId.Value))
                            .ToListAsync();

                        foreach (var assetDescription in importedAssetDescriptions.AssetDescriptions)
                        {
                            var dbItem = (assetDescription.MarketItem ?? dbItems?.FirstOrDefault(x => x.DescriptionId == assetDescription.Id));
                            if (dbItem == null)
                            {
                                dbItem = assetDescription.MarketItem = new SteamMarketItem()
                                {
                                    SteamId = assetDescription.NameId?.ToString(),
                                    AppId = app.Id,
                                    App = app,
                                    Description = assetDescription,
                                    Currency = usdCurrency,
                                };
                            }

                            var steamItem = steamSearchResults.Results.FirstOrDefault(x => x.AssetDescription.ClassId == assetDescription.ClassId);
                            if (steamItem?.SellPrice > 0)
                            {
                                dbItem.UpdateSteamBuyPrice(steamItem.SellPrice, steamItem.SellListings);
                            }
                        }

                        await _steamDb.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    await Context.Message.ReplyAsync($"Request failed for page {paginationStart}/{steamSearchResults?.TotalCount.ToString() ?? "???"}. {ex.Message}");
                }
                finally
                {
                    paginationStart += steamSearchResults.Results?.Count ?? 0;
                }

            } while (steamSearchResults?.Success == true && paginationStart < totalCount);

            var itemCount = (steamSearchResults?.TotalCount.ToString() ?? "???");
            await message.ModifyAsync(
                x => x.Content = $"Imported market items {itemCount}/{itemCount}"
            );

            return CommandResult.Success();
        }

        [Command("import-market-items-price-history")]
        public async Task<RuntimeResult> ImportMarketItemsPriceHistory(ulong appId)
        {
            var message = await Context.Message.ReplyAsync("Importing market items price history...");

            var app = await _steamDb.SteamApps
                .FirstOrDefaultAsync(x => x.SteamId == appId.ToString());

            var cutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(24));
            var items = _steamDb.SteamMarketItems
                .Include(x => x.App)
                .Include(x => x.Currency)
                .Include(x => x.Description)
                //.Where(x => x.App.IsActive)
                .Where(x => x.LastCheckedSalesOn == null || x.LastCheckedSalesOn <= cutoff)
                .OrderBy(x => x.LastCheckedSalesOn == null)
                .ThenBy(x => x.LastCheckedSalesOn)
                .ToArray();

            if (!items.Any())
            {
                return CommandResult.Success();
            }

            var usdCurrency = _steamDb.SteamCurrencies.FirstOrDefault(x => x.Name == Constants.SteamCurrencyUSD);
            if (usdCurrency == null)
            {
                return CommandResult.Fail("USD currency not found");
            }

            int unsavedBufferCount = 0;
            foreach (var item in items)
            {
                try
                {
                    await message.ModifyAsync(
                        x => x.Content = $"Importing market items price history {Array.IndexOf(items, item)}/{items.Length}..."
                    );

                    var responseHtml = await _steamCommunityClient.GetTextAsync(
                         new SteamMarketListingPageRequest()
                         {
                             AppId = item.App.SteamId,
                             MarketHashName = item.Description.Name,
                         }
                    );

                    var salesHistoryGraphArray = Regex.Match(responseHtml, @"var line1=\[(.*)\];").Groups.OfType<Capture>().LastOrDefault()?.Value;
                    if (!string.IsNullOrEmpty(salesHistoryGraphArray))
                    {
                        var salesHistoryGraph = JsonSerializer.Deserialize<string[][]>($"[{salesHistoryGraphArray}]");
                        await UpdateMarketItemSalesHistory(item, salesHistoryGraph, usdCurrency);
                    }
                }
                catch (SteamRequestException ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    {
                        item.LastCheckedSalesOn = DateTimeOffset.Now;
                        continue;
                    }
                }
                finally
                {
                    if (unsavedBufferCount >= 100)
                    {
                        await _steamDb.SaveChangesAsync();
                        unsavedBufferCount = 0;
                    }
                    else
                    {
                        unsavedBufferCount++;
                    }
                }
            }

            await _steamDb.SaveChangesAsync();

            await message.ModifyAsync(
                x => x.Content = $"Imported market items price history {items.Length}/{items.Length}"
            );

            return CommandResult.Success();
        }

        private async Task<SteamMarketItem> UpdateMarketItemSalesHistory(SteamMarketItem item, string[][] salesGraph, SteamCurrency salesCurrency = null)
        {
            if (item == null || salesGraph == null || salesGraph.Length == 0)
            {
                return item;
            }

            // Lazy-load sales history if missing, required for recalculation
            if (item.SalesHistory?.Any() != true)
            {
                item = await _steamDb.SteamMarketItems
                    .Include(x => x.SalesHistory)
                    .AsSplitQuery()
                    .SingleOrDefaultAsync(x => x.Id == item.Id);
            }

            // If the sales are not already in our items currency, exchange them now
            var itemSales = ParseMarketItemSalesFromGraph(salesGraph, item.LastCheckedSalesOn);
            if (itemSales != null && salesCurrency != null && salesCurrency.Id != item.CurrencyId)
            {
                foreach (var sale in itemSales)
                {
                    sale.MedianPrice = item.Currency.CalculateExchange(sale.MedianPrice, salesCurrency);
                }
            }

            item.LastCheckedSalesOn = DateTimeOffset.Now;
            item.RecalculateSales(itemSales);

            return item;
        }

        private SteamMarketItemSale[] ParseMarketItemSalesFromGraph(string[][] salesGraph, DateTimeOffset? ignoreSalesBefore = null)
        {
            var sales = new List<SteamMarketItemSale>();
            if (salesGraph == null)
            {
                return sales.ToArray();
            }

            var totalQuantity = 0;
            for (var i = 0; i < salesGraph.Length; i++)
            {
                var timeStamp = DateTime.ParseExact(salesGraph[i][0], "MMM dd yyyy HH: z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                var medianPrice = salesGraph[i][1].SteamPriceAsInt();
                var quantity = salesGraph[i][2].SteamQuantityValueAsInt();
                sales.Add(new SteamMarketItemSale()
                {
                    Timestamp = timeStamp,
                    MedianPrice = medianPrice,
                    Quantity = quantity,
                });
                totalQuantity += quantity;
            }

            return sales.ToArray();
        }
    }
}
