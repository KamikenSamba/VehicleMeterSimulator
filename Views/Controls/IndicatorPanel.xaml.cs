using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Media;

namespace VehicleMeterSimulator.Views.Controls;

public partial class IndicatorPanel : UserControl
{
    private static readonly Brush OffBackgroundBrush = new SolidColorBrush(Color.FromRgb(9, 9, 11));
    private static readonly Brush OffBorderBrush = new SolidColorBrush(Color.FromRgb(43, 43, 45));
    private static readonly Brush OffTextBrush = new SolidColorBrush(Color.FromRgb(52, 52, 56));
    private static readonly Brush AmberBackgroundBrush = new SolidColorBrush(Color.FromRgb(50, 31, 6));
    private static readonly Brush AmberBorderBrush = new SolidColorBrush(Color.FromRgb(235, 154, 40));
    private static readonly Brush AmberTextBrush = new SolidColorBrush(Color.FromRgb(255, 188, 74));
    private static readonly Brush RedBackgroundBrush = new SolidColorBrush(Color.FromRgb(45, 8, 8));
    private static readonly Brush RedBorderBrush = new SolidColorBrush(Color.FromRgb(215, 48, 48));
    private static readonly Brush RedTextBrush = new SolidColorBrush(Color.FromRgb(255, 86, 86));

    public IndicatorPanel()
    {
        InitializeComponent();
    }

    public void UpdateIndicators(
        bool isIgnitionOn,
        bool isEngineRunning,
        bool isSelfCheckActive,
        bool shouldShowShiftUp)
    {
        if (!isIgnitionOn)
        {
            SetAllWarningIndicatorsOff();
        }
        else if (isSelfCheckActive)
        {
            SetWarningIndicator(CheckIndicatorBorder, CheckIndicatorText, true, IndicatorColor.Amber);
            SetWarningIndicator(AbsIndicatorBorder, AbsIndicatorText, true, IndicatorColor.Amber);
            SetWarningIndicator(OilIndicatorBorder, OilIndicatorText, true, IndicatorColor.Red);
            SetWarningIndicator(BatteryIndicatorBorder, BatteryIndicatorText, true, IndicatorColor.Red);
            SetWarningIndicator(BrakeIndicatorBorder, BrakeIndicatorText, true, IndicatorColor.Red);
        }
        else if (!isEngineRunning)
        {
            SetWarningIndicator(CheckIndicatorBorder, CheckIndicatorText, false, IndicatorColor.Amber);
            SetWarningIndicator(AbsIndicatorBorder, AbsIndicatorText, false, IndicatorColor.Amber);
            SetWarningIndicator(OilIndicatorBorder, OilIndicatorText, true, IndicatorColor.Red);
            SetWarningIndicator(BatteryIndicatorBorder, BatteryIndicatorText, true, IndicatorColor.Red);
            SetWarningIndicator(BrakeIndicatorBorder, BrakeIndicatorText, false, IndicatorColor.Red);
        }
        else
        {
            SetAllWarningIndicatorsOff();
        }

        SetWarningIndicator(ShiftUpIndicatorBorder, ShiftUpIndicatorText, shouldShowShiftUp, IndicatorColor.Red);
    }

    private void SetAllWarningIndicatorsOff()
    {
        SetWarningIndicator(CheckIndicatorBorder, CheckIndicatorText, false, IndicatorColor.Amber);
        SetWarningIndicator(AbsIndicatorBorder, AbsIndicatorText, false, IndicatorColor.Amber);
        SetWarningIndicator(OilIndicatorBorder, OilIndicatorText, false, IndicatorColor.Red);
        SetWarningIndicator(BatteryIndicatorBorder, BatteryIndicatorText, false, IndicatorColor.Red);
        SetWarningIndicator(BrakeIndicatorBorder, BrakeIndicatorText, false, IndicatorColor.Red);
    }

    private static void SetWarningIndicator(Border border, TextBlock textBlock, bool isOn, IndicatorColor indicatorColor)
    {
        if (!isOn)
        {
            border.Background = OffBackgroundBrush;
            border.BorderBrush = OffBorderBrush;
            textBlock.Foreground = OffTextBrush;
            AutomationProperties.SetName(border, $"{textBlock.Text} OFF");
            AutomationProperties.SetName(textBlock, $"{textBlock.Text} OFF");
            return;
        }

        AutomationProperties.SetName(border, $"{textBlock.Text} ON");
        AutomationProperties.SetName(textBlock, $"{textBlock.Text} ON");

        if (indicatorColor == IndicatorColor.Amber)
        {
            border.Background = AmberBackgroundBrush;
            border.BorderBrush = AmberBorderBrush;
            textBlock.Foreground = AmberTextBrush;
            return;
        }

        border.Background = RedBackgroundBrush;
        border.BorderBrush = RedBorderBrush;
        textBlock.Foreground = RedTextBrush;
    }

    private enum IndicatorColor
    {
        Amber,
        Red
    }
}
