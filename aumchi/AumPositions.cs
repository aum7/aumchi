// aumpositions.cs
using System;
using cAlgo.API;

namespace Aumchi
{
    public class AumPositions
    {
        private readonly Robot robot;
        private readonly AumUI ui;
        private const string label = "aumchi";
        public AumPositions(Robot robot, AumUI ui)
        {
            this.robot = robot;
            this.ui = ui;
        }
        public void ManageExit(Signal signal)
        {
            // iterate positions
            foreach (var pos in robot.Positions)
            {
                if (pos.Label != label) continue;
                // debug todo
                robot.Print($"{DateTime.UtcNow} (utc) {label} : position : {pos.SymbolName} | {pos.TradeType} | {pos.VolumeInUnits}");
                if (pos.TradeType == TradeType.Buy && signal.Kind == SignalKind.closeBuySignal)
                    robot.ClosePosition(pos);
                else if (pos.TradeType == TradeType.Sell && signal.Kind == SignalKind.closeSellSignal)
                    robot.ClosePosition(pos);
            }
        }
    }
}