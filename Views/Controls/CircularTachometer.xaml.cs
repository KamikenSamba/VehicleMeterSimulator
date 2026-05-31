using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VehicleMeterSimulator.Views.Controls;

public partial class CircularTachometer : UserControl
{
    private const double StartAngle = -150.0;
    private const double SweepAngle = 300.0;
    private const double Center = 280.0;
    private const double TickOuterRadius = 228.0;
    private const double MajorTickInnerRadius = 200.0;
    private const double MinorTickInnerRadius = 204.0;
    private const double NumberRadius = 224.0;
    private const double RedZoneRadius = 224.0;

    private static readonly Brush NormalNeedleBrush = new SolidColorBrush(Color.FromRgb(234, 248, 255));
    private static readonly Brush WarningNeedleBrush = new SolidColorBrush(Color.FromRgb(255, 172, 66));
    private static readonly Brush RedlineNeedleBrush = new SolidColorBrush(Color.FromRgb(255, 64, 64));
    private static readonly Brush NormalTextBrush = new SolidColorBrush(Color.FromRgb(170, 184, 194));
    private static readonly Brush WarningTextBrush = new SolidColorBrush(Color.FromRgb(255, 190, 86));
    private static readonly Brush RedlineTextBrush = new SolidColorBrush(Color.FromRgb(255, 82, 82));
    private static readonly Brush TickBrush = new SolidColorBrush(Color.FromRgb(221, 232, 239));
    private static readonly Brush GenericTickBrush = new SolidColorBrush(Color.FromRgb(191, 215, 230));
    private static readonly Brush GenericTextBrush = new SolidColorBrush(Color.FromRgb(210, 232, 244));
    private static readonly Brush StandardOuterRingBrush = new SolidColorBrush(Color.FromRgb(50, 56, 61));
    private static readonly Brush StandardInnerGlowBrush = new SolidColorBrush(Color.FromRgb(102, 128, 143));
    private static readonly Brush SportOuterRingBrush = new SolidColorBrush(Color.FromRgb(155, 62, 30));
    private static readonly Brush SportInnerGlowBrush = new SolidColorBrush(Color.FromRgb(235, 120, 48));

    private int configuredMaxRpm = 0;
    private int configuredRevLimiterRpm = 0;
    private string configuredMeterStyleId = "";
    private Brush currentTickBrush = TickBrush;
    private Brush currentNumberBrush = TickBrush;

    public CircularTachometer()
    {
        InitializeComponent();
        ConfigureStyle("lfa-inspired", "", 10000, 9000);
    }

    public void ConfigureStyle(string meterStyleId, string vehicleName, int maxRpm, int revLimiterRpm)
    {
        var normalizedStyleId = string.Equals(meterStyleId, "lfa-inspired", StringComparison.OrdinalIgnoreCase)
            ? "lfa-inspired"
            : "generic-sport";

        configuredMeterStyleId = normalizedStyleId;
        configuredMaxRpm = Math.Max(maxRpm, 1000);
        configuredRevLimiterRpm = Math.Clamp(revLimiterRpm, 0, configuredMaxRpm);

        if (normalizedStyleId == "lfa-inspired")
        {
            GaugeVehicleNameText.Visibility = Visibility.Collapsed;
            GaugeStyleText.Visibility = Visibility.Collapsed;
            MeterModeText.Text = "ASG";
            MeterModeText.Foreground = new SolidColorBrush(Color.FromRgb(95, 118, 132));
            currentTickBrush = TickBrush;
            currentNumberBrush = TickBrush;
        }
        else
        {
            GaugeVehicleNameText.Text = vehicleName;
            GaugeStyleText.Text = "GENERIC SPORT DISPLAY";
            GaugeVehicleNameText.Visibility = Visibility.Visible;
            GaugeStyleText.Visibility = Visibility.Visible;
            MeterModeText.Text = "SPORT";
            MeterModeText.Foreground = new SolidColorBrush(Color.FromRgb(134, 187, 216));
            currentTickBrush = GenericTickBrush;
            currentNumberBrush = GenericTextBrush;
        }

        BuildTachometerScale(configuredMaxRpm, configuredRevLimiterRpm);
        ApplyDrivingModeAccent("standard");
    }

    public void ApplyDrivingModeAccent(string accentStyleId)
    {
        var isSportAccent = string.Equals(accentStyleId, "sport", StringComparison.OrdinalIgnoreCase);

        OuterRingEllipse.Stroke = isSportAccent ? SportOuterRingBrush : StandardOuterRingBrush;
        InnerGlowEllipse.Stroke = isSportAccent ? SportInnerGlowBrush : StandardInnerGlowBrush;
        InnerGlowEllipse.Opacity = isSportAccent ? 0.9 : 0.65;
        GaugeStyleText.Foreground = isSportAccent
            ? new SolidColorBrush(Color.FromRgb(255, 170, 82))
            : new SolidColorBrush(Color.FromRgb(134, 187, 216));
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
        if (safeMaxRpm != configuredMaxRpm || revLimiterRpm != configuredRevLimiterRpm)
        {
            ConfigureStyle(configuredMeterStyleId, GaugeVehicleNameText.Text, safeMaxRpm, revLimiterRpm);
        }

        var clampedRpm = Math.Clamp(needleDisplayRpm, 0.0, safeMaxRpm);
        var rpmRatio = clampedRpm / safeMaxRpm;
        NeedleRotateTransform.Angle = StartAngle + rpmRatio * SweepAngle;

        SpeedValueText.Text = $"{Math.Round(currentSpeedKmh):0}";
        GearValueText.Text = currentGear;
        GearValueText.FontSize = currentGear.Length >= 3 ? 48 : 68;
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

        var warningRpm = Math.Max(0, revLimiterRpm - 1000);
        if (currentRpm >= warningRpm)
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

    private void BuildTachometerScale(int maxRpm, int revLimiterRpm)
    {
        TachTickCanvas.Children.Clear();
        TachNumberCanvas.Children.Clear();

        var maxNumber = Math.Max(1, maxRpm / 1000);
        for (var number = 0; number <= maxNumber; number++)
        {
            var rpm = number * 1000.0;
            var ratio = rpm / maxRpm;
            var angle = StartAngle + ratio * SweepAngle;
            var isRedZone = rpm >= revLimiterRpm;

            AddTick(angle, number == 0 || number == maxNumber || number % 5 == 0, isRedZone);
            AddNumber(number, angle, isRedZone);
        }

        RedZoneArc.Data = CreateArcGeometry(
            StartAngle + ((double)revLimiterRpm / maxRpm) * SweepAngle,
            StartAngle + SweepAngle,
            RedZoneRadius);
    }

    private void AddTick(double angle, bool isMajor, bool isRedZone)
    {
        var innerRadius = isMajor ? MajorTickInnerRadius : MinorTickInnerRadius;
        var inner = PointFromNeedleAngle(angle, innerRadius);
        var outer = PointFromNeedleAngle(angle, TickOuterRadius);
        var tick = new Line
        {
            X1 = inner.X,
            Y1 = inner.Y,
            X2 = outer.X,
            Y2 = outer.Y,
            Stroke = isRedZone ? RedlineTextBrush : currentTickBrush,
            StrokeThickness = isMajor ? 3 : 2,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };

        TachTickCanvas.Children.Add(tick);
    }

    private void AddNumber(int number, double angle, bool isRedZone)
    {
        var position = PointFromNeedleAngle(angle, NumberRadius);
        var textBlock = new TextBlock
        {
            Text = number.ToString(),
            Width = 48,
            Height = 32,
            Foreground = isRedZone ? RedlineTextBrush : currentNumberBrush,
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center
        };

        Canvas.SetLeft(textBlock, position.X - 24);
        Canvas.SetTop(textBlock, position.Y - 16);
        TachNumberCanvas.Children.Add(textBlock);
    }

    private static Geometry CreateArcGeometry(double startAngle, double endAngle, double radius)
    {
        var startPoint = PointFromNeedleAngle(startAngle, radius);
        var endPoint = PointFromNeedleAngle(endAngle, radius);
        var arcSize = new Size(radius, radius);
        var isLargeArc = Math.Abs(endAngle - startAngle) > 180.0;

        return new PathGeometry
        {
            Figures =
            [
                new PathFigure
                {
                    StartPoint = startPoint,
                    Segments =
                    [
                        new ArcSegment
                        {
                            Point = endPoint,
                            Size = arcSize,
                            SweepDirection = SweepDirection.Clockwise,
                            IsLargeArc = isLargeArc
                        }
                    ]
                }
            ]
        };
    }

    private static Point PointFromNeedleAngle(double angle, double radius)
    {
        var radians = angle * Math.PI / 180.0;
        return new Point(
            Center + radius * Math.Sin(radians),
            Center - radius * Math.Cos(radians));
    }
}
