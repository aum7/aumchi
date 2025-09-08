// aumui.cs
using cAlgo.API;

namespace Aumchi
{
    public class AumUI
    {
        private readonly Robot robot;
        // status panel / ui
        private StackPanel statusPanel;
        private TextBlock tradingBlock;
        private TextBlock trailingBlock;
        private readonly Color inactiveColor = Color.DarkGray;
        private readonly Color buyColor = Color.DodgerBlue;
        private readonly Color sellColor = Color.Red;

        public AumUI(Robot robot)
        {
            this.robot = robot;
            InitStatusUI();
        }
        // initialize status panel
        private void InitStatusUI()
        {
            statusPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = 10
            };
            tradingBlock = new TextBlock { Text = "not trading", ForegroundColor = inactiveColor };
            trailingBlock = new TextBlock { Text = "not trailing", ForegroundColor = inactiveColor };
            statusPanel.AddChild(tradingBlock);
            statusPanel.AddChild(trailingBlock);

            robot.Chart.AddControl(statusPanel);
        }
        // update status panel
        public void UpdateStatusUI(bool isTrailing, TradeType? armedTradeType, bool enableTrading)
        {
            // trading block
            if (enableTrading && armedTradeType.HasValue)
            {
                tradingBlock.Text = "trading";
                tradingBlock.ForegroundColor = armedTradeType == TradeType.Buy ?
                buyColor : sellColor;
            }
            else
            {
                tradingBlock.Text = "not trading";
                tradingBlock.ForegroundColor = inactiveColor;
            }
            // trailing block
            if (isTrailing)
            {
                trailingBlock.Text = "trailing";
                trailingBlock.ForegroundColor = armedTradeType == TradeType.Buy ? buyColor : sellColor;
            }
            else
            {
                trailingBlock.Text = "not trailing";
                trailingBlock.ForegroundColor = inactiveColor;
            }
        }
    }
}