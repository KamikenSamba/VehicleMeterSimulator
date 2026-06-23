using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VehicleMeterSimulator.Models;

namespace VehicleMeterSimulator.Views.Controls.NissanNoteE13;

public partial class NoteE13InstrumentCluster : UserControl
{
    private const double PowerStartAngle = -128.0;
    private const double PowerSweepAngle = 256.0;
    private const double PowerCenterX = 160.0;
    private const double PowerCenterY = 153.0;
    private const double PowerTickOuterRadius = 136.0;
    private const double PowerTickInnerRadius = 121.0;

    private static readonly Brush OffBrush = new SolidColorBrush(Color.FromRgb(119, 127, 132));
    private static readonly Brush ReadyBrush = new SolidColorBrush(Color.FromRgb(89, 255, 142));
    private static readonly Brush NormalBrush = new SolidColorBrush(Color.FromRgb(232, 236, 232));
    private static readonly Brush SportBrush = new SolidColorBrush(Color.FromRgb(255, 185, 80));
    private static readonly Brush EcoBrush = new SolidColorBrush(Color.FromRgb(93, 225, 135));
    private static readonly Brush ChargeBrush = new SolidColorBrush(Color.FromRgb(65, 215, 228));
    private static readonly Brush PowerBrush = new SolidColorBrush(Color.FromRgb(238, 244, 244));

    public NoteE13InstrumentCluster()
    {
        InitializeComponent();
        BuildPowerMeterTicks();
    }

    public void UpdateCluster(
        NissanNoteE13RuntimeState state,
        string systemMessage,
        bool isSelfCheckActive,
        bool isLeftTurnVisible,
        bool isRightTurnVisible,
        bool isTailLampOn,
        bool isHighBeamOn,
        bool isPreviewPanelVisible,
        string turnSignalAudioStatus,
        string reverseAudioStatus,
        string forwardApproachAudioStatus)
    {
        PowerStateText.Text = state.PowerState == E13PowerState.Ready
            ? "READY"
            : $"POWER {state.PowerStateDisplay}";
        PowerStateText.Foreground = state.PowerState == E13PowerState.Ready ? ReadyBrush : OffBrush;

        SpeedText.Text = $"{Math.Round(state.CurrentSpeedKmh):0}";
        ShiftPositionText.Text = state.ShiftPositionDisplay;
        DriveModeText.Text = state.DriveModeDisplay;
        DriveModeText.Foreground = state.DriveMode switch
        {
            E13DriveMode.Sport => SportBrush,
            E13DriveMode.Eco => EcoBrush,
            _ => NormalBrush
        };
        DriveModeBadge.BorderBrush = DriveModeText.Foreground;

        PowerMeterValueText.Text = state.PowerMeterPercent.ToString();
        PowerMeterModeText.Text = state.PowerMeterPercent switch
        {
            > 0 => "POWER",
            < 0 => "CHARGE",
            _ => "NEUTRAL"
        };
        UpdatePowerMeterBar(state.PowerMeterPercent);
        UpdateFuelLevel(state.FuelLevelPercent);

        EstimatedRangeText.Text = $"RANGE {state.EstimatedRangeKm} km";
        OdometerText.Text = $"ODO {state.OdometerKm:N0} km";
        TripText.Text = $"TRIP {state.TripMeterKm:0.0} km";
        ClockText.Text = DateTime.Now.ToString("HH:mm");
        OutsideTempText.Text = $"{state.OutsideTemperatureCelsius}°C";
        SystemMessageText.Text = systemMessage;
        TurnSignalAudioStatusText.Text = turnSignalAudioStatus;
        ReverseAudioStatusText.Text = reverseAudioStatus;
        ForwardApproachAudioStatusText.Text = forwardApproachAudioStatus;
        UpdateAudioStatusBrush(TurnSignalAudioStatusText, turnSignalAudioStatus);
        UpdateAudioStatusBrush(ReverseAudioStatusText, reverseAudioStatus);
        UpdateAudioStatusBrush(ForwardApproachAudioStatusText, forwardApproachAudioStatus);

        WarningLampLayer.UpdateLamps(
            state.PowerState != E13PowerState.Off,
            isSelfCheckActive,
            state.IsReady,
            state.IsElectricParkingBrakeOn,
            state.IsAutoBrakeHoldEnabled,
            state.IsAutoBrakeHoldHolding,
            isLeftTurnVisible,
            isRightTurnVisible,
            isTailLampOn,
            isHighBeamOn,
            state.IsChargeModeOn,
            state.IsMannerModeOn);

        PreviewPanel.Visibility = isPreviewPanelVisible ? Visibility.Visible : Visibility.Collapsed;
        PreviewText.Text =
            $"Power: {state.PowerStateDisplay}  Shift: {state.ShiftPositionDisplay}\n" +
            $"Mode: {state.DriveModeDisplay}  EPB: {(state.IsElectricParkingBrakeOn ? "ON" : "OFF")}\n" +
            $"AUTO HOLD: {(state.IsAutoBrakeHoldEnabled ? (state.IsAutoBrakeHoldHolding ? "HOLD" : "ON") : "OFF")}";
    }

    private static void UpdateAudioStatusBrush(TextBlock textBlock, string status)
    {
        textBlock.Foreground = status switch
        {
            "PLAYING" => ReadyBrush,
            "MUTED" => SportBrush,
            "NO FILE" => OffBrush,
            _ => NormalBrush
        };
    }

    private void UpdatePowerMeterBar(int powerMeterPercent)
    {
        var clamped = Math.Clamp(powerMeterPercent, -100, 100);
        var angle = PowerStartAngle + ((clamped + 100.0) / 200.0) * PowerSweepAngle;
        PowerNeedleRotateTransform.Angle = angle;
        PowerNeedleLine.Stroke = clamped < 0 ? ChargeBrush : PowerBrush;
        PowerMeterValueText.Foreground = clamped < 0 ? ChargeBrush : PowerBrush;
        PowerChargeArc.Opacity = clamped < 0 ? 1.0 : 0.35;
    }

    private void UpdateFuelLevel(int fuelLevelPercent)
    {
        var clamped = Math.Clamp(fuelLevelPercent, 0, 100);
        var height = 182.0 * clamped / 100.0;
        FuelLevelBar.Height = height;
        Canvas.SetTop(FuelLevelBar, 210.0 - height);
    }

    private void BuildPowerMeterTicks()
    {
        PowerTickCanvas.Children.Clear();

        for (var value = -100; value <= 100; value += 10)
        {
            var angle = PowerStartAngle + ((value + 100.0) / 200.0) * PowerSweepAngle;
            var isMajor = value % 20 == 0;
            var outer = PointFromPowerAngle(angle, PowerTickOuterRadius);
            var inner = PointFromPowerAngle(angle, isMajor ? PowerTickInnerRadius : PowerTickInnerRadius + 7.0);
            var tick = new Line
            {
                X1 = inner.X,
                Y1 = inner.Y,
                X2 = outer.X,
                Y2 = outer.Y,
                Stroke = value < 0 ? ChargeBrush : PowerBrush,
                StrokeThickness = isMajor ? 2.4 : 1.4,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            PowerTickCanvas.Children.Add(tick);
        }

        PowerChargeArc.Data = CreatePowerArcGeometry(
            PowerStartAngle,
            PowerStartAngle + PowerSweepAngle * 0.28,
            PowerTickOuterRadius + 8.0);
    }

    private static Geometry CreatePowerArcGeometry(double startAngle, double endAngle, double radius)
    {
        var start = PointFromPowerAngle(startAngle, radius);
        var end = PointFromPowerAngle(endAngle, radius);
        return new PathGeometry
        {
            Figures =
            [
                new PathFigure
                {
                    StartPoint = start,
                    Segments =
                    [
                        new ArcSegment
                        {
                            Point = end,
                            Size = new Size(radius, radius),
                            SweepDirection = SweepDirection.Clockwise,
                            IsLargeArc = Math.Abs(endAngle - startAngle) > 180.0
                        }
                    ]
                }
            ]
        };
    }

    private static Point PointFromPowerAngle(double angle, double radius)
    {
        var radians = angle * Math.PI / 180.0;
        return new Point(
            PowerCenterX + radius * Math.Sin(radians),
            PowerCenterY - radius * Math.Cos(radians));
    }
}
