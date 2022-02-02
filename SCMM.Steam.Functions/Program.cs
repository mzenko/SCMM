using Azure.Identity;
using CommandQuery.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SCMM.Azure.AI;
using SCMM.Azure.AI.Extensions;
using SCMM.Azure.ApplicationInsights.Filters;
using SCMM.Azure.ServiceBus.Extensions;
using SCMM.Google.Client;
using SCMM.Google.Client.Extensions;
using SCMM.Market.Buff.Client;
using SCMM.Market.CSDeals.Client;
using SCMM.Market.CSTrade.Client;
using SCMM.Market.DMarket.Client;
using SCMM.Market.iTradegg.Client;
using SCMM.Market.LootFarm.Client;
using SCMM.Market.RustSkins.Client;
using SCMM.Market.RustTM.Client;
using SCMM.Market.SkinBaron.Client;
using SCMM.Market.SkinMarketgg.Client;
using SCMM.Market.Skinport.Client;
using SCMM.Market.SkinsMonkey.Client;
using SCMM.Market.SwapGG.Client;
using SCMM.Market.TradeitGG.Client;
using SCMM.Market.TradeSkinsFast.Client;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Data.Models.Json;
using SCMM.Steam.API;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using System.Reflection;

JsonSerializerOptionsExtensions.SetDefaultOptions();

await new HostBuilder()
    .ConfigureLogging()
    .ConfigureAppConfiguration()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices()
    .Build()
    .RunAsync();

public static class HostExtensions
{
    public static IHostBuilder ConfigureLogging(this IHostBuilder builder)
    {
        return builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            if (AppDomain.CurrentDomain.IsDebugBuild())
            {
                logging.AddDebug();
                logging.AddConsole();
            }
            {
                logging.AddApplicationInsights();
            }
        });
    }

    public static IHostBuilder ConfigureAppConfiguration(this IHostBuilder builder)
    {
        return builder.ConfigureAppConfiguration(config =>
        {
            var appConfigConnectionString = Environment.GetEnvironmentVariable("AppConfigurationConnection");
            if (!String.IsNullOrEmpty(appConfigConnectionString))
            {
                config.AddAzureAppConfiguration(
                    options =>
                    {
                        options.Connect(appConfigConnectionString)
                            .ConfigureKeyVault(kv => kv.SetCredential(new DefaultAzureCredential()))
                            .Select(KeyFilter.Any, LabelFilter.Null)
                            .Select(KeyFilter.Any, Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
                            .Select(KeyFilter.Any, Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
                    },
                    optional: true
                );
            }
        });
    }

    public static IHostBuilder ConfigureServices(this IHostBuilder builder)
    {
        return builder.ConfigureServices(services =>
        {
            // Logging
            services.AddApplicationInsightsTelemetry();
            services.AddApplicationInsightsTelemetryProcessor<IgnoreSyntheticRequestsFilter>();
            services.AddApplicationInsightsTelemetryProcessor<Ignore304NotModifiedResponsesFilter>();

            // Database
            var dbConnectionString = Environment.GetEnvironmentVariable("SteamDbConnection");
            if (!String.IsNullOrEmpty(dbConnectionString))
            {
                services.AddDbContext<SteamDbContext>(options =>
                {
                    options.UseSqlServer(dbConnectionString, sql =>
                    {
                        //sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        sql.EnableRetryOnFailure();
                    });
                    options.EnableSensitiveDataLogging(AppDomain.CurrentDomain.IsDebugBuild());
                    options.EnableDetailedErrors(AppDomain.CurrentDomain.IsDebugBuild());
                });
            }

            // Service bus
            var serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnection");
            if (!String.IsNullOrEmpty(serviceBusConnectionString))
            {
                services.AddAzureServiceBus(serviceBusConnectionString);
            }

            // 3rd party clients
            services.AddSingleton((services) =>
            {
                var configuration = services.GetService<IConfiguration>();
                return configuration.GetSteamConfiguration();
            });
            services.AddSingleton((services) =>
            {
                var configuration = services.GetService<IConfiguration>();
                return configuration.GetAzureAiConfiguration();
            });
            services.AddSingleton((services) =>
            {
                var configuration = services.GetService<IConfiguration>();
                return configuration.GetGoogleConfiguration();
            });
            services.AddSingleton<SteamSession>();
            services.AddSingleton<AzureAiClient>();
            services.AddSingleton<GoogleClient>();
            services.AddSingleton<BuffWebClient>();
            services.AddSingleton<CSDealsWebClient>();
            services.AddSingleton<CSTradeWebClient>();
            services.AddSingleton<DMarketWebClient>();
            //services.AddSingleton<GAMERALLWebClient>();
            services.AddSingleton<iTradeggWebClient>();
            services.AddSingleton<LootFarmWebClient>();
            services.AddSingleton<RustSkinsWebClient>();
            services.AddSingleton<RustTMWebClient>();
            //services.AddSingleton<RUSTVendorWebClient>();
            //services.AddSingleton<RustyTradeWebClient>();
            services.AddSingleton<SkinBaronWebClient>();
            services.AddSingleton<SkinMarketGGWebClient>();
            services.AddSingleton<SkinportWebClient>();
            services.AddSingleton<SkinsMonkeyWebClient>();
            services.AddSingleton<SwapGGWebClient>();
            services.AddSingleton<TradeitGGWebClient>();
            services.AddSingleton<TradeSkinsFastWebClient>();
            services.AddScoped<SteamWebApiClient>();
            services.AddScoped<SteamCommunityWebClient>();
            services.AddScoped<SteamWorkshopDownloaderWebClient>();

            // Command/query/message handlers
            services.AddCommands(Assembly.GetEntryAssembly(), Assembly.Load("SCMM.Discord.API"), Assembly.Load("SCMM.Steam.API"));
            services.AddQueries(Assembly.GetEntryAssembly(), Assembly.Load("SCMM.Discord.API"), Assembly.Load("SCMM.Steam.API"));
            services.AddMessages(Assembly.GetEntryAssembly());

            // Services
            services.AddScoped<SteamService>();
        });
    }
}
