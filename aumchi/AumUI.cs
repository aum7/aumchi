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
            tradingBlock = new TextBlock { Text = "not trading", ForegroundColor = AumStyles.clrInactive };
            trailingBlock = new TextBlock { Text = "not trailing", ForegroundColor = AumStyles.clrInactive };
            statusPanel.AddChild(tradingBlock);
            statusPanel.AddChild(trailingBlock);

            robot.Chart.AddControl(statusPanel);
        }
        // update status panel
        public void UpdateStatusUI(bool enableTrading, bool isTrailing, TradeType? armedTradeType)
        {
            // trading block
            if (enableTrading && armedTradeType.HasValue)
            {
                tradingBlock.Text = "trading";
                tradingBlock.ForegroundColor = armedTradeType.Value == TradeType.Buy ?
                AumStyles.clrBuy : AumStyles.clrSell;
            }
            else
            {
                tradingBlock.Text = "not trading";
                tradingBlock.ForegroundColor = AumStyles.clrInactive;
            }
            // trailing block
            if (isTrailing)
            {
                trailingBlock.Text = "trailing";
                trailingBlock.ForegroundColor = armedTradeType == TradeType.Buy ? AumStyles.clrBuy : AumStyles.clrSell;
            }
            else
            {
                trailingBlock.Text = "not trailing";
                trailingBlock.ForegroundColor = AumStyles.clrInactive;
            }
        }
    }
}