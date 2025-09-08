// aumpositions.cs
using cAlgo.API;

namespace Aumchi
{
    public class AumPositions
    {
        private readonly Robot robot;
        private readonly bool enableTrading;
        private const string Label = "aumchi";
        public AumPositions(Robot robot, bool enableTrading)
        {
            this.robot = robot;
            this.enableTrading = enableTrading;
        }
        public void Manage()
        {
            if (!enableTrading) return;
            // iterate positions
            foreach (var pos in robot.Positions)
            {
                if (pos.Label != Label) continue;
                // todo move stoploss to breakeven
                // trail
            }
        }
    }
}