using System.Windows.Controls;
using System.Windows.Media;

namespace VehicleMeterSimulator.Views.Controls.NissanNoteE13;

public partial class NoteE13WarningLampLayer : UserControl
{
    private static readonly Brush OffBackgroundBrush = new SolidColorBrush(Color.FromRgb(7, 8, 10));
    private static readonly Brush OffBorderBrush = new SolidColorBrush(Color.FromRgb(42, 44, 48));
    private static readonly Brush OffTextBrush = new SolidColorBrush(Color.FromRgb(52, 56, 60));
    private static readonly Brush GreenBackgroundBrush = new SolidColorBrush(Color.FromRgb(5, 34, 18));
    private static readonly Brush GreenBorderBrush = new SolidColorBrush(Color.FromRgb(46, 220, 104));
    private static readonly Brush GreenTextBrush = new SolidColorBrush(Color.FromRgb(82, 255, 140));
    private static readonly Brush AmberBackgroundBrush = new SolidColorBrush(Color.FromRgb(48, 32, 5));
    private static readonly Brush AmberBorderBrush = new SolidColorBrush(Color.FromRgb(230, 160, 48));
    private static readonly Brush AmberTextBrush = new SolidColorBrush(Color.FromRgb(255, 195, 88));
    private static readonly Brush RedBackgroundBrush = new SolidColorBrush(Color.FromRgb(45, 8, 8));
    private static readonly Brush RedBorderBrush = new SolidColorBrush(Color.FromRgb(215, 48, 48));
    private static readonly Brush RedTextBrush = new SolidColorBrush(Color.FromRgb(255, 86, 86));
    private static readonly Brush BlueBackgroundBrush = new SolidColorBrush(Color.FromRgb(6, 26, 45));
    private static readonly Brush BlueBorderBrush = new SolidColorBrush(Color.FromRgb(83, 177, 245));
    private static readonly Brush BlueTextBrush = new SolidColorBrush(Color.FromRgb(124, 205, 255));

    public NoteE13WarningLampLayer()
    {
        InitializeComponent();
    }

    public void UpdateLamps(
        bool isPowerOn,
        bool isSelfCheckActive,
        bool isReady,
        bool isElectricParkingBrakeOn,
        bool isAutoHoldEnabled,
        bool isAutoHoldHolding,
        bool isLeftTurnVisible,
        bool isRightTurnVisible,
        bool isTailLampOn,
        bool isHighBeamOn,
        bool isChargeModeOn,
        bool isMannerModeOn)
    {
        var test = isSelfCheckActive;
        if (!isPowerOn && !test)
        {
            SetAllOff();
            return;
        }

        SetLamp(ReadyLampBorder, ReadyLampText, test || isReady, LampColor.Green);
        SetLamp(ParkingLampBorder, ParkingLampText, test || isElectricParkingBrakeOn, LampColor.Red);
        SetLamp(AutoHoldLampBorder, AutoHoldLampText, test || isAutoHoldEnabled, isAutoHoldHolding ? LampColor.Green : LampColor.White);
        SetLamp(LeftTurnLampBorder, LeftTurnLampText, test || isLeftTurnVisible, LampColor.Green);
        SetLamp(RightTurnLampBorder, RightTurnLampText, test || isRightTurnVisible, LampColor.Green);
        SetLamp(TailLampBorder, TailLampText, test || isTailLampOn, LampColor.Green);
        SetLamp(HighBeamLampBorder, HighBeamLampText, test || isHighBeamOn, LampColor.Blue);
        SetLamp(BatteryLampBorder, BatteryLampText, test || !isReady, LampColor.Amber);
        SetLamp(BrakeLampBorder, BrakeLampText, test || isElectricParkingBrakeOn, LampColor.Red);
        SetLamp(AbsLampBorder, AbsLampText, test, LampColor.Amber);
        SetLamp(ChargeLampBorder, ChargeLampText, test || isChargeModeOn, LampColor.Green);
        SetLamp(MannerLampBorder, MannerLampText, test || isMannerModeOn, LampColor.Green);
    }

    private void SetAllOff()
    {
        SetLamp(ReadyLampBorder, ReadyLampText, false, LampColor.Green);
        SetLamp(ParkingLampBorder, ParkingLampText, false, LampColor.Red);
        SetLamp(AutoHoldLampBorder, AutoHoldLampText, false, LampColor.White);
        SetLamp(LeftTurnLampBorder, LeftTurnLampText, false, LampColor.Green);
        SetLamp(RightTurnLampBorder, RightTurnLampText, false, LampColor.Green);
        SetLamp(TailLampBorder, TailLampText, false, LampColor.Green);
        SetLamp(HighBeamLampBorder, HighBeamLampText, false, LampColor.Blue);
        SetLamp(BatteryLampBorder, BatteryLampText, false, LampColor.Amber);
        SetLamp(BrakeLampBorder, BrakeLampText, false, LampColor.Red);
        SetLamp(AbsLampBorder, AbsLampText, false, LampColor.Amber);
        SetLamp(ChargeLampBorder, ChargeLampText, false, LampColor.Green);
        SetLamp(MannerLampBorder, MannerLampText, false, LampColor.Green);
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
            case LampColor.Green:
                border.Background = GreenBackgroundBrush;
                border.BorderBrush = GreenBorderBrush;
                textBlock.Foreground = GreenTextBrush;
                break;
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
            case LampColor.White:
                border.Background = OffBackgroundBrush;
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 230, 226));
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(235, 240, 236));
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
        Green,
        Amber,
        Red,
        Blue,
        White
    }
}
