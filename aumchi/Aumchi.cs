// aumchi.cs
using System;
using cAlgo.API;

namespace Aumchi
{
    [Robot(AccessRights = AccessRights.None)]
    public class Aumchi : Robot
    {
        // external parameters
        [Parameter("info", DefaultValue =
        @"put trend line on chart
put orders in line comment
when line is crossed by price
order will be executed
available orders :
- buy (blue)
- sell (red)
- close (green)
- alert (pink)
- trail : will trail t-line as horizontal line
trail is used in combination with order
ie 'buy trail' or 'trail close'")]
        public string ManualText { get; set; }
        [Parameter("enable trading", DefaultValue = false)]
        public bool EnableTrading { get; set; }
        [Parameter("trail order line pips", DefaultValue = 41.0)]
        public double TrailOrderLinePips { get; set; }
        [Parameter("stoploss pips", DefaultValue = 100.0)]
        public double StoplossPips { get; set; }

        private AumUI ui;
        private AumSignals signals;
        private AumTrader trader;
        private AumPositions positions;
        protected override void OnStart()
        {
            Print($"{DateTime.UtcNow} (utc) aumchi started");
            ui = new AumUI(this);
            signals = new AumSignals(this, TrailOrderLinePips, EnableTrading, ui);
            trader = new AumTrader(this, EnableTrading, StoplossPips);
            positions = new AumPositions(this, EnableTrading);
            signals.OnSignal += HandleSignal;
            // subscribe to chart object events
            Chart.ObjectsUpdated += Chart_ObjectsUpdated;
            Chart.ObjectsRemoved += Chart_ObjectsRemoved;
            // initialize lines already on chart
            signals.ScanLinesOnChart();
        }
        protected override void OnTick()
        {
            // keep signal handling separate from trading / management
            signals.Update();
            positions.Manage();
        }
        private void HandleSignal(Signal signal)
        {
            trader.ExecuteSignal(signal);
        }
        protected override void OnStop()
        {
            // unsubscribe all events
            Chart.ObjectsUpdated -= Chart_ObjectsUpdated;
            Chart.ObjectsRemoved -= Chart_ObjectsRemoved; // todo ???
            Print($"{DateTime.UtcNow} (utc) aumchi stopped");
        }
        // chart event functions
        private void Chart_ObjectsUpdated(ChartObjectsUpdatedEventArgs args)
        {
            foreach (var chartObject in args.ChartObjects)
            {
                if (chartObject is ChartTrendLine trendLine)
                {
                    signals.TLineUpdate(trendLine);
                }
            }
        }
        private void Chart_ObjectsRemoved(ChartObjectsRemovedEventArgs args)
        {
            foreach (var chartObject in args.ChartObjects)
            {
                if (chartObject.ObjectType is ChartObjectType.TrendLine)
                {
                    signals.StopTrackingLine(chartObject.Name);
                }
            }
        }
    }
}