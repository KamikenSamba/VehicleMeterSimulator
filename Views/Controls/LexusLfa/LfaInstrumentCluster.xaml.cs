using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VehicleMeterSimulator.Views.Controls.LexusLfa;

public partial class LfaInstrumentCluster : UserControl
{
    private const double StartAngle = -150.0;
    private const double SweepAngle = 300.0;
    private const double CenterX = 310.0;
    private const double CenterY = 260.0;
    private const double MajorTickOuterRadius = 246.0;
    private const double MajorTickInnerRadius = 214.0;
    private const double MinorTickInnerRadius = 226.0;
    private const double NumberRadius = 194.0;
    private const double RedZoneRadius = 236.0;
    private const double RevIndicatorRadius = 255.0;

    private static readonly Brush StandardAccentBrush = new SolidColorBrush(Color.FromRgb(38, 48, 58));
    private static readonly Brush SportAccentBrush = new SolidColorBrush(Color.FromRgb(210, 64, 42));
    private static readonly Brush NumberBrush = new SolidColorBrush(Color.FromRgb(230, 235, 236));
    private static readonly Brush RedZoneBrush = new SolidColorBrush(Color.FromRgb(228, 38, 38));
    private static readonly Brush NeedleNormalBrush = new SolidColorBrush(Color.FromRgb(247, 253, 255));
    private static readonly Brush NeedleWarningBrush = new SolidColorBrush(Color.FromRgb(255, 188, 72));
    private static readonly Brush NeedleRedBrush = new SolidColorBrush(Color.FromRgb(255, 66, 58));
    private static readonly Brush MessageBrush = new SolidColorBrush(Color.FromRgb(182, 184, 180));
    private static readonly Brush SportModeBrush = new SolidColorBrush(Color.FromRgb(255, 176, 76));

    private int configuredMaxRpm;
    private int configuredRevLimiterRpm;

    public LfaInstrumentCluster()
    {
        InitializeComponent();
        ConfigureScale(10000, 9000);
    }

    public void ConfigureScale(int maxRpm, int revLimiterRpm)
    {
        configuredMaxRpm = Math.Max(1000, maxRpm);
        configuredRevLimiterRpm = Math.Clamp(revLimiterRpm, 0, configuredMaxRpm);

        BuildScale();
        RedZoneArc.Data = CreateArcGeometry(
            RpmToAngle(configuredRevLimiterRpm),
            RpmToAngle(configuredMaxRpm),
            RedZoneRadius);
        RevIndicatorArc.Data = CreateArcGeometry(
            RpmToAngle(Math.Max(configuredRevLimiterRpm - 1000, 0)),
            RpmToAngle(configuredMaxRpm),
            RevIndicatorRadius);
    }

    public void UpdateCluster(
        double actualRpm,
        double needleRpm,
        double speedKmh,
        string gearText,
        string driveModeText,
        string transmissionModeText,
        string systemMessage,
        bool isIgnitionOn,
        bool isEngineRunning,
        bool isSelfCheckActive,
        bool isParkingBrakeApplied,
        bool shouldShowShiftUp,
        bool isLeftTurnVisible,
        bool isRightTurnVisible,
        bool isTailLampOn,
        bool isHighBeamOn,
        bool isLampTestActive,
        bool isMenuPreviewActive,
        string accentStyleId,
        int maxRpm,
        int revLimiterRpm)
    {
        if (maxRpm != configuredMaxRpm || revLimiterRpm != configuredRevLimiterRpm)
        {
            ConfigureScale(maxRpm, revLimiterRpm);
        }

        var safeMaxRpm = Math.Max(1, configuredMaxRpm);
        var clampedNeedleRpm = Math.Clamp(needleRpm, 0.0, safeMaxRpm);
        NeedleRotateTransform.Angle = RpmToAngle(clampedNeedleRpm);

        SpeedText.Text = $"{Math.Round(speedKmh):0}";
        GearText.Text = gearText;
        GearText.FontSize = gearText.Length >= 3 ? 48 : 66;
        DriveModeText.Text = $"{driveModeText} / {transmissionModeText}";
        RpmText.Text = $"RPM: {Math.Round(actualRpm):0}";
        SelfCheckText.Visibility = isSelfCheckActive ? Visibility.Visible : Visibility.Collapsed;
        SystemMessageText.Text = systemMessage;

        ApplyRpmColors(clampedNeedleRpm, revLimiterRpm);
        ApplyMenuPreview(isMenuPreviewActive);
        ApplyAccent(accentStyleId);

        WarningLampLayer.UpdateLamps(
            isIgnitionOn,
            isEngineRunning,
            isSelfCheckActive,
            isParkingBrakeApplied,
            shouldShowShiftUp,
            isLeftTurnVisible,
            isRightTurnVisible,
            isTailLampOn,
            isHighBeamOn,
            isLampTestActive);
    }

    private void BuildScale()
    {
        TickCanvas.Children.Clear();
        NumberCanvas.Children.Clear();

        var maxNumber = Math.Max(1, configuredMaxRpm / 1000);
        for (var number = 0; number <= maxNumber; number++)
        {
            var angle = RpmToAngle(number * 1000.0);
            var isRedZone = number * 1000 >= configuredRevLimiterRpm;
            AddTick(angle, true, isRedZone);
            AddNumber(number, angle, isRedZone);

            if (number == maxNumber)
            {
                continue;
            }

            for (var minor = 1; minor < 5; minor++)
            {
                var minorRpm = number * 1000.0 + minor * 200.0;
                AddTick(RpmToAngle(minorRpm), false, minorRpm >= configuredRevLimiterRpm);
            }
        }
    }

    private void AddTick(double angle, bool isMajor, bool isRedZone)
    {
        var outer = PointFromAngle(angle, MajorTickOuterRadius);
        var inner = PointFromAngle(angle, isMajor ? MajorTickInnerRadius : MinorTickInnerRadius);
        var tick = new Line
        {
            X1 = inner.X,
            Y1 = inner.Y,
            X2 = outer.X,
            Y2 = outer.Y,
            Stroke = isRedZone ? RedZoneBrush : NumberBrush,
            StrokeThickness = isMajor ? 3.5 : 1.6,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };

        TickCanvas.Children.Add(tick);
    }

    private void AddNumber(int number, double angle, bool isRedZone)
    {
        var position = PointFromAngle(angle, NumberRadius);
        var textBlock = new TextBlock
        {
            Text = number.ToString(),
            Width = 46,
            Height = 30,
            FontSize = 23,
            FontWeight = FontWeights.SemiBold,
            Foreground = isRedZone ? RedZoneBrush : NumberBrush,
            TextAlignment = TextAlignment.Center
        };

        Canvas.SetLeft(textBlock, position.X - 23);
        Canvas.SetTop(textBlock, position.Y - 15);
        NumberCanvas.Children.Add(textBlock);
    }

    private void ApplyRpmColors(double needleRpm, int revLimiterRpm)
    {
        if (needleRpm >= revLimiterRpm)
        {
            NeedleLine.Stroke = NeedleRedBrush;
            RpmText.Foreground = NeedleRedBrush;
            RevIndicatorArc.Stroke = NeedleWarningBrush;
            RevIndicatorArc.Opacity = 1.0;
            return;
        }

        if (needleRpm >= Math.Max(0, revLimiterRpm - 1000))
        {
            NeedleLine.Stroke = NeedleWarningBrush;
            RpmText.Foreground = NeedleWarningBrush;
            RevIndicatorArc.Stroke = new SolidColorBrush(Color.FromRgb(47, 235, 109));
            RevIndicatorArc.Opacity = 0.88;
            return;
        }

        NeedleLine.Stroke = NeedleNormalBrush;
        RpmText.Foreground = MessageBrush;
        RevIndicatorArc.Opacity = 0.0;
    }

    private void ApplyMenuPreview(bool isMenuPreviewActive)
    {
        MenuPanel.Visibility = isMenuPreviewActive ? Visibility.Visible : Visibility.Collapsed;
        TachShiftTransform.X = isMenuPreviewActive ? 120 : 0;
    }

    private void ApplyAccent(string accentStyleId)
    {
        var isSport = string.Equals(accentStyleId, "sport", StringComparison.OrdinalIgnoreCase);
        ModeAccentRing.Stroke = isSport ? SportAccentBrush : StandardAccentBrush;
        ModeAccentRing.Opacity = isSport ? 1.0 : 0.85;
        DriveModeText.Foreground = isSport ? SportModeBrush : new SolidColorBrush(Color.FromRgb(137, 145, 150));
    }

    private double RpmToAngle(double rpm)
    {
        var safeMax = Math.Max(1.0, configuredMaxRpm);
        var ratio = Math.Clamp(rpm / safeMax, 0.0, 1.0);
        return StartAngle + ratio * SweepAngle;
    }

    private static Geometry CreateArcGeometry(double startAngle, double endAngle, double radius)
    {
        var startPoint = PointFromAngle(startAngle, radius);
        var endPoint = PointFromAngle(endAngle, radius);
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
                            Size = new Size(radius, radius),
                            SweepDirection = SweepDirection.Clockwise,
                            IsLargeArc = Math.Abs(endAngle - startAngle) > 180.0
                        }
                    ]
                }
            ]
        };
    }

    private static Point PointFromAngle(double angle, double radius)
    {
        var radians = angle * Math.PI / 180.0;
        return new Point(
            CenterX + radius * Math.Sin(radians),
            CenterY - radius * Math.Cos(radians));
    }
}
