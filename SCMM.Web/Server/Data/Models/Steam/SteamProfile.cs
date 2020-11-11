﻿using SCMM.Web.Server.Data.Types;
using SCMM.Web.Shared.Data.Models.Steam;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SCMM.Web.Server.Data.Models.Steam
{
    public class SteamProfile : ConfigurableEntity<SteamProfileConfiguration>
    {
        public SteamProfile()
        {
            Roles = new PersistableStringCollection();
            InventoryItems = new Collection<SteamProfileInventoryItem>();
            MarketItems = new Collection<SteamProfileMarketItem>();
            WorkshopFiles = new Collection<SteamAssetWorkshopFile>();
        }

        public string SteamId { get; set; }

        public string ProfileId { get; set; }

        public string DiscordId { get; set; }

        public string Name { get; set; }

        public string AvatarUrl { get; set; }

        public string AvatarLargeUrl { get; set; }

        public string TradeUrl { get; set; }

        public string Country { get; set; }

        public Guid? LanguageId { get; set; }

        public SteamLanguage Language { get; set; }

        public Guid? CurrencyId { get; set; }

        public SteamCurrency Currency { get; set; }

        public DateTimeOffset? LastViewedInventoryOn { get; set; }

        public DateTimeOffset? LastUpdatedInventoryOn { get; set; }

        public DateTimeOffset? LastSignedInOn { get; set; }

        public int DonatorLevel { get; set; }

        public long GamblingOffset { get; set; }

        public SteamProfileFlags Flags { get; set; }

        public SteamVisibilityType Privacy { get; set; }

        public PersistableStringCollection Roles { get; set; }

        public ICollection<SteamProfileInventoryItem> InventoryItems { get; set; }

        public ICollection<SteamProfileMarketItem> MarketItems { get; set; }

        public ICollection<SteamAssetWorkshopFile> WorkshopFiles { get; set; }

        protected override IEnumerable<ConfigurationDefinition> ConfigurationDefinitions
            => SteamProfileConfiguration.Definitions;
    }
}
