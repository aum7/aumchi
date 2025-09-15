// aumtrail.cs
using System;
using System.Data;
using System.Linq;
using cAlgo.API;

namespace Aumchi
{
    public class AumTrail
    {
        private readonly Robot robot;
        private readonly double trailOrderLinePips;
        private readonly int trailOrderLineBarsBack;
        private readonly TimeFrame trailOrderLineTf;

        public AumTrail(
            Robot robot,
            double trailOrderLinePips,
            int trailOrderLineBarsBack,
            TimeFrame trailOrderLineTf)
        {
            this.robot = robot;
            this.trailOrderLinePips = trailOrderLinePips;
            this.trailOrderLineBarsBack = trailOrderLineBarsBack;
            this.trailOrderLineTf = trailOrderLineTf;
        }
        // if trend line has comment recognized as order, update ontick
        public void UpdateTrailLine(LineOrderSpecs order_line)
        {
            if (!order_line.IsTrail) return;
            // set t-line horizontal for visual recognition of order
            order_line.Line.Y1 = order_line.Line.Y2;
            double marketPrice = order_line.OrderType is OrderType.buy or OrderType.alertBuy
                ? robot.Symbol.Ask
                : robot.Symbol.Bid;
            double marketPriceClose = order_line.OrderType is OrderType.closeBuy
                ? robot.Symbol.Bid
                : robot.Symbol.Ask;
            double trailLevel;
            // trail with pips
            if (trailOrderLinePips > 0)
            {
                double offset = trailOrderLinePips * robot.Symbol.PipSize;
                switch (order_line.OrderType)
                {
                    case OrderType.buy:
                    case OrderType.alertBuy:
                        trailLevel = order_line.Line.Y1 - offset;
                        if (marketPrice < trailLevel)
                            order_line.Line.Y1 = order_line.Line.Y2 = marketPrice + offset;
                        break;
                    case OrderType.sell:
                    case OrderType.alertSell:
                        trailLevel = order_line.Line.Y1 + offset;
                        if (marketPrice > trailLevel)
                            order_line.Line.Y1 = order_line.Line.Y2 = marketPrice - offset;
                        break;
                    case OrderType.closeBuy:
                        trailLevel = order_line.Line.Y1 + offset;
                        if (marketPriceClose > trailLevel)
                            order_line.Line.Y1 = order_line.Line.Y2 = marketPrice - offset;
                        break;
                    case OrderType.closeSell:
                        trailLevel = order_line.Line.Y1 - offset;
                        if (marketPriceClose < trailLevel)
                            order_line.Line.Y1 = order_line.Line.Y2 = marketPrice + offset;
                        break;
                }
            }
            //     if (order_line.OrderType is OrderType.buy or OrderType.alertBuy)
            //     {
            //         trailLevel = order_line.Line.Y1 - offset;
            //         if (marketPrice < trailLevel)
            //             order_line.Line.Y1 = order_line.Line.Y2 = marketPrice + offset;
            //     }
            //     else if (order_line.OrderType is OrderType.sell or OrderType.alertSell)
            //     {
            //         trailLevel = order_line.Line.Y1 + offset;
            //         if (marketPrice > trailLevel)
            //             order_line.Line.Y1 = order_line.Line.Y2 = marketPrice - offset;
            //     }
            //     // close orders
            //     else if (order_line.OrderType is OrderType.closeBuy)
            //     {
            //         trailLevel = order_line.Line.Y1 + offset;
            //         if (marketPriceClose > trailLevel)
            //             order_line.Line.Y1 = order_line.Line.Y2 = marketPriceClose - offset;
            //     }
            //     else if (order_line.OrderType is OrderType.closeSell)
            //     {
            //         trailLevel = order_line.Line.Y1 - offset;
            //         if (marketPriceClose < trailLevel)
            //             order_line.Line.Y1 = order_line.Line.Y2 = marketPriceClose + offset;
            //     }
            // }
            // trail x bars back on x timeframe
            else
            {
                var bars = robot.MarketData.GetBars(trailOrderLineTf);
                var barsCount = bars.Count;
                int lookbackPeriod = Math.Min(trailOrderLineBarsBack, barsCount - 1);
                // exit if no valid period
                // if (lookbackPeriod <= 0) return;
                // indexer logic
                // int startIndex = barsAvailable - lookbackPeriod;
                // var highs = bars.HighPrices;
                // var lows = bars.LowPrices;

                // switch (order_line.OrderType)
                // {
                //     // trade & alert orders
                //     case OrderType.buy:
                //     case OrderType.alertBuy:
                //         trailLevel = highs.Skip(startIndex).Max();
                //         if (marketPrice < trailLevel)
                //             order_line.Line.Y1 = order_line.Line.Y2 = trailLevel;
                //         break;
                //     case OrderType.sell:
                //     case OrderType.alertSell:
                //         trailLevel = lows.Skip(startIndex).Min();
                //         if (marketPrice > trailLevel)
                //             order_line.Line.Y1 = order_line.Line.Y2 = trailLevel;
                //         break;
                //     // close orders have reversed logic
                //     case OrderType.closeBuy:
                //         trailLevel = lows.Skip(startIndex).Min();
                //         if (marketPriceClose > trailLevel)
                //             order_line.Line.Y1 = order_line.Line.Y2 = trailLevel;
                //         break;
                //     case OrderType.closeSell:
                //         trailLevel = highs.Skip(startIndex).Max();
                //         if (marketPriceClose < trailLevel)
                //             order_line.Line.Y1 = order_line.Line.Y2 = trailLevel;
                //         break;
                // }
                // get trail level / price based on trade direction
                if (order_line.OrderType is OrderType.buy or OrderType.alertBuy)
                {
                    // get highest price of last x bars back
                    trailLevel = bars.HighPrices.Skip(barsCount - 1 - lookbackPeriod).Take(lookbackPeriod).Max();
                    if (marketPrice < trailLevel)
                        order_line.Line.Y1 = order_line.Line.Y2 = trailLevel;
                }
                else if (order_line.OrderType is OrderType.sell or OrderType.alertSell)
                {
                    // get lowest price for last x bars back
                    trailLevel = bars.LowPrices.Skip(barsCount - 1 - lookbackPeriod).Take(lookbackPeriod).Min();
                    if (marketPrice > trailLevel)
                        order_line.Line.Y1 = order_line.Line.Y2 = trailLevel;
                }
                else if (order_line.OrderType is OrderType.closeBuy)
                {
                    trailLevel = bars.LowPrices.Skip(barsCount - 1 - lookbackPeriod).Take(lookbackPeriod).Min();
                    if (marketPriceClose > trailLevel)
                        order_line.Line.Y1 = order_line.Line.Y2 = trailLevel;
                }
                else if (order_line.OrderType is OrderType.closeSell)
                {
                    trailLevel = bars.HighPrices.Skip(barsCount - 1 - lookbackPeriod).Take(lookbackPeriod).Max();
                    if (marketPriceClose < trailLevel)
                        order_line.Line.Y1 = order_line.Line.Y2 = trailLevel;
                }
            }
        }
    }
}