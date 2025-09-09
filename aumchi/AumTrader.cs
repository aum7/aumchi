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
        public void ExecuteSignal(Signal signal)
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
            long volume = (long)Math.Max(1, robot.Symbol.QuantityToVolumeInUnits(lotSize));
            // double? stoploss = stoplossUnits > 0 ? (tradeType == TradeType.Buy ? signal.Price - stoplossUnits : signal.Price + stoplossUnits) : (double?)null;
            double? stoploss = stoplossPips > 0 ? (tradeType == TradeType.Buy ? signal.Price - robot.Symbol.PipSize * stoplossPips : signal.Price + robot.Symbol.PipSize * stoplossPips) : (double?)null;

            var result = robot.ExecuteMarketOrder(tradeType, robot.SymbolName, volume, label, stoploss, null);
            if (result.IsSuccessful) robot.Print($"{DateTime.UtcNow} (utc) aumchi : executed {tradeType} @ {result.Position.EntryPrice}");
            else robot.Print($"{DateTime.UtcNow} (utc) aumchi : market order execution failed : {result.Error}");
        }
    }
}