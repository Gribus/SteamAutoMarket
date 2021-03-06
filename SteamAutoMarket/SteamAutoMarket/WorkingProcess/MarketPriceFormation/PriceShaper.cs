﻿namespace SteamAutoMarket.WorkingProcess.MarketPriceFormation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Forms;

    using SteamAutoMarket.CustomElements.Utils;
    using SteamAutoMarket.Steam.TradeOffer.Models.Full;

    internal class PriceShaper
    {
        public PriceShaper(
            DataGridView itemsToSaleGridView,
            EMarketSaleType type,
            double lowerValue,
            double lowerPercentValue)
        {
            Grid = itemsToSaleGridView;
            Type = type;
            LowerValue = lowerValue;
            LowerPercentValue = lowerPercentValue;
        }

        private static DataGridView Grid { get; set; }

        private static EMarketSaleType Type { get; set; }

        private static double LowerValue { get; set; }

        private static double LowerPercentValue { get; set; }

        public ToSaleObject GetItemsForSales()
        {
            List<ItemsForSale> itemsForSales;

            switch (Type)
            {
                case EMarketSaleType.Recommended:
                    itemsForSales = this.GetRecommendedPriceItemsForSale();
                    break;

                case EMarketSaleType.LowerThanAverage:
                    itemsForSales = this.GetAveragePriceItemsForSale();
                    break;

                case EMarketSaleType.LowerThanCurrent:
                    itemsForSales = this.GetCurrentPriceItemsForSale();
                    break;

                case EMarketSaleType.Manual:
                    itemsForSales = this.GetManualPriceItemsForSale();
                    break;

                default: throw new InvalidOperationException("Not implemented market sale type");
            }

            var changeValueType = EChangeValueType.ChangeValueTypeNone;
            double changeValue = 0;

            if (LowerValue != 0)
            {
                changeValueType = EChangeValueType.ChangeValueTypeByValue;
                changeValue = LowerValue;
            }
            else if (LowerPercentValue != 0)
            {
                changeValueType = EChangeValueType.ChangeValueTypeByPercent;
                changeValue = LowerPercentValue;
            }

            return new ToSaleObject(itemsForSales, Type, changeValueType, changeValue);
        }

        private List<ItemsForSale> GetRecommendedPriceItemsForSale()
        {
            var itemsForSale = new List<ItemsForSale>();

            foreach (var row in Grid.Rows.Cast<DataGridViewRow>())
            {
                var items = this.GetGridItemsList(row.Index);
                if (items == null)
                {
                    continue;
                }

                double? resultPrice = null;
                var averagePrice = this.GetGridAveragePrice(row.Index);
                var currentPrice = this.GetGridCurrentPrice(row.Index);

                if (averagePrice != null && currentPrice != null)
                {
                    if (averagePrice > currentPrice)
                    {
                        resultPrice = averagePrice;
                    }
                    else if (currentPrice >= averagePrice)
                    {
                        resultPrice = currentPrice - 0.01;
                    }
                }

                if (resultPrice == null || resultPrice.Value <= 0)
                {
                    resultPrice = null;
                }

                itemsForSale.Add(new ItemsForSale(items, resultPrice));
            }

            return itemsForSale;
        }

        private List<ItemsForSale> GetCurrentPriceItemsForSale()
        {
            var itemsForSale = new List<ItemsForSale>();

            var priceShapingStrategy = this.GetPriceShapingStrategy();

            foreach (var row in Grid.Rows.Cast<DataGridViewRow>())
            {
                var items = this.GetGridItemsList(row.Index);
                if (items == null)
                {
                    continue;
                }

                var price = this.GetGridCurrentPrice(row.Index);
                if (price == null || price.Value <= 0)
                {
                    price = null;
                }
                else
                {
                    price = priceShapingStrategy.Format(price.Value);
                }

                itemsForSale.Add(new ItemsForSale(items, price));
            }

            return itemsForSale;
        }

        private List<ItemsForSale> GetAveragePriceItemsForSale()
        {
            var itemsForSale = new List<ItemsForSale>();

            var priceShapingStrategy = this.GetPriceShapingStrategy();

            foreach (var row in Grid.Rows.Cast<DataGridViewRow>())
            {
                var items = this.GetGridItemsList(row.Index);
                if (items == null)
                {
                    continue;
                }

                var price = this.GetGridAveragePrice(row.Index);
                if (price == null || price.Value <= 0)
                {
                    price = null;
                }
                else
                {
                    price = priceShapingStrategy.Format(price.Value);
                }

                itemsForSale.Add(new ItemsForSale(items, price));
            }

            return itemsForSale;
        }

        private List<ItemsForSale> GetManualPriceItemsForSale()
        {
            var itemsForSale = new List<ItemsForSale>();

            foreach (var row in Grid.Rows.Cast<DataGridViewRow>())
            {
                var items = this.GetGridItemsList(row.Index);
                if (items == null)
                {
                    continue;
                }

                var price = this.GetGridCurrentPrice(row.Index);
                if (!price.HasValue)
                {
                    continue;
                }

                itemsForSale.Add(new ItemsForSale(items, price.Value));
            }

            return itemsForSale;
        }

        private List<FullRgItem> GetGridItemsList(int index)
        {
            var items = new List<FullRgItem>();

            var row = ItemsToSaleGridUtils.GetGridHidenItemsListCell(Grid, index);
            if (row?.Value == null)
            {
                return items;
            }

            items = row.Value as List<FullRgItem>;
            if (items != null && items.Count == 0)
            {
                return null;
            }

            return items;
        }

        private double? GetGridCurrentPrice(int index)
        {
            var row = ItemsToSaleGridUtils.GetGridCurrentPriceTextBoxCell(Grid, index);
            if (row?.Value == null)
            {
                return null;
            }

            if (!this.GetDouble(row.Value, out var price))
            {
                return null;
            }

            if (price <= 0)
            {
                return null;
            }

            return price;
        }

        private double? GetGridAveragePrice(int index)
        {
            var row = ItemsToSaleGridUtils.GetGridAveragePriceTextBoxCell(Grid, index);
            if (row?.Value == null)
            {
                return null;
            }

            if (!this.GetDouble(row.Value, out var price))
            {
                return null;
            }

            if (price <= 0)
            {
                return null;
            }

            return price;
        }

        private bool GetDouble(object value, out double result)
        {
            string stringValue;
            switch (value)
            {
                case double _:
                    result = (double)value;
                    return true;
                case string _:
                    stringValue = (string)value;
                    break;
                default:
                    stringValue = value.ToString();
                    break;
            }

            return double.TryParse(stringValue, NumberStyles.Any, CultureInfo.CurrentCulture, out result)
                   || double.TryParse(stringValue, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result)
                   || double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        private PriceShapingStrategy GetPriceShapingStrategy()
        {
            if (LowerValue != 0)
            {
                return PriceShapingStrategyContainer.ByValue;
            }

            if (LowerPercentValue != 0)
            {
                return PriceShapingStrategyContainer.ByPercent;
            }

            return PriceShapingStrategyContainer.DoNotChange;
        }

        public abstract class PriceShapingStrategy
        {
            public abstract double Format(double oldPrice);
        }

        public class ChangeByValueStrategy : PriceShapingStrategy
        {
            public override double Format(double oldValue)
            {
                return oldValue + LowerValue;
            }
        }

        public class ChangeByPercentStrategy : PriceShapingStrategy
        {
            public override double Format(double oldValue)
            {
                return oldValue + oldValue * LowerPercentValue / 100;
            }
        }

        public class DoNotChangeStrategy : PriceShapingStrategy
        {
            public override double Format(double oldValue)
            {
                return oldValue;
            }
        }

        public class PriceShapingStrategyContainer
        {
            public static PriceShapingStrategy ByValue { get; } = new ChangeByValueStrategy();

            public static PriceShapingStrategy ByPercent { get; } = new ChangeByPercentStrategy();

            public static PriceShapingStrategy DoNotChange { get; } = new DoNotChangeStrategy();
        }
    }
}