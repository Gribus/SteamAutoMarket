﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SteamAutoMarket.Steam.TradeOffer.Models
{
    public class InventoryRootModel
    {
        [JsonProperty("assets")] public List<RgInventory> Assets { get; set; }

        [JsonProperty("descriptions")] public List<RgDescription> Descriptions { get; set; }

        [JsonProperty("more_items")] public int MoreItems { get; set; }

        [JsonProperty("last_assetid")] public string LastAssetid { get; set; }

        [JsonProperty("total_inventory_count")]
        public int TotalInventoryCount { get; set; }

        [JsonProperty("success")] public int Success { get; set; }

        [JsonProperty("rwgrsn")] public int Rwgrsn { get; set; }

        public static RgDescription GetDescription(RgInventory asset, List<RgDescription> descriptions)
        {
            RgDescription description = null;
            try
            {
                description = descriptions
                    .First(item => asset.Instanceid == item.InstanceId && asset.Classid == item.Classid);
            }
            catch (Exception ex) when (ex is ArgumentNullException || ex is InvalidOperationException)
            {
            }

            return description;
        }
    }
}