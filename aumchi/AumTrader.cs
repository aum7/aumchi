// aumtrader.cs
using cAlgo.API;
using System;

namespace Aumchi
{
    public class AumTrader
    {
        private readonly Robot robot;
        private readonly bool enableTrading;
        private readonly double stoplossPips;
        private readonly double lotSize;
        private const string label = "aumchi";
        public AumTrader(Robot robot, bool enableTrading, double stoplossPips, double lotSize)
        {
            this.robot = robot;
            this.enableTrading = enableTrading;
            this.stoplossPips = stoplossPips;
            this.lotSize = lotSize;
        }
        public void ManageEntry(Signal signal)
        {
            // minimal market execution
            if (!enableTrading)
            {
                robot.Print($"{DateTime.UtcNow} (utc) aumchi : trading disabled");
                return;
            }
            TradeType tradeType;
            switch (signal.Kind)
            {
                case SignalKind.buySignal:
                    tradeType = TradeType.Buy; break;
                case SignalKind.sellSignal:
                    tradeType = TradeType.Sell; break;
                default:
                    robot.Print($"{DateTime.UtcNow} (utc) aumchi : unknown signal ({signal.Kind})");
                    return;
            }
            double? stoploss = null;
            if (stoplossPips > 0)
            {
                // below gives unexpected results
                // double slOffset = stoplossPips * robot.Symbol.PipSize;
                // ? signal.Price - slOffset
                // : signal.Price + slOffset;
                stoploss = stoplossPips > 0 ? (tradeType == TradeType.Buy ? signal.Price - stoplossPips : signal.Price + stoplossPips) : (double?)null;
            }
            long volume = (long)Math.Max(1, robot.Symbol.QuantityToVolumeInUnits(lotSize));

            var result = robot.ExecuteMarketOrder(tradeType, robot.SymbolName, volume, label, stoploss, null);

            if (result.IsSuccessful) robot.Print($"{DateTime.UtcNow} (utc) aumchi : executed {tradeType} @ {result.Position.EntryPrice}");

            else robot.Print($"{DateTime.UtcNow} (utc) aumchi : market order execution failed : {result.Error}");
        }
    }
}