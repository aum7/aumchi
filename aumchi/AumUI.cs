// aumui.cs
using System;
using cAlgo.API;

namespace Aumchi
{
    public class AumUI
    {
        private readonly Robot robot;
        // status panel / ui
        private StackPanel statusPanel;
        private Button btnTrade;
        private Button btnTrail;

        public event Action OnTradeButtonClick;
        public AumUI(Robot robot)
        {
            this.robot = robot;
            InitStatusUI();
            UpdateStatusUI(false, null, null);
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
            btnTrade.Click += (args) => OnTradeButtonClick?.Invoke();
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
        public void UpdateStatusUI(bool EnableTrading, OrderType? tradeType, OrderType? trailType)
        {
            // early exit
            if (btnTrade == null || btnTrail == null) return;
            // trade button
            if (!EnableTrading) btnTrade.BackgroundColor = AumStyle.clrInactive;
            else
            {
                if (tradeType == null || tradeType == OrderType.none)
                    btnTrade.BackgroundColor = AumStyle.clrTradingEnabled;
                else
                {
                    btnTrade.BackgroundColor = tradeType == OrderType.buy
                    ? AumStyle.clrBuy
                    : AumStyle.clrSell;
                    ;
                }
            }
            if (trailType.HasValue)
            {
                btnTrail.BackgroundColor = trailType switch
                {
                    OrderType.alertBuy or OrderType.closeBuy => AumStyle.clrBuy,
                    OrderType.alertSell or OrderType.closeSell => AumStyle.clrSell,
                    _ => AumStyle.clrInactive
                };
            }
            else btnTrail.BackgroundColor = AumStyle.clrInactive;
        }
    }
}