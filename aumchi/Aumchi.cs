// aumchi.cs
using System;
using cAlgo.API;

namespace Aumchi
{
    public static class AumStyle
    {
        public static readonly Color clrBuy = Color.DodgerBlue;
        public static readonly Color clrSell = Color.Red;
        public static readonly Color clrAlert = Color.Magenta;
        public static readonly Color clrClose = Color.LimeGreen;
        public static readonly Color clrInactive = Color.DarkGray;
    }
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
- alert (magenta)
- trail : will trail t-line as horizontal line
trail is used in combination with order
ie 'buy trail' or 'trail close'")]
        public string ManualText { get; set; }
        [Parameter("enable trading", DefaultValue = false)]
        public bool EnableTrading { get; set; }
        // only trigger line order once
        [Parameter("trigger line only once", DefaultValue = true)]
        public bool TriggerOrderOnce { get; set; }
        // if set to 0 > use x bars back instead of pips
        [Parameter("trail order line pips", DefaultValue = 41.0)]
        public double TrailOrderLinePips { get; set; }
        [Parameter("trail order line X bars back", DefaultValue = 3)]
        public int TrailOrderLineBarsBack { get; set; }
        [Parameter("trail order line on tf", DefaultValue = "1h")]
        public TimeFrame TrailOrderLineTf { get; set; }
        [Parameter("stoploss (pips)", DefaultValue = 200.0)]
        public double StoplossPips { get; set; }
        [Parameter("lots size", DefaultValue = 0.01)]
        public double LotSize { get; set; }
        // alert settings (aumchi\aumchi\sounds\alert | bells | siren
        [Parameter("alert file", DefaultValue = @"C:\Users\mua\Documents\cAlgo\Sources\Robots\aumchi\aumchi\sounds\alert.mp3")]
        public string SoundFile { get; set; }

        private AumUI ui;
        private AumSignals signals;
        private AumTrader trader;
        private AumPositions positions;
        protected override void OnStart()
        {
            Print($"{DateTime.UtcNow} (utc) aumchi started");
            // debug
            // Print($"ticksize : {Symbol.TickSize}"); // 0,01
            // Print($"pipsize : {Symbol.PipSize}"); // 0,1
            // debug end
            ui = new AumUI(this, () => EnableTrading, v => EnableTrading = v);
            signals = new AumSignals(this, TriggerOrderOnce, TrailOrderLinePips, TrailOrderLineBarsBack, TrailOrderLineTf, SoundFile, ui);
            signals.OnSignal += HandleSignal;
            trader = new AumTrader(this, EnableTrading, StoplossPips, LotSize);
            positions = new AumPositions(this, ui);
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
        }
        private void HandleSignal(Signal signal)
        {
            trader.ManageEntry(signal);
            positions.ManageExit(signal);
        }
        protected override void OnStop()
        {
            // unsubscribe all events
            Chart.ObjectsUpdated -= Chart_ObjectsUpdated;
            Chart.ObjectsRemoved -= Chart_ObjectsRemoved;
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