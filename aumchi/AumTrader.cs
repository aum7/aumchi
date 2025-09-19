// aumtrader.cs
using cAlgo.API;
using System;

namespace Aumchi
{
    public class AumTrader
    {
        private readonly Robot robot;
        private readonly Func<bool> isTradingEnabled;
        private readonly bool singleTradeOnly;
        private readonly double stoplossPips;
        private readonly double lotSize;
        private const string label = "aumchi";
        public AumTrader(
            Robot robot,
            Func<bool> isTradingEnabled,
            bool singleTradeOnly,
            double stoplossPips,
            double lotSize)
        {
            this.robot = robot;
            this.isTradingEnabled = isTradingEnabled;
            this.singleTradeOnly = singleTradeOnly;
            this.stoplossPips = stoplossPips;
            this.lotSize = lotSize;
        }
        // returns true if market order executed successfully
        public bool ManageEntry(Signal signal)
        {
            // minimal market execution
            if (!isTradingEnabled())
            {
                robot.Print($"{DateTime.UtcNow} (utc) {label} : entry signal - trading disabled");
                return false;
            }
            // single trade logic
            if (singleTradeOnly && robot.Positions.Find(label, robot.SymbolName) != null)
            {
                robot.Print($"{DateTime.UtcNow} (utc) {label} : single trade enabled, position already open");
                return false;
            }
            TradeType tradeType;
            switch (signal.Kind)
            {
                case SignalKind.buySignal:
                    tradeType = TradeType.Buy; break;
                case SignalKind.sellSignal:
                    tradeType = TradeType.Sell; break;
                default:
                    robot.Print($"{DateTime.UtcNow} (utc) {label} : unknown signal ({signal.Kind})");
                    return false;
            }
            long volume = (long)Math.Max(1, robot.Symbol.QuantityToVolumeInUnits(lotSize));
            // use stoplosspips as is : no conversion
            var result = robot.ExecuteMarketOrder(tradeType, robot.SymbolName, volume, label, stoplossPips, null);

            if (result.IsSuccessful)
            {
                robot.Print($"{DateTime.UtcNow} (utc) {label} : executed {tradeType} @ {result.Position.EntryPrice}");
                return true;
            }
            else
            {
                robot.Print($"{DateTime.UtcNow} (utc) {label} : market order execution failed : {result.Error}");
                return false;
            }
        }
    }
}