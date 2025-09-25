// line trader cbot draws breakout or retracement lines on chart 
// if price crosses breakout line - order towards crossing direction is placed
// if price crosses retracement line - order in opposite direction of the crossing is placed
using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;

namespace Aumchi
{
    public class SignalLines
    {
        private readonly Robot robot;
        private readonly Chart chart;
        public SignalLines(Robot robot)
        {
            this.robot = robot;
            this.chart = robot.Chart;
        }
        public SignalLineDrawManager DrawManager { get; set; }
        public SignalLineRepository SignalLineRepo { get; set; }

        public void Init()
        {
            DrawManager = new SignalLineDrawManager(robot);
            SignalLineRepo = new SignalLineRepository(robot.Chart);
        }
        public class SignalLineDrawManager : IDisposable
        {
            private readonly Algo _algo;
            private readonly Chart _chart;
            private ProtoSignalLine _currentProtoSignalLine;
            private const string StatusTextName = "ProtoSignalLineStatus";

            public SignalLineDrawManager(Algo algo)
            {
                _algo = algo;
                _chart = algo.Chart;
                _chart.DragStart += OnChartDragStart;
                _chart.DragEnd += OnChartDragEnd;
                _chart.Drag += OnChartDrag;
            }
            private void OnChartDragStart(ChartDragEventArgs args)
            {
                if (args.ChartArea != args.Chart)
                    return;
                var signalType = GetSignalType(args);
                if (signalType.HasValue)
                {
                    _chart.IsScrollingEnabled = false;
                    _currentProtoSignalLine = new ProtoSignalLine(_algo, signalType, args.TimeValue, args.YValue);
                    UpdateStatus();
                }
                else
                {
                    _currentProtoSignalLine = null;
                }
            }
            private void OnChartDragEnd(ChartDragEventArgs args)
            {
                if (_currentProtoSignalLine != null)
                {
                    _currentProtoSignalLine.Complete(args.TimeValue, args.YValue);
                    _currentProtoSignalLine = null;

                    _chart.IsScrollingEnabled = true;
                    _chart.RemoveObject(StatusTextName);
                }
            }
            private void OnChartDrag(ChartDragEventArgs args)
            {
                if (_currentProtoSignalLine != null)
                {
                    _currentProtoSignalLine.Update(args.TimeValue, args.YValue);
                    UpdateStatus();
                }
            }
            private void UpdateStatus()
            {
                var text = string.Format("Creating {0} line", _currentProtoSignalLine.LineLabel);
                _chart.DrawStaticText(StatusTextName, text, VerticalAlignment.Top, HorizontalAlignment.Left, _chart.ColorSettings.ForegroundColor);
            }
            private SignalType? GetSignalType(ChartDragEventArgs args)
            {
                if (args.CtrlKey && !args.ShiftKey)
                    return SignalType.Breakout;
                if (!args.CtrlKey && args.ShiftKey)
                    return SignalType.Retracement;

                return null;
            }
            public void Dispose()
            {
                _chart.DragStart -= OnChartDragStart;
                _chart.DragEnd -= OnChartDragEnd;
                _chart.Drag -= OnChartDrag;
            }
        }
        public class SignalLineRepository : IDisposable
        {
            private readonly Chart _chart;
            private readonly Dictionary<string, SignalLine> _signalLines;

            public SignalLineRepository(Chart chart)
            {
                _chart = chart;
                _signalLines = new Dictionary<string, SignalLine>();
                foreach (var chartTrendLine in chart.FindAllObjects<ChartTrendLine>())
                    TryAddSignalLine(chartTrendLine);
                _chart.ObjectsAdded += OnChartObjectAdded;
                _chart.ObjectsRemoved += OnChartObjectRemoved;
                _chart.ObjectsUpdated += OnChartObjectUpdated;
            }
            public IEnumerable<SignalLine> GetLines()
            {
                return _signalLines.Values;
            }
            private void TryAddSignalLine(ChartObject chartObject)
            {
                var chartTrendLine = chartObject as ChartTrendLine;
                if (chartTrendLine != null && IsSignalLine(chartTrendLine))
                    _signalLines.Add(chartTrendLine.Name, CreateSignalLine(chartTrendLine));
            }
            private void TryRemoveSignalLine(ChartObject chartObject)
            {
                if (_signalLines.ContainsKey(chartObject.Name))
                    _signalLines.Remove(chartObject.Name);
            }
            private void OnChartObjectAdded(ChartObjectsAddedEventArgs args)
            {
                if (args.Area != args.Chart)
                    return;
                foreach (var chartObject in args.ChartObjects)
                    TryAddSignalLine(chartObject);
            }
            private void OnChartObjectUpdated(ChartObjectsUpdatedEventArgs args)
            {
                if (args.Area != args.Chart)
                    return;
                foreach (var chartObject in args.ChartObjects)
                {
                    TryRemoveSignalLine(chartObject);
                    TryAddSignalLine(chartObject);
                }
            }
            private void OnChartObjectRemoved(ChartObjectsRemovedEventArgs args)
            {
                if (args.Area != args.Chart)
                    return;
                foreach (var chartObject in args.ChartObjects)
                    TryRemoveSignalLine(chartObject);
            }
            private SignalLine CreateSignalLine(ChartTrendLine chartTrendLine)
            {
                var signalType = GetLineSignalType(chartTrendLine);
                var tradeType = GetLineTradeType(chartTrendLine);
                var signalLine = new SignalLine(chartTrendLine, signalType, tradeType);
                return signalLine;
            }
            private bool IsSignalLine(ChartTrendLine line)
            {
                return SignalLineLabels.AllLabels.Contains(line.Comment);
            }
            private SignalType GetLineSignalType(ChartTrendLine line)
            {
                var comment = line.Comment;
                if (comment == SignalLineLabels.BuyBreakoutLabel || comment == SignalLineLabels.SellBreakoutLabel)
                    return SignalType.Breakout;
                if (comment == SignalLineLabels.BuyRetraceLabel || comment == SignalLineLabels.SellRetraceLabel)
                    return SignalType.Retracement;
                throw new ArgumentException();
            }
            private TradeType GetLineTradeType(ChartTrendLine line)
            {
                var comment = line.Comment;
                if (comment == SignalLineLabels.BuyBreakoutLabel || comment == SignalLineLabels.BuyRetraceLabel)
                    return TradeType.Buy;
                if (comment == SignalLineLabels.SellBreakoutLabel || comment == SignalLineLabels.SellRetraceLabel)
                    return TradeType.Sell;
                throw new ArgumentException();
            }
            public void Dispose()
            {
                _chart.ObjectsAdded -= OnChartObjectAdded;
                _chart.ObjectsRemoved -= OnChartObjectRemoved;
                _chart.ObjectsUpdated -= OnChartObjectUpdated;
            }
        }
        public class ProtoSignalLine
        {
            private static readonly Color BuyLineColor = Color.DodgerBlue;
            private static readonly Color SellLineColor = Color.Red;

            private readonly Chart _chart;
            private readonly ChartTrendLine _line;
            private readonly SignalType? _signalType;
            private readonly Symbol _symbol;

            public ProtoSignalLine(Algo algo, SignalType? signalType, DateTime startTimeValue, double startYValue)
            {
                _chart = algo.Chart;
                _signalType = signalType;
                _symbol = algo.Symbol;
                _line = _chart.DrawTrendLine(string.Format("signalLines {0:N}", Guid.NewGuid()), startTimeValue, startYValue, startTimeValue, startYValue, LineColor);
                _line.ExtendToInfinity = false;
                _line.Thickness = 2;
                _line.IsInteractive = true;
            }
            private bool IsPriceAboveLine
            {
                get { return _line != null && _symbol.Bid >= _line.CalculateY(_chart.Bars.Count - 1); }
            }
            private TradeType LineTradeType
            {
                get { return _signalType == SignalType.Breakout ? (IsPriceAboveLine ? TradeType.Sell : TradeType.Buy) : (IsPriceAboveLine ? TradeType.Buy : TradeType.Sell); }
            }
            private Color LineColor
            {
                get { return LineTradeType == TradeType.Buy ? BuyLineColor : SellLineColor; }
            }
            public string LineLabel
            {
                get { return _signalType == SignalType.Breakout ? (LineTradeType == TradeType.Buy ? SignalLineLabels.BuyBreakoutLabel : SignalLineLabels.SellBreakoutLabel) : (LineTradeType == TradeType.Buy ? SignalLineLabels.BuyRetraceLabel : SignalLineLabels.SellRetraceLabel); }
            }
            private bool CanComplete
            {
                get { return _line.Time1 != _line.Time2 || Math.Abs(_line.Y1 - _line.Y2) >= _symbol.PipValue; }
            }
            public void Update(DateTime timeValue, double yValue)
            {
                _line.Time2 = timeValue;
                _line.Y2 = yValue;
                _line.Color = LineColor;
            }
            public void Complete(DateTime timeValue, double yValue)
            {
                Update(timeValue, yValue);
                if (CanComplete)
                {
                    _line.Comment = LineLabel;
                    _line.IsInteractive = true;
                }
                else
                {
                    _chart.RemoveObject(_line.Name);
                }
            }
        }
        public class SignalLine
        {
            public TradeType TradeType { get; private set; }
            public SignalType SignalType { get; private set; }
            private readonly ChartTrendLine _chartTrendLine;
            public SignalLine(ChartTrendLine chartTrendLine, SignalType signalType, TradeType tradeType)
            {
                _chartTrendLine = chartTrendLine;
                SignalType = signalType;
                TradeType = tradeType;
            }
            public void MarkAsExecuted()
            {
                _chartTrendLine.Thickness = 1;
                _chartTrendLine.Color = Color.FromArgb(150, _chartTrendLine.Color);
            }
            public bool CanExecute(double price, int barIndex)
            {
                if (_chartTrendLine.Thickness <= 1)
                    return false;
                var lineValue = _chartTrendLine.CalculateY(barIndex);
                switch (SignalType)
                {
                    case SignalType.Breakout:
                        return CanExecuteForBreakout(price, lineValue);
                    case SignalType.Retracement:
                        return CanExecuteForRetrace(price, lineValue);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            private bool CanExecuteForBreakout(double price, double lineValue)
            {
                return TradeType == TradeType.Buy && price > lineValue || TradeType == TradeType.Sell && price < lineValue;
            }
            private bool CanExecuteForRetrace(double price, double lineValue)
            {
                return TradeType == TradeType.Buy && price <= lineValue || TradeType == TradeType.Sell && price >= lineValue;
            }
        }
        public enum SignalType
        {
            Breakout,
            Retracement
        }
        public static class SignalLineLabels
        {
            public const string BuyBreakoutLabel = "BuyBreakout";
            public const string SellBreakoutLabel = "SellBreakout";
            public const string BuyRetraceLabel = "BuyRetracement";
            public const string SellRetraceLabel = "SellRetracement";
            public static readonly string[] AllLabels =
            {
            BuyBreakoutLabel,
            SellBreakoutLabel,
            BuyRetraceLabel,
            SellRetraceLabel
        };
        }
    }
}