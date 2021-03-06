﻿namespace SteamAutoMarket.Steam.Market.Models.Json
{
    using System;

    using Newtonsoft.Json;

    [Serializable]
    public class JDescription
    {
        [JsonProperty("appid")]
        public int Appid { get; set; }

        [JsonProperty("classid")]
        public long Classid { get; set; }

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }

        [JsonProperty("icon_url_large")]
        public string IconUrlLarge { get; set; }

        [JsonProperty("id")]
        public long? Id { get; set; }

        [JsonProperty("instanceid")]
        public long Instanceid { get; set; }

        [JsonProperty("marketable")]
        public bool Marketable { get; set; }

        [JsonProperty("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonProperty("market_name")]
        public string MarketName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_color")]
        public string NameColor { get; set; }

        [JsonProperty("tradable")]
        public bool Tradable { get; set; }
    }
}