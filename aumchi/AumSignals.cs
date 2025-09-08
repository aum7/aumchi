// aumsignals.cs
using System;
using System.Linq;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Internals;

namespace Aumchi
{
    public enum OrderType
    {
        buy, sell, alertBuy, alertSell, none // closeBuy, closeSell,
    }
    public class LineOrderSpecs
    {
        public ChartTrendLine Line { get; set; }
        public OrderType OrderType { get; set; }
        public bool IsTrail { get; set; }
        public string Comment { get; set; }
    }
    public class Signal
    {
        public TradeType Type { get; set; }
        public double Price { get; set; }
        public string SourceID { get; set; } // line id
        public double SuggestedVolumeInUnits { get; set; } = 1;
    }
    public class AumSignals
    {
        private readonly Robot robot;
        private readonly double trailOrderLinePips;
        private readonly int trailOrderLineBarsBack;
        private readonly string trailOrderLineTf;
        private readonly bool enableTrading;
        private readonly AumUI ui;
        private readonly Dictionary<string, LineOrderSpecs> trackedLines = new();
        private ChartStaticText botText;
        private Color botTextColor;
        private TradeType? armedTradeType;
        public event Action<Signal> OnSignal;
        public AumSignals(Robot robot, double trailOrderLinePips, int trailOrderLineBarsBack, string trailOrderLineTf, bool enableTrading, AumUI ui)
        {
            this.robot = robot;
            this.trailOrderLinePips = trailOrderLinePips;
            this.trailOrderLineBarsBack = trailOrderLineBarsBack;
            this.trailOrderLineTf = trailOrderLineTf;
            this.enableTrading = enableTrading;
            this.ui = ui;
        }
        // called on init
        public void ScanLinesOnChart()
        {
            foreach (var line in robot.Chart.Objects.OfType<ChartTrendLine>())
            {
                TLineUpdate(line);
            }
        }
        // chart events monitor
        public void TLineUpdate(ChartTrendLine line)
        {
            if (line == null) return;
            var comment = line.Comment.ToLower().Trim();
            var orderType = ParseOrderType(comment);
            bool isTrail = comment.Contains("trail");
            // remover invalid lines
            if (orderType == OrderType.none && !isTrail)
            {
                StopTrackingLine(line.Name);
                return;
            }
            // get specs for new line, else get existing line
            if (!trackedLines.TryGetValue(line.Name, out var order_line))
            {
                order_line = new LineOrderSpecs();
                trackedLines[line.Name] = order_line;
                robot.Print($"new order t-line '{line.Name}' with comment '{line.Comment}'");
            }
            else
            {
                // print only if comment has changed
                if (order_line.Comment != line.Comment)
                {
                    robot.Print($"line '{line.Name}' comment changed to '{line.Comment}'");
                }
            }
            // update spec with new comment
            order_line.Line = line;
            order_line.IsTrail = isTrail;
            order_line.OrderType = orderType;
            order_line.Comment = line.Comment;
            // update color based on order
            SetLineColor(order_line);
        }
        private OrderType ParseOrderType(string comment)
        {
            var words = comment.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // if (words.Contains("close") && words.Contains("buy")) return OrderType.closeBuy;
            // if (words.Contains("close") && words.Contains("sell")) return OrderType.closeSell;
            if (words.Contains("alert") && words.Contains("buy")) return OrderType.alertBuy;
            if (words.Contains("alert") && words.Contains("sell")) return OrderType.alertSell;
            if (words.Contains("buy")) return OrderType.buy;
            if (words.Contains("sell")) return OrderType.sell;
            return OrderType.none;
        }
        private void SetLineColor(LineOrderSpecs order_line)
        {
            switch (order_line.OrderType)
            {
                case OrderType.buy:
                    botTextColor = Color.DodgerBlue;
                    order_line.Line.Color = botTextColor;
                    break;
                case OrderType.sell:
                    botTextColor = Color.Red;
                    order_line.Line.Color = botTextColor;
                    break;
                case OrderType.alertBuy:
                case OrderType.alertSell:
                    botTextColor = Color.Magenta;
                    order_line.Line.Color = botTextColor;
                    break;
            }
        }
        // called by remove event : clean data when line deleted
        public void StopTrackingLine(string lineName)
        {
            if (!trackedLines.Remove(lineName, out var spec)) return;
            robot.Print($"removed order line '{lineName}'");
            // if trailing line was removed, update chart text
            if (spec.IsTrail && !trackedLines.Values.Any(l => l.IsTrail) && botText != null)
            {
                robot.Chart.RemoveObject("trailInfo");
                botText = null;
            }
        }
        // called each tick : iterate tracked lines with order
        public void Update()
        {
            bool isAnyLineTrailing = false;
            foreach (var order_line in trackedLines.Values)
            {
                if (order_line.IsTrail)
                {
                    UpdateTrailLine(order_line);
                    isAnyLineTrailing = true;
                }
                if (order_line.OrderType != OrderType.none)
                {
                    CheckLineCross(order_line);
                }
            }
            // manage trail text
            ui.UpdateStatusUI(isAnyLineTrailing, armedTradeType, enableTrading);
        }
        // if trend line has comment recognized as order, update ontick
        private void UpdateTrailLine(LineOrderSpecs order_line)
        {
            // map order line timeframe to timeframe
            TimeFrame selectedTf;
            // var int mappedTrailOrderLineTf = 
            switch (trailOrderLineTf)
            {
                case "1mi":
                    selectedTf = TimeFrame.Minute; break;
                case "5mi":
                    selectedTf = TimeFrame.Minute5; break;
                case "10mi":
                    selectedTf = TimeFrame.Minute10; break;
                case "15mi":
                    selectedTf = TimeFrame.Minute15; break;
                case "30mi":
                    selectedTf = TimeFrame.Minute30; break;
                case "1h":
                    selectedTf = TimeFrame.Hour; break;
                case "4h":
                    selectedTf = TimeFrame.Hour4; break;
                case "1d":
                    selectedTf = TimeFrame.Daily; break;
                case "1w":
                    selectedTf = TimeFrame.Weekly; break;
                case "1m":
                    selectedTf = TimeFrame.Monthly; break;
                default:
                    robot.Print($"error : timeframe {trailOrderLineTf} is not supported : defaulting to '1h'");
                    selectedTf = TimeFrame.Hour;
                    break;
            }
            if (!order_line.IsTrail) return;
            // set t-line horizontal for visual recognition of order
            order_line.Line.Y1 = order_line.Line.Y2;
            double marketPrice = order_line.OrderType is OrderType.buy or OrderType.alertBuy ? robot.Symbol.Ask : robot.Symbol.Bid;
            double trailLevel;
            // trail with pips
            if (trailOrderLinePips > 0)
            {
                double offset = trailOrderLinePips * robot.Symbol.PipSize;
                if (order_line.OrderType is OrderType.buy or OrderType.alertBuy)
                {
                    trailLevel = order_line.Line.Y1 - offset;
                    if (marketPrice < trailLevel)
                        order_line.Line.Y1 = order_line.Line.Y2 = marketPrice + offset;
                }
                else if (order_line.OrderType is OrderType.sell or OrderType.alertSell)
                {
                    trailLevel = order_line.Line.Y1 + offset;
                    if (marketPrice > trailLevel)
                        order_line.Line.Y1 = order_line.Line.Y2 = marketPrice - offset;
                }
            }
            // trail x bars back on x timeframe
            else
            {
                var bars = robot.MarketData.GetBars(selectedTf);
                int barsAvailable = bars.Count;
                int lookbackPeriod = Math.Min(trailOrderLineBarsBack, barsAvailable - 1);
                // exit if no valid period
                if (lookbackPeriod <= 0) return;
                // get trail level / price based on trade direction
                if (order_line.OrderType is OrderType.buy or OrderType.alertBuy)
                {
                    // get highest price of last x bars back
                    trailLevel = bars.HighPrices.Skip(barsAvailable - 1 - lookbackPeriod).Take(lookbackPeriod).Max();
                    if (marketPrice < trailLevel)
                    {
                        order_line.Line.Y1 = order_line.Line.Y2 = trailLevel;
                    }
                }
                else if (order_line.OrderType is OrderType.sell or OrderType.alertSell)
                {
                    // get lowest price for last x bars back
                    trailLevel = bars.LowPrices.Skip(barsAvailable - 1 - lookbackPeriod).Take(lookbackPeriod).Min();
                    if (marketPrice > trailLevel)
                    {
                        order_line.Line.Y1 = order_line.Line.Y2 = trailLevel;
                    }
                }
            }
        }
        // price crosses t-line > signal triggered
        private void CheckLineCross(LineOrderSpecs order_line)
        {
            // trendline end checked : if in past > dont execute orders
            var lastBarTime = robot.Bars.Last(1).OpenTime;
            var orderEndTime = order_line.Line.Time2;
            if (lastBarTime >= orderEndTime)
            {
                StopTrackingLine(order_line.Line.Name);
                robot.Print($"t-line '{order_line.Line.Name}' expired at {order_line.Line.Time2} : removed from tracked lines");
                return;
            }
            // todo check price vs line & raise onsignal
        }
    }
}