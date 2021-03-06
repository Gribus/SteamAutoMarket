﻿namespace SteamAutoMarket.Steam.TradeOffer.Models
{
    using System;

    using Newtonsoft.Json;

    [Serializable]
    public class ItemAttribute
    {
        [JsonProperty("account_info")]
        public AccountInfo AccountInfo { get; set; }

        [JsonProperty("defindex")]
        public ushort DefIndex { get; set; }

        [JsonProperty("float_value")]
        public float FloatValue { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}