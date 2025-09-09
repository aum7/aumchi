// aumui.cs
using cAlgo.API;

namespace Aumchi
{
    public class AumUI
    {
        private readonly Robot robot;
        private readonly bool enableTrading;
        // status panel / ui
        private StackPanel statusPanel;
        private TextBlock tradingBlock;
        private TextBlock trailingBlock;

        public AumUI(Robot robot, bool enableTrading)
        {
            this.robot = robot;
            this.enableTrading = enableTrading;
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
            tradingBlock = new TextBlock { Text = "not trading", ForegroundColor = AumStyle.clrInactive };
            trailingBlock = new TextBlock { Text = "not trailing", ForegroundColor = AumStyle.clrInactive };
            statusPanel.AddChild(tradingBlock);
            statusPanel.AddChild(trailingBlock);

            robot.Chart.AddControl(statusPanel);
        }
        // update status panel
        public void UpdateStatusUI(bool isTrailing, TradeType? armedTradeType)
        {
            // trading block
            if (enableTrading && armedTradeType.HasValue)
            {
                tradingBlock.Text = "trading";
                tradingBlock.ForegroundColor = armedTradeType.Value == TradeType.Buy ?
                AumStyle.clrBuy : AumStyle.clrSell;
            }
            else
            {
                tradingBlock.Text = "not trading";
                tradingBlock.ForegroundColor = AumStyle.clrInactive;
            }
            // trailing block
            if (isTrailing)
            {
                trailingBlock.Text = "trailing";
                trailingBlock.ForegroundColor = armedTradeType == TradeType.Buy ? AumStyle.clrBuy : AumStyle.clrSell;
            }
            else
            {
                trailingBlock.Text = "not trailing";
                trailingBlock.ForegroundColor = AumStyle.clrInactive;
            }
        }
    }
}