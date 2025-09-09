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
    public enum SignalKind { buySignal, sellSignal }
    public class LineOrderSpecs
    {
        public ChartTrendLine Line { get; set; }
        public OrderType OrderType { get; set; }
        public string Comment { get; set; }
        public bool IsTrail { get; set; }
        public bool IsTriggered { get; set; }
    }
    public class Signal
    {
        public SignalKind Kind { get; set; }
        public TradeType Type { get; set; }
        public double Price { get; set; }
        public string Message { get; set; }
    }
    public class AumSignals
    {
        private readonly Robot robot;
        private readonly bool enableTrading;
        private readonly bool triggerOrderOnce;
        private readonly double trailOrderLinePips;
        private readonly int trailOrderLineBarsBack;
        private readonly TimeFrame trailOrderLineTf;
        private readonly string soundFile;
        private readonly AumUI ui;
        private readonly Dictionary<string, LineOrderSpecs> orderLinesDict = new();
        private ChartStaticText botText;
        private TradeType? armedTradeType;
        public event Action<Signal> OnSignal;
        public AumSignals(Robot robot, bool enableTrading, bool triggerOrderOnce, double trailOrderLinePips, int trailOrderLineBarsBack, TimeFrame trailOrderLineTf, string soundFile, AumUI ui)
        {
            this.robot = robot;
            this.enableTrading = enableTrading;
            this.triggerOrderOnce = triggerOrderOnce;
            this.trailOrderLinePips = trailOrderLinePips;
            this.trailOrderLineBarsBack = trailOrderLineBarsBack;
            this.trailOrderLineTf = trailOrderLineTf;
            this.soundFile = soundFile;
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
            bool isTriggered = comment.Contains("hit");
            // remover invalid lines
            if (orderType == OrderType.none && !isTrail)
            {
                StopTrackingLine(line.Name);
                return;
            }
            // get specs for new line, else get existing line
            if (!orderLinesDict.TryGetValue(line.Name, out var order_line))
            {
                order_line = new LineOrderSpecs();
                orderLinesDict[line.Name] = order_line;
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
            order_line.Comment = line.Comment;
            order_line.IsTrail = isTrail;
            order_line.OrderType = orderType;
            order_line.IsTriggered = isTriggered;
            // update color based on order
            if (isTriggered) line.Color = AumStyles.clrInactive;
            else SetLineColor(order_line);
        }
        private OrderType ParseOrderType(string comment)
        {
            var words = comment.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // if (words.Contains("close") && words.Contains("buy")) return OrderType.closeBuy;
            // if (words.Contains("close") && words.Contains("sell")) return OrderType.closeSell;
            if (words.Contains("alert") && words.Contains("buy")) return OrderType.alertBuy;
            if (words.Contains("alert") && words.Contains("sell")) return OrderType.alertSell;
            if (words.Contains("buy"))
            {
                armedTradeType = TradeType.Buy;
                return OrderType.buy;
            }
            if (words.Contains("sell"))
            {
                armedTradeType = TradeType.Sell;
                return OrderType.sell;
            }
            if (words.Contains("hit"))
            {
                armedTradeType = null;
                return OrderType.none;
            }
            return OrderType.none;
        }
        private void SetLineColor(LineOrderSpecs order_line)
        {
            switch (order_line.OrderType)
            {
                case OrderType.buy:
                    order_line.Line.Color = AumStyles.clrBuy;
                    break;
                case OrderType.sell:
                    order_line.Line.Color = AumStyles.clrSell;
                    break;
                case OrderType.alertBuy:
                case OrderType.alertSell:
                    order_line.Line.Color = AumStyles.clrAlert;
                    break;
            }
        }
        // called by remove event : clean data when line deleted
        public void StopTrackingLine(string lineName)
        {
            if (!orderLinesDict.Remove(lineName, out var spec)) return;
            robot.Print($"removed order line '{lineName}'");
            // todo correct place to reset armedtradetype ???
            armedTradeType = null;
            // if trailing line was removed, update chart text
            if (spec.IsTrail && !orderLinesDict.Values.Any(l => l.IsTrail) && botText != null)
            {
                robot.Chart.RemoveObject("trailInfo");
                botText = null;
            }
        }
        // called each tick : iterate tracked lines with order
        public void Update()
        {
            // debug
            // robot.Print($"aumchi : armedtradetype : {armedTradeType}");
            bool isAnyLineTrailing = false;
            foreach (var order_line in orderLinesDict.Values)
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
            ui.UpdateStatusUI(enableTrading, isAnyLineTrailing, armedTradeType);
        }
        // if trend line has comment recognized as order, update ontick
        private void UpdateTrailLine(LineOrderSpecs order_line)
        {
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
                // var bars = robot.MarketData.GetBars(selectedTf);
                var bars = robot.MarketData.GetBars(trailOrderLineTf);
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
            // if line already triggered : return
            if (triggerOrderOnce && order_line.IsTriggered) return;
            // trendline end checked : if in past > dont execute orders
            var lastBarTime = robot.Bars.Last(1).OpenTime;
            var orderEndTime = order_line.Line.Time2;
            if (lastBarTime >= orderEndTime)
            {
                StopTrackingLine(order_line.Line.Name);
                robot.Print($"t-line '{order_line.Line.Name}' expired at {order_line.Line.Time2} : removed from tracked lines");
                return;
            }
            // check line triggered : current price
            double bid = robot.Symbol.Bid;
            double ask = robot.Symbol.Ask;
            // interpolate line price if not horizontal
            DateTime t1 = order_line.Line.Time1;
            DateTime t2 = order_line.Line.Time2;
            double y1 = order_line.Line.Y1;
            double y2 = order_line.Line.Y2;
            double lineSlope = (y2 - y1) / (t2 - t1).TotalSeconds;
            double linePrice = y1 + lineSlope * (lastBarTime - t1).TotalSeconds;

            bool wasHit = false;
            // check trigger : buy signal
            if (order_line.OrderType == OrderType.buy && ask > linePrice)
            {
                var sig = new Signal { Kind = SignalKind.buySignal };
                OnSignal?.Invoke(sig);
                robot.Print($"{DateTime.UtcNow} (utc) buy hit");
                wasHit = true;
            }
            else if (order_line.OrderType == OrderType.alertBuy && ask > linePrice)
            {
                PlayAlertSound("buy", ask);
                robot.Print($"{DateTime.UtcNow} (utc) alert buy hit");
                wasHit = true;
            }
            else if (order_line.OrderType == OrderType.sell && bid < linePrice)
            {
                var sig = new Signal { Kind = SignalKind.sellSignal };
                OnSignal?.Invoke(sig);
                robot.Print($"{DateTime.UtcNow} (utc) sell hit");
                wasHit = true;
            }
            else if (order_line.OrderType == OrderType.alertSell && bid < linePrice)
            {
                PlayAlertSound("sell", bid);
                robot.Print($"{DateTime.UtcNow} (utc) alert sell hit");
                wasHit = true;
            }
            // update line comment when triggered
            if (wasHit && triggerOrderOnce)
            {
                order_line.Line.Comment += " hit";
                robot.Print($"t-line '{order_line.Line.Name}' was triggered : marked as hit");
            }
        }
        private void PlayAlertSound(string alertSignal, double price)
        {
            try
            {
                robot.Notifications.PlaySound(soundFile);
                robot.Print($"{DateTime.UtcNow} (utc) alert : {alertSignal} @ {price}");
            }
            catch (Exception ex)
            {
                robot.Print($"alert sound failed : {ex.Message}");
            }
        }
    }
}