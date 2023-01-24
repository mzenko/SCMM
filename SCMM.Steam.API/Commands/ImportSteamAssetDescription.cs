﻿using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCMM.Shared.Abstractions.Messaging;
using SCMM.Shared.API.Messages;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Models;
using SCMM.Steam.Data.Models.Community.Models;
using SCMM.Steam.Data.Models.Community.Requests.Html;
using SCMM.Steam.Data.Models.Store.Requests.Html;
using SCMM.Steam.Data.Models.Store.Requests.Json;
using SCMM.Steam.Data.Models.WebApi.Models;
using SCMM.Steam.Data.Models.WebApi.Requests.IPublishedFileService;
using SCMM.Steam.Data.Store;
using Steam.Models;
using Steam.Models.SteamEconomy;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SCMM.Steam.API.Commands
{
    public class ImportSteamAssetDescriptionRequest : ICommand<ImportSteamAssetDescriptionResponse>
    {
        public ulong AppId { get; set; }

        public ulong AssetClassId { get; set; }

        /// <summary>
        /// Optional, removes the need to lookup AssetClassId if supplied
        /// </summary>
        public SteamAssetClass AssetClass { get; set; }

        /// <summary>
        /// If true, any additional or missing item info will be looked up. If false, only the supplied info will be used.
        /// </summary>
        public bool LookupAdditionalItemInfo { get; set; } = true;
    }

    public class ImportSteamAssetDescriptionResponse
    {
        /// <remarks>
        /// If asset does not exist, this will be null
        /// </remarks>
        public SteamAssetDescription AssetDescription { get; set; }
    }

    public class ImportSteamAssetDescription : ICommandHandler<ImportSteamAssetDescriptionRequest, ImportSteamAssetDescriptionResponse>
    {
        private readonly ILogger<ImportSteamAssetDescription> _logger;
        private readonly SteamDbContext _db;
        private readonly SteamConfiguration _cfg;
        private readonly SteamWebApiClient _apiClient;
        private readonly ProxiedSteamCommunityWebClient _communityClient;
        private readonly ProxiedSteamStoreWebClient _storeClient;
        private readonly IServiceBus _serviceBus;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;

        public ImportSteamAssetDescription(ILogger<ImportSteamAssetDescription> logger, SteamDbContext db, IConfiguration cfg, SteamWebApiClient apiClient, ProxiedSteamCommunityWebClient communityClient, ProxiedSteamStoreWebClient storeClient, IServiceBus serviceBus, ICommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _logger = logger;
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _apiClient = apiClient;
            _communityClient = communityClient;
            _storeClient = storeClient;
            _serviceBus = serviceBus;
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        public async Task<ImportSteamAssetDescriptionResponse> HandleAsync(ImportSteamAssetDescriptionRequest request)
        {
            var steamWebInterfaceFactory = new SteamWebInterfaceFactory(_cfg.ApplicationKey);

            // Get asset class info
            var assetClass = (AssetClassInfoModel)null;
            if (request.AssetClass == null && request.AssetClassId > 0 && request.LookupAdditionalItemInfo)
            {
                // We need to fetch it from Steam...
                var steamEconomy = steamWebInterfaceFactory.CreateSteamWebInterface<SteamEconomy>();
                var assetClassInfoResponse = await steamEconomy.GetAssetClassInfoAsync(
                    (uint)request.AppId,
                    new List<ulong>()
                    {
                    request.AssetClassId
                    }
                );
                if (assetClassInfoResponse?.Data?.Success != true)
                {
                    throw new Exception($"Failed to get class info for asset {request.AssetClassId}, request failed");
                }
                assetClass = assetClassInfoResponse.Data.AssetClasses.FirstOrDefault(x => x.ClassId == request.AssetClassId);
            }
            else
            {
                // It has already been fetched, map it to the description model...
                assetClass = new AssetClassInfoModel()
                {
                    IconUrl = request.AssetClass.IconUrl,
                    IconUrlLarge = request.AssetClass.IconUrlLarge,
                    Name = request.AssetClass.Name,
                    MarketHashName = request.AssetClass.MarketHashName,
                    MarketName = request.AssetClass.MarketName,
                    NameColor = request.AssetClass.NameColor,
                    BackgroundColor = request.AssetClass.BackgroundColor,
                    Type = request.AssetClass.Type,
                    Tradable = request.AssetClass.Tradable,
                    Marketable = request.AssetClass.Marketable,
                    Commodity = request.AssetClass.Commodity,
                    MarketTradableRestriction = request.AssetClass.MarketTradableRestriction,
                    MarketMarketableRestriction = request.AssetClass.MarketMarketableRestriction,
                    Descriptions = new ReadOnlyCollection<AssetClassDescriptionModel>(
                        new List<AssetClassDescriptionModel>(
                            request.AssetClass.Descriptions?.Select(x => new AssetClassDescriptionModel()
                            {
                                Type = x.Type,
                                Value = x.Value,
                                Color = x.Type
                            }) ?? Enumerable.Empty<AssetClassDescriptionModel>()
                        )
                    ),
                    Actions = new ReadOnlyCollection<AssetClassActionModel>(
                        new List<AssetClassActionModel>(
                            request.AssetClass.Actions?.Select(x => new AssetClassActionModel()
                            {
                                Link = x.Link,
                                Name = x.Name
                            }) ?? Enumerable.Empty<AssetClassActionModel>()
                        )
                    ),
                    Tags = new ReadOnlyCollection<AssetClassTagModel>(
                        new List<AssetClassTagModel>(
                            request.AssetClass.Tags?.Select(x => new AssetClassTagModel()
                            {
                                Category = x.Category,
                                InternalName = x.InternalName,
                                CategoryName = x.LocalizedCategoryName,
                                Name = x.LocalizedTagName
                            }) ?? Enumerable.Empty<AssetClassTagModel>()
                        )
                    ),
                    ClassId = request.AssetClass.ClassId
                };
            }
            if (assetClass == null)
            {
                throw new Exception($"Failed to get class info for asset {request.AssetClassId}, asset was not found");
            }

            // Does this asset already exist?
            var assetDescription = (await _db.SteamAssetDescriptions.Include(x => x.App).FirstOrDefaultAsync(x => x.ClassId == assetClass.ClassId)) ??
                                   (_db.SteamAssetDescriptions.Local.FirstOrDefault(x => x.ClassId == assetClass.ClassId));
            if (assetDescription == null)
            {
                // Does a similiarly named item already exist?
                assetDescription = await _db.SteamAssetDescriptions.FirstOrDefaultAsync(x =>
                    x.App.SteamId == request.AppId.ToString() &&
                    x.ClassId == null &&
                    x.Name == assetClass.Name
                );
                if (assetDescription == null)
                {
                    // Doesn't exist in database, create it now...
                    _db.SteamAssetDescriptions.Add(assetDescription = new SteamAssetDescription()
                    {
                        App = await _db.SteamApps.FirstOrDefaultAsync(x => x.SteamId == request.AppId.ToString()),
                        ClassId = assetClass.ClassId,
                    });
                }
            }

            // Get published file details from Steam (if workshopfileid is available)
            var publishedFile = (PublishedFileDetailsModel)null;
            var publishedFileVotes = (PublishedFileVoteData)null;
            var publishedFilePreviews = (IEnumerable<PublishedFilePreview>)null;
            var publishedFileChangeNotesPageHtml = (XElement)null;
            var publishedFileId = (ulong)0;
            var publishedFileHasChanged = false;
            var viewWorkshopAction = assetClass.Actions?.FirstOrDefault(x =>
                string.Equals(x.Name, Constants.SteamActionViewWorkshopItemId, StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(x.Name, Constants.SteamActionViewWorkshopItem, StringComparison.InvariantCultureIgnoreCase)
            );
            if (viewWorkshopAction != null)
            {
                var workshopFileIdGroups = Regex.Match(viewWorkshopAction.Link, Constants.SteamActionViewWorkshopItemRegex).Groups;
                publishedFileId = (workshopFileIdGroups.Count > 1) ? ulong.Parse(workshopFileIdGroups[1].Value) : 0;
            }
            if (publishedFileId > 0 && request.LookupAdditionalItemInfo)
            {
                // Get file details
                var steamRemoteStorage = steamWebInterfaceFactory.CreateSteamWebInterface<SteamRemoteStorage>();
                var publishedFileDetails = await steamRemoteStorage.GetPublishedFileDetailsAsync(publishedFileId);
                if (publishedFileDetails?.Data == null)
                {
                    throw new Exception($"Failed to get workshop file {publishedFileId} for asset {request.AssetClassId}, response was empty");
                }

                publishedFile = publishedFileDetails.Data;
                publishedFileHasChanged = (assetDescription.TimeUpdated == null || assetDescription.TimeUpdated < publishedFile.TimeUpdated);

                // Get file vote data (if missing or item is not yet accepted, votes don't change once accepted)
                if ((assetDescription.VotesDown == null || assetDescription.VotesUp == null || !assetDescription.IsAccepted) && !string.IsNullOrEmpty(publishedFile.Title))
                {
                    // NOTE: We have to do two seperate calls to "QueryFiles" as for some strange reason Steam only returns vote counts if requested in isolation
                    var queryVoteData = await _apiClient.PublishedFileServiceQueryFiles(new QueryFilesJsonRequest()
                    {
                        QueryType = QueryFilesJsonRequest.QueryTypeRankedByTextSearch,
                        SearchText = publishedFile.Title,
                        AppId = publishedFile.ConsumerAppId,
                        Page = 0,
                        NumPerPage = 3,
                        ReturnVoteData = true
                    });

                    publishedFileVotes = queryVoteData?.PublishedFileDetails?.FirstOrDefault(x => x.PublishedFileId == publishedFile.PublishedFileId)?.VoteData;
                }

                // Get file previews (if missing or changed since our last check)
                if ((publishedFileHasChanged || !assetDescription.Previews.Any()) && !string.IsNullOrEmpty(publishedFile.Title))
                {
                    // NOTE: We have to do two seperate calls to "QueryFiles" as for some strange reason Steam only returns vote counts if requested in isolation
                    var queryPreviews = await _apiClient.PublishedFileServiceQueryFiles(new QueryFilesJsonRequest()
                    {
                        QueryType = QueryFilesJsonRequest.QueryTypeRankedByTextSearch,
                        SearchText = publishedFile.Title,
                        AppId = publishedFile.ConsumerAppId,
                        Page = 0,
                        NumPerPage = 3,
                        ReturnPreviews = true
                    });

                    publishedFilePreviews = queryPreviews?.PublishedFileDetails?.FirstOrDefault(x => x.PublishedFileId == publishedFile.PublishedFileId)?.Previews;
                }

                // Get change history (if missing or changed since our last check)
                if ((publishedFileHasChanged || !assetDescription.Changes.Any()) && assetDescription.TimeAccepted != null)
                {
                    publishedFileChangeNotesPageHtml = await _communityClient.GetHtml(new SteamWorkshopFileChangeNotesPageRequest()
                    {
                        Id = publishedFile.PublishedFileId.ToString()
                    });
                }
            }

            var assetClassHasItemDescription = assetClass.Descriptions?
                .Where(x =>
                    string.Equals(x.Type, Constants.SteamAssetClassDescriptionTypeHtml, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(x.Type, Constants.SteamAssetClassDescriptionTypeBBCode, StringComparison.InvariantCultureIgnoreCase)
                )
                .Select(x => x.Value)
                .Where(x => !String.IsNullOrEmpty(x))
                .Any() ?? false;

            // Get community market details from Steam (if item description or nameid is missing and it is a marketable item)
            var marketListingPageHtml = (string)null;
            var assetIsMarketable = (
                string.Equals(assetClass.Marketable, "1", StringComparison.InvariantCultureIgnoreCase) || 
                (string.IsNullOrEmpty(assetClass.Marketable) && !string.IsNullOrEmpty(assetClass.MarketHashName))
            );
            var needsDescription = (!assetClassHasItemDescription && string.IsNullOrEmpty(assetDescription.Description));
            var needsNameId = (assetDescription.NameId == null);
            if (assetIsMarketable && (needsDescription || needsNameId) && request.LookupAdditionalItemInfo)
            {
                marketListingPageHtml = await _communityClient.GetText(new SteamMarketListingPageRequest()
                {
                    AppId = request.AppId.ToString(),
                    MarketHashName = assetClass.MarketHashName,
                });
            }

            // Get store details from Steam (if item description is missing and it is a recently accepted store item)
            var storeItemPageHtml = (XElement)null;
            var assetIsRecentlyAccepted = (assetDescription.TimeAccepted != null && assetDescription.TimeAccepted >= DateTimeOffset.Now.Subtract(TimeSpan.FromDays(10)));
            if (assetIsRecentlyAccepted && needsDescription && request.LookupAdditionalItemInfo)
            {
                var storeItems = await _storeClient.GetStorePaginated(new SteamItemStoreGetItemDefsPaginatedJsonRequest()
                {
                    AppId = request.AppId.ToString(),
                    Filter = SteamItemStoreGetItemDefsPaginatedJsonRequest.FilterAll,
                    SearchText = assetClass.MarketHashName,
                    Count = 1
                });
                if (storeItems?.Success == true && !string.IsNullOrEmpty(storeItems?.ResultsHtml))
                {
                    if (storeItems.ResultsHtml.Contains(assetClass.MarketHashName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var itemIdMatchGroup = Regex.Match(storeItems.ResultsHtml, Constants.SteamStoreItemDefLinkRegex).Groups;
                        var itemId = (itemIdMatchGroup.Count > 1)
                            ? itemIdMatchGroup[1].Value.Trim()
                            : null;

                        storeItemPageHtml = await _storeClient.GetStoreDetailPage(new SteamItemStoreDetailPageRequest()
                        {
                            AppId = request.AppId.ToString(),
                            ItemId = itemId,
                        });
                    }
                }
            }

            // Update the asset description
            var updateAssetDescription = await _commandProcessor.ProcessWithResultAsync(new UpdateSteamAssetDescriptionRequest()
            {
                AssetDescription = assetDescription,
                AssetClass = assetClass,
                PublishedFile = publishedFile,
                PublishedFileVoteData = publishedFileVotes,
                PublishedFilePreviews = publishedFilePreviews,
                PublishedFileChangeNotesPageHtml = publishedFileChangeNotesPageHtml,
                MarketListingPageHtml = marketListingPageHtml,
                StoreItemPageHtml = storeItemPageHtml
            });

            // If the asset description is now persistent (not transient)...
            if (!assetDescription.IsTransient)
            {
                // Queue a download of the workshop file data for analyse (if it's missing or has changed since our last check)
                if (publishedFileId > 0 && (publishedFileHasChanged || string.IsNullOrEmpty(assetDescription.WorkshopFileUrl)) && !assetDescription.WorkshopFileIsUnavailable && request.LookupAdditionalItemInfo)
                {
                    var app = await _db.SteamApps.FirstOrDefaultAsync(x => x.SteamId == request.AppId.ToString());
                    if (app?.IsActive == true)
                    {
                        await _serviceBus.SendMessageAsync(new ImportWorkshopFileContentsMessage()
                        {
                            AppId = request.AppId,
                            PublishedFileId = publishedFileId,
                            Force = publishedFileHasChanged
                        });
                    }
                }
            }

            return new ImportSteamAssetDescriptionResponse
            {
                AssetDescription = assetDescription
            };
        }
    }
}
