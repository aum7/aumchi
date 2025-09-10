// aumui.cs
using System;
using cAlgo.API;

namespace Aumchi
{
    public class AumUI
    {
        private readonly Robot robot;
        private readonly Func<bool> getEnableTrading;
        private readonly Action<bool> setEnableTrading;
        // cache status to refresh ui after toggle
        private OrderType? currentOrderType;
        // status panel / ui
        private StackPanel statusPanel;
        private Button btnTrade;
        private Button btnTrail;

        public AumUI(Robot robot, Func<bool> getEnableTrading, Action<bool> setEnableTrading)
        {
            this.robot = robot;
            this.getEnableTrading = getEnableTrading;
            this.setEnableTrading = setEnableTrading;
            InitStatusUI();
            UpdateStatusUI(false, null);
        }
        // initialize status panel
        private void InitStatusUI()
        {
            statusPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = 10
            };
            btnTrade = new Button
            {
                Text = "TRADE",
                BackgroundColor = AumStyle.clrInactive,
                Margin = new Thickness(0, 0, 0, 5)
            };
            btnTrade.Click += delegate
            {
                // toggle enable trading
                bool newVal = !getEnableTrading();
                setEnableTrading(newVal);
                UpdateStatusUI(false, null);
            };
            btnTrail = new Button
            {
                Text = "TRAIL",
                BackgroundColor = AumStyle.clrInactive,
            };
            statusPanel.AddChild(btnTrade);
            statusPanel.AddChild(btnTrail);

            robot.Chart.AddControl(statusPanel);
        }
        // update status panel
        public void UpdateStatusUI(bool isTrail, OrderType? orderType)
        {
            // cache for click handler
            currentOrderType = orderType;
            bool enabled = getEnableTrading();
            // trading block
            if (enabled && orderType.HasValue && (orderType.Value == OrderType.buy || orderType.Value == OrderType.sell))
            {
                btnTrade.BackgroundColor = orderType.Value == OrderType.buy ? AumStyle.clrBuy : AumStyle.clrSell;
            }
            else
            {
                // indicate alert or close order
                btnTrade.BackgroundColor = orderType.Value switch
                {
                    OrderType.alertBuy or OrderType.alertSell => AumStyle.clrAlert,
                    OrderType.closeBuy or OrderType.closeSell => AumStyle.clrClose,
                    _ => AumStyle.clrInactive
                };
            }
            // trail button 
            if (isTrail && orderType.HasValue && orderType.Value != OrderType.none)
            {
                btnTrail.BackgroundColor = orderType.Value switch
                {
                    OrderType.buy => AumStyle.clrBuy,
                    OrderType.sell => AumStyle.clrSell,
                    _ => AumStyle.clrInactive
                };
            }
        }
    }
}