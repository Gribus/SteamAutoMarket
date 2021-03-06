﻿namespace SteamAutoMarket.Steam.Market.Models
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class PriceHistoryDay
    {
        public DateTime Date { get; set; }

        public List<PriceHistoryItem> History { get; set; }
    }
}