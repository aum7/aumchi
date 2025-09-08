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
        private const string Label = "aumchi";
        public AumTrader(Robot robot, bool enableTrading, double stoplossPips)
        {
            this.robot = robot;
            this.enableTrading = enableTrading;
            this.stoplossPips = stoplossPips;
        }
        public void ExecuteSignal(Signal signal)
        {
            // minimal market execution todo stoploss
            if (!enableTrading)
            {
                robot.Print($"{DateTime.UtcNow} (utc) aumchi : trading disabled");
                return;
            }
            long volume = (long)Math.Max(
                1, signal.SuggestedVolumeInUnits);
            double? stoploss = stoplossPips > 0
            ? (signal.Type == TradeType.Buy
            ? signal.Price - robot.Symbol.PipSize * stoplossPips
            : signal.Price + robot.Symbol.PipSize * stoplossPips)
            : (double?)null;
            var result = robot.ExecuteMarketOrder(
                signal.Type,
                robot.SymbolName,
                volume, Label, stoploss, null);
            if (result.IsSuccessful)
                robot.Print($"{DateTime.UtcNow} (utc) aumchi executed {signal.Type} @ {result.Position.EntryPrice}");
            else robot.Print($"{DateTime.UtcNow} (utc) aumchi execute failed : {result.Error}");
        }
    }
}