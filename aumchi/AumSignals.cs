// aumsignals.cs
using System;
using System.Linq;
using System.Collections.Generic;
using cAlgo.API;

namespace Aumchi
{
    public enum OrderType
    {
        buy, sell, alertBuy, alertSell, closeBuy, closeSell, none
    }
    public enum SignalKind
    {
        buySignal, sellSignal, closeBuySignal, closeSellSignal
    }
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
        public string LineName { get; set; }
    }
    public class AumSignals
    {
        private readonly Robot robot;
        // callback to read EnableTrading
        private readonly Func<bool> isTradingEnabled;
        private readonly bool triggerOrderOnce;
        private readonly double trailOrderLinePips;
        private readonly int trailOrderLineBarsBack;
        private readonly TimeFrame trailOrderLineTf;
        private readonly string soundFile;
        private readonly AumUI ui;
        private readonly AumTrail trail;

        private readonly Dictionary<string, LineOrderSpecs> orderLinesDict = new();
        public event Action<Signal> OnSignal;
        // functions
        public AumSignals(
            Robot robot,
            Func<bool> isTradingEnabled,
            bool triggerOrderOnce,
            double trailOrderLinePips,
            int trailOrderLineBarsBack,
            TimeFrame trailOrderLineTf,
            string soundFile,
            AumUI ui)
        {
            this.robot = robot;
            this.isTradingEnabled = isTradingEnabled;
            this.triggerOrderOnce = triggerOrderOnce;
            this.trailOrderLinePips = trailOrderLinePips;
            this.trailOrderLineBarsBack = trailOrderLineBarsBack;
            this.trailOrderLineTf = trailOrderLineTf;
            this.soundFile = soundFile;
            this.trail = new AumTrail(
                robot,
                trailOrderLinePips,
                trailOrderLineBarsBack,
                trailOrderLineTf);
            this.ui = ui;
        }
        // todo revise below code
        public OrderType? GetTradingStatus()
        {
            var active = orderLinesDict.Values.FirstOrDefault(o => o.OrderType != OrderType.none && !o.IsTrail);
            return active?.OrderType;
        }
        public OrderType? GetTrailStatus()
        {
            var active = orderLinesDict.Values.FirstOrDefault(o => o.IsTrail && o.OrderType != OrderType.none);
            return active?.OrderType;
        }
        // called on init
        public void ScanLinesOnChart()
        {
            foreach (var line in robot.Chart.Objects.OfType<ChartTrendLine>())
            {
                TLineUpdate(line);
            }
            ui.UpdateStatusUI(
                isTradingEnabled(),
                GetTradingStatus(),
                GetTrailStatus()
                );
        }
        // chart events monitor : check trend lines
        public void TLineUpdate(ChartTrendLine line)
        {
            if (line == null) return;
            var comment = (line.Comment ?? string.Empty).ToLower().Trim();
            var orderType = ParseOrderType(comment);
            // hit marks inactive
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
            // print only if comment has changed
            else if (order_line.Comment != line.Comment)
            {
                robot.Print($"line '{line.Name}' comment changed to '{line.Comment}'");
            }
            // update spec with new comment
            order_line.Line = line;
            order_line.Comment = line.Comment;
            order_line.IsTrail = isTrail;
            order_line.OrderType = orderType;
            order_line.IsTriggered = isTriggered;
            // treat hit lines as inactive
            order_line.OrderType = isTriggered ? OrderType.none : orderType;
            // update color based on order
            if (isTriggered) line.Color = AumStyle.clrInactive;
            else SetLineColor(order_line);
            // manage trade & trail text / button
            ui.UpdateStatusUI(
                isTradingEnabled(),
                order_line.OrderType != OrderType.none ? order_line.OrderType : null,
                order_line.IsTrail ? (order_line.OrderType != OrderType.none ? order_line.OrderType : null) : null);
        }
        private OrderType ParseOrderType(string comment)
        {
            var words = comment.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // if hit return none
            if (words.Contains("hit"))
                return OrderType.none;
            if (words.Contains("buy") && words.Contains("close"))
                return OrderType.closeBuy;
            if (words.Contains("sell") && words.Contains("close"))
                return OrderType.closeSell;
            if (words.Contains("buy") && words.Contains("alert"))
                return OrderType.alertBuy;
            if (words.Contains("sell") && words.Contains("alert"))
                return OrderType.alertSell;
            if (words.Contains("buy"))
                return OrderType.buy;
            if (words.Contains("sell"))
                return OrderType.sell;
            return OrderType.none;
        }
        private static void SetLineColor(LineOrderSpecs order_line)
        {
            switch (order_line.OrderType)
            {
                case OrderType.buy:
                    order_line.Line.Color = AumStyle.clrBuy;
                    break;
                case OrderType.sell:
                    order_line.Line.Color = AumStyle.clrSell;
                    break;
                case OrderType.alertBuy:
                case OrderType.alertSell:
                    order_line.Line.Color = AumStyle.clrAlert;
                    break;
                case OrderType.closeBuy:
                case OrderType.closeSell:
                    order_line.Line.Color = AumStyle.clrClose;
                    break;
            }
        }
        // called by remove event : clean data when line deleted
        public void StopTrackingLine(string lineName)
        {
            if (!orderLinesDict.Remove(lineName, out var spec)) return;
            robot.Print($"removed order line '{lineName}' from dict");
            // pass inactive state to ui
            ui.UpdateStatusUI(
                isTradingEnabled(),
                GetTradingStatus(),
                GetTrailStatus()
                );
            // ui.UpdateStatusUI(false, null, null);
        }
        // called each tick : iterate tracked lines with order
        public void Update()
        {
            // debug
            foreach (var order_line in orderLinesDict.Values)
            {
                if (order_line.IsTrail)
                {
                    trail.UpdateTrailLine(order_line);
                }
                if (order_line.OrderType != OrderType.none)
                {
                    CheckLineCross(order_line);
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

            // bool wasHit = false;
            // check trigger : buy signal
            if (order_line.OrderType == OrderType.buy && ask > linePrice)
            {
                var sig = new Signal
                {
                    Kind = SignalKind.buySignal,
                };
                OnSignal?.Invoke(sig);
                robot.Print($"{DateTime.UtcNow} (utc) BUY hit for line {order_line.Line.Name}");
                // wasHit = true;
            }
            else if (order_line.OrderType == OrderType.alertBuy && ask > linePrice)
            {
                PlayAlertSound();
                robot.Print($"{DateTime.UtcNow} (utc) ALERT BUY hit for line {order_line.Line.Name} @ ask {ask}");
                // wasHit = true;
            }
            else if (order_line.OrderType == OrderType.sell && bid < linePrice)
            {
                var sig = new Signal
                {
                    Kind = SignalKind.sellSignal,
                };
                OnSignal?.Invoke(sig);
                robot.Print($"{DateTime.UtcNow} (utc) SELL hit for line {order_line.Line.Name}");
                // wasHit = true;
            }
            else if (order_line.OrderType == OrderType.alertSell && bid < linePrice)
            {
                PlayAlertSound();
                robot.Print($"{DateTime.UtcNow} (utc) ALERT SELL hit for line {order_line.Line.Name} @ bid {bid}");
                // wasHit = true;
            }
            else if (order_line.OrderType == OrderType.closeBuy && bid < linePrice)
            {
                var sig = new Signal { Kind = SignalKind.closeBuySignal };
                OnSignal?.Invoke(sig);
                robot.Print($"{DateTime.UtcNow} (utc) CLOSE BUY hit for line {order_line.Line.Name}");
                // wasHit = true;
            }
            else if (order_line.OrderType == OrderType.closeSell && ask > linePrice)
            {
                var sig = new Signal { Kind = SignalKind.closeSellSignal };
                OnSignal?.Invoke(sig);
                robot.Print($"{DateTime.UtcNow} (utc) CLOSE SELL hit for line {order_line.Line.Name}");
                // wasHit = true;
            }
            // update line comment when triggered
            //     if (wasHit && triggerOrderOnce)
            //     {
            //         order_line.Line.Comment += " hit";
            //         order_line.IsTriggered = true;
            //         order_line.OrderType = OrderType.none;
            //         robot.Print($"t-line '{order_line.Line.Name}' was triggered : marked as hit");
            //     }
        }
        private void PlayAlertSound()
        {
            try
            {
                robot.Notifications.PlaySound(soundFile);
                // robot.Print($"{DateTime.UtcNow} (utc) alert : {alertSignal} @ {price}");
            }
            catch (Exception ex)
            {
                robot.Print($"alert sound failed : {ex.Message}");
            }
        }
        // mark line as triggered if position opened successfully
        public void MarkLineTriggered(string lineName)
        {
            if (!orderLinesDict.TryGetValue(lineName, out var order_line)) return;
            if (order_line.IsTriggered) return;
            order_line.Line.Comment = (order_line.Line.Comment ?? string.Empty) + "hit";
            order_line.IsTriggered = true;
            order_line.OrderType = OrderType.none;
            order_line.Line.Color = AumStyle.clrInactive;
            robot.Print($"t-line {lineName} was triggered : marked as 'hit'");
            ui.UpdateStatusUI(
                isTradingEnabled(),
                GetTradingStatus(),
                GetTrailStatus()
            );
        }
    }
}