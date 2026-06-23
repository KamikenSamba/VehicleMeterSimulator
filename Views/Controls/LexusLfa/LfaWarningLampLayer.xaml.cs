using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VehicleMeterSimulator.Views.Controls.LexusLfa;

public partial class LfaWarningLampLayer : UserControl
{
    private static readonly Brush OffBackgroundBrush = new SolidColorBrush(Color.FromRgb(8, 8, 10));
    private static readonly Brush OffBorderBrush = new SolidColorBrush(Color.FromRgb(42, 42, 46));
    private static readonly Brush OffTextBrush = new SolidColorBrush(Color.FromRgb(52, 52, 56));
    private static readonly Brush AmberBackgroundBrush = new SolidColorBrush(Color.FromRgb(50, 31, 6));
    private static readonly Brush AmberBorderBrush = new SolidColorBrush(Color.FromRgb(235, 154, 40));
    private static readonly Brush AmberTextBrush = new SolidColorBrush(Color.FromRgb(255, 188, 74));
    private static readonly Brush RedBackgroundBrush = new SolidColorBrush(Color.FromRgb(45, 8, 8));
    private static readonly Brush RedBorderBrush = new SolidColorBrush(Color.FromRgb(215, 48, 48));
    private static readonly Brush RedTextBrush = new SolidColorBrush(Color.FromRgb(255, 86, 86));
    private static readonly Brush BlueBackgroundBrush = new SolidColorBrush(Color.FromRgb(5, 24, 42));
    private static readonly Brush BlueBorderBrush = new SolidColorBrush(Color.FromRgb(80, 172, 245));
    private static readonly Brush BlueTextBrush = new SolidColorBrush(Color.FromRgb(115, 196, 255));
    private static readonly Brush GreenTextBrush = new SolidColorBrush(Color.FromRgb(56, 242, 126));
    private static readonly Brush GreenOffBrush = new SolidColorBrush(Color.FromRgb(22, 48, 29));

    public LfaWarningLampLayer()
    {
        InitializeComponent();
    }

    public void UpdateLamps(
        bool isIgnitionOn,
        bool isEngineRunning,
        bool isSelfCheckActive,
        bool isParkingBrakeApplied,
        bool isShiftUpActive,
        bool isLeftTurnVisible,
        bool isRightTurnVisible,
        bool isTailLampOn,
        bool isHighBeamOn,
        bool isLampTestActive)
    {
        var testAll = isSelfCheckActive || isLampTestActive;

        LeftTurnText.Foreground = testAll || isLeftTurnVisible ? GreenTextBrush : GreenOffBrush;
        RightTurnText.Foreground = testAll || isRightTurnVisible ? GreenTextBrush : GreenOffBrush;
        TailLampText.Foreground = testAll || isTailLampOn ? GreenTextBrush : OffTextBrush;

        if (!isIgnitionOn && !isLampTestActive)
        {
            SetAllOff();
            return;
        }

        if (testAll)
        {
            SetLamp(CheckLampBorder, CheckLampText, true, LampColor.Amber);
            SetLamp(AbsLampBorder, AbsLampText, true, LampColor.Amber);
            SetLamp(OilLampBorder, OilLampText, true, LampColor.Red);
            SetLamp(BatteryLampBorder, BatteryLampText, true, LampColor.Red);
            SetLamp(BrakeLampBorder, BrakeLampText, true, LampColor.Red);
            SetLamp(VscLampBorder, VscLampText, true, LampColor.Amber);
            SetLamp(TrcLampBorder, TrcLampText, true, LampColor.Amber);
            SetLamp(HighBeamLampBorder, HighBeamLampText, true, LampColor.Blue);
            SetLamp(ShiftLampBorder, ShiftLampText, true, LampColor.Amber);
            return;
        }

        SetLamp(CheckLampBorder, CheckLampText, false, LampColor.Amber);
        SetLamp(AbsLampBorder, AbsLampText, false, LampColor.Amber);
        SetLamp(OilLampBorder, OilLampText, !isEngineRunning, LampColor.Red);
        SetLamp(BatteryLampBorder, BatteryLampText, !isEngineRunning, LampColor.Red);
        SetLamp(BrakeLampBorder, BrakeLampText, isParkingBrakeApplied, LampColor.Red);
        SetLamp(VscLampBorder, VscLampText, false, LampColor.Amber);
        SetLamp(TrcLampBorder, TrcLampText, false, LampColor.Amber);
        SetLamp(HighBeamLampBorder, HighBeamLampText, isHighBeamOn, LampColor.Blue);
        SetLamp(ShiftLampBorder, ShiftLampText, isShiftUpActive, LampColor.Amber);
    }

    private void SetAllOff()
    {
        SetLamp(CheckLampBorder, CheckLampText, false, LampColor.Amber);
        SetLamp(AbsLampBorder, AbsLampText, false, LampColor.Amber);
        SetLamp(OilLampBorder, OilLampText, false, LampColor.Red);
        SetLamp(BatteryLampBorder, BatteryLampText, false, LampColor.Red);
        SetLamp(BrakeLampBorder, BrakeLampText, false, LampColor.Red);
        SetLamp(VscLampBorder, VscLampText, false, LampColor.Amber);
        SetLamp(TrcLampBorder, TrcLampText, false, LampColor.Amber);
        SetLamp(HighBeamLampBorder, HighBeamLampText, false, LampColor.Blue);
        SetLamp(ShiftLampBorder, ShiftLampText, false, LampColor.Amber);
    }

    private static void SetLamp(Border border, TextBlock textBlock, bool isOn, LampColor color)
    {
        if (!isOn)
        {
            border.Background = OffBackgroundBrush;
            border.BorderBrush = OffBorderBrush;
            textBlock.Foreground = OffTextBrush;
            return;
        }

        switch (color)
        {
            case LampColor.Amber:
                border.Background = AmberBackgroundBrush;
                border.BorderBrush = AmberBorderBrush;
                textBlock.Foreground = AmberTextBrush;
                break;
            case LampColor.Blue:
                border.Background = BlueBackgroundBrush;
                border.BorderBrush = BlueBorderBrush;
                textBlock.Foreground = BlueTextBrush;
                break;
            default:
                border.Background = RedBackgroundBrush;
                border.BorderBrush = RedBorderBrush;
                textBlock.Foreground = RedTextBrush;
                break;
        }
    }

    private enum LampColor
    {
        Amber,
        Red,
        Blue
    }
}
