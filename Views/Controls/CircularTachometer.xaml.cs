using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VehicleMeterSimulator.Views.Controls;

public partial class CircularTachometer : UserControl
{
    private const double StartAngle = -150.0;
    private const double SweepAngle = 300.0;

    private static readonly Brush NormalNeedleBrush = new SolidColorBrush(Color.FromRgb(234, 248, 255));
    private static readonly Brush WarningNeedleBrush = new SolidColorBrush(Color.FromRgb(255, 172, 66));
    private static readonly Brush RedlineNeedleBrush = new SolidColorBrush(Color.FromRgb(255, 64, 64));
    private static readonly Brush NormalTextBrush = new SolidColorBrush(Color.FromRgb(170, 184, 194));
    private static readonly Brush WarningTextBrush = new SolidColorBrush(Color.FromRgb(255, 190, 86));
    private static readonly Brush RedlineTextBrush = new SolidColorBrush(Color.FromRgb(255, 82, 82));

    public CircularTachometer()
    {
        InitializeComponent();
    }

    public void UpdateGauge(
        double currentRpm,
        double currentSpeedKmh,
        string currentGear,
        int maxRpm,
        int revLimiterRpm)
    {
        UpdateGauge(
            currentRpm,
            currentRpm,
            currentSpeedKmh,
            currentGear,
            maxRpm,
            revLimiterRpm,
            false);
    }

    public void UpdateGauge(
        double actualRpm,
        double needleDisplayRpm,
        double currentSpeedKmh,
        string currentGear,
        int maxRpm,
        int revLimiterRpm,
        bool isSelfCheckActive)
    {
        var safeMaxRpm = Math.Max(maxRpm, 1);
        var clampedRpm = Math.Clamp(needleDisplayRpm, 0.0, safeMaxRpm);
        var rpmRatio = clampedRpm / safeMaxRpm;
        NeedleRotateTransform.Angle = StartAngle + rpmRatio * SweepAngle;

        SpeedValueText.Text = $"{Math.Round(currentSpeedKmh):0}";
        GearValueText.Text = currentGear;
        RpmDigitalText.Text = $"RPM: {Math.Round(actualRpm):0}";
        SelfCheckText.Visibility = isSelfCheckActive ? Visibility.Visible : Visibility.Collapsed;

        UpdateHighRpmColors(needleDisplayRpm, revLimiterRpm);
    }

    private void UpdateHighRpmColors(double currentRpm, int revLimiterRpm)
    {
        if (currentRpm >= revLimiterRpm)
        {
            NeedleLine.Stroke = RedlineNeedleBrush;
            RpmDigitalText.Foreground = RedlineTextBrush;
            RedZoneArc.Stroke = RedlineTextBrush;
            RedZoneArc.Opacity = 1.0;
            return;
        }

        if (currentRpm >= 8000)
        {
            NeedleLine.Stroke = WarningNeedleBrush;
            RpmDigitalText.Foreground = WarningTextBrush;
            RedZoneArc.Stroke = RedlineTextBrush;
            RedZoneArc.Opacity = 0.85;
            return;
        }

        NeedleLine.Stroke = NormalNeedleBrush;
        RpmDigitalText.Foreground = NormalTextBrush;
        RedZoneArc.Stroke = RedlineTextBrush;
        RedZoneArc.Opacity = 0.65;
    }
}
