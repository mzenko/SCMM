﻿using System.Text.Json.Serialization;

namespace SCMM.Market.iTradegg.Client
{
    public class iTradeggInventoryItemsResponse
    {
        [JsonPropertyName("inventory")]
        public IDictionary<string, iTradeggItem> Items { get; set; }

        [JsonPropertyName("extraItems")]
        public IDictionary<string, iTradeggItem> ExtraItems { get; set; }
    }
}
