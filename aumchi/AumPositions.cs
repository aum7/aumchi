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

                bool canClose = (pos.TradeType == TradeType.Buy && signal.Kind == SignalKind.closeBuySignal) || (pos.TradeType == TradeType.Sell && signal.Kind == SignalKind.closeSellSignal);
                if (!canClose) continue;
                var result = robot.ClosePosition(pos);
                var time = DateTime.UtcNow;
                if (result.IsSuccessful)
                {
                    var price = result.Position?.CurrentPrice;
                    robot.Print($"{time} (utc) {label} : closed position @ {price}");
                    // debug
                    Console.WriteLine($"[debug] result type : {result.GetType()}");
                    foreach (var prop in result.GetType().GetProperties()) Console.WriteLine($"prop : {prop.Name} | value : {prop.GetValue(result)}");
                }
                else robot.Print($"{time} (utc) {label} : close order execution failed : {result.Error}");
            }
        }
    }
}