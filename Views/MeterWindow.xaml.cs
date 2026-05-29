using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VehicleMeterSimulator.Models;

namespace VehicleMeterSimulator.Views;

public partial class MeterWindow : Window
{
    private enum StartupSweepPhase
    {
        None,
        Rising,
        Falling
    }

    private const double StartupSweepPhaseMilliseconds = 650.0;

    private static readonly Brush InactiveTextBrush = new SolidColorBrush(Color.FromRgb(143, 143, 138));
    private static readonly Brush InactiveMessageBrush = new SolidColorBrush(Color.FromRgb(168, 168, 163));
    private static readonly Brush InactiveBorderBrush = new SolidColorBrush(Color.FromRgb(48, 48, 52));
    private static readonly Brush ActiveTextBrush = new SolidColorBrush(Color.FromRgb(220, 54, 54));
    private static readonly Brush ActiveMessageBrush = new SolidColorBrush(Color.FromRgb(220, 160, 160));
    private static readonly Brush ActiveBorderBrush = new SolidColorBrush(Color.FromRgb(160, 35, 35));
    private static readonly Brush ThrottleInactiveBrush = new SolidColorBrush(Color.FromRgb(189, 189, 184));

    private readonly VehicleProfile vehicle;
    private readonly VehicleRuntimeState runtimeState;
    private readonly DispatcherTimer updateTimer;
    private StartupSweepPhase startupSweepPhase = StartupSweepPhase.None;
    private double startupSweepElapsedMilliseconds = 0.0;
    private double displayedNeedleRpm = 0.0;
    private string? sweepStatusMessage;

    public MeterWindow(VehicleProfile vehicle)
    {
        InitializeComponent();

        this.vehicle = vehicle;
        runtimeState = new VehicleRuntimeState();
        updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        updateTimer.Tick += UpdateTimer_Tick;

        Title = $"{this.vehicle.Name} - Meter Display";
        VehicleNameTextBlock.Text = this.vehicle.Name;
        UpdateRuntimeDisplay();
        updateTimer.Start();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Focus();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        runtimeState.SetAcceleratorPressed(false);
        runtimeState.SetBrakePressed(false);
        StopUpdateTimer();
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        runtimeState.SetAcceleratorPressed(false);
        runtimeState.SetBrakePressed(false);
        UpdateRuntimeDisplay();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.I)
        {
            if (IsStartupSweepActive)
            {
                CancelStartupSweep();
                runtimeState.ToggleIgnition();
                UpdateRuntimeDisplay();
                e.Handled = true;
                return;
            }

            var wasIgnitionOn = runtimeState.IsIgnitionOn;
            runtimeState.ToggleIgnition();
            if (!wasIgnitionOn && runtimeState.IsIgnitionOn)
            {
                StartStartupSweep();
            }

            UpdateRuntimeDisplay();
            e.Handled = true;
            return;
        }

        if (IsStartupSweepActive)
        {
            sweepStatusMessage = "Instrument Cluster Self Check In Progress";
            UpdateRuntimeDisplay();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.S)
        {
            runtimeState.ToggleEngine();
            UpdateRuntimeDisplay();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.A)
        {
            if (!e.IsRepeat)
            {
                runtimeState.SetAcceleratorPressed(true);
                UpdateRuntimeDisplay();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Z)
        {
            if (!e.IsRepeat)
            {
                runtimeState.SetBrakePressed(true);
                UpdateRuntimeDisplay();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up)
        {
            if (!e.IsRepeat)
            {
                runtimeState.TryShiftUp(vehicle);
                UpdateRuntimeDisplay();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down)
        {
            if (!e.IsRepeat)
            {
                runtimeState.TryShiftDown(vehicle);
                UpdateRuntimeDisplay();
            }

            e.Handled = true;
        }
    }

    private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (IsStartupSweepActive && (e.Key == Key.A || e.Key == Key.Z))
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Key.A)
        {
            runtimeState.SetAcceleratorPressed(false);
            UpdateRuntimeDisplay();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Z)
        {
            runtimeState.SetBrakePressed(false);
            UpdateRuntimeDisplay();
            e.Handled = true;
        }
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        if (IsStartupSweepActive)
        {
            UpdateStartupSweep(updateTimer.Interval.TotalMilliseconds);
            UpdateRuntimeDisplay();
            return;
        }

        runtimeState.UpdateSimulation(vehicle, updateTimer.Interval.TotalSeconds);
        UpdateRuntimeDisplay();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        runtimeState.SetAcceleratorPressed(false);
        runtimeState.SetBrakePressed(false);
        StopUpdateTimer();

        var mainWindow = new MainWindow();
        Application.Current.MainWindow = mainWindow;
        mainWindow.Show();
        Close();
    }

    private void StopUpdateTimer()
    {
        if (updateTimer.IsEnabled)
        {
            updateTimer.Stop();
        }
    }

    private bool IsStartupSweepActive => startupSweepPhase != StartupSweepPhase.None;

    private void StartStartupSweep()
    {
        startupSweepPhase = StartupSweepPhase.Rising;
        startupSweepElapsedMilliseconds = 0.0;
        displayedNeedleRpm = 0.0;
        sweepStatusMessage = "Instrument Cluster Self Check";
    }

    private void CancelStartupSweep()
    {
        startupSweepPhase = StartupSweepPhase.None;
        startupSweepElapsedMilliseconds = 0.0;
        displayedNeedleRpm = 0.0;
        sweepStatusMessage = null;
    }

    private void UpdateStartupSweep(double elapsedMilliseconds)
    {
        startupSweepElapsedMilliseconds += elapsedMilliseconds;
        var progress = Math.Clamp(startupSweepElapsedMilliseconds / StartupSweepPhaseMilliseconds, 0.0, 1.0);

        if (startupSweepPhase == StartupSweepPhase.Rising)
        {
            displayedNeedleRpm = vehicle.MaxRpm * EaseInOut(progress);

            if (progress >= 1.0)
            {
                startupSweepPhase = StartupSweepPhase.Falling;
                startupSweepElapsedMilliseconds = 0.0;
                displayedNeedleRpm = vehicle.MaxRpm;
            }

            return;
        }

        displayedNeedleRpm = vehicle.MaxRpm * (1.0 - EaseInOut(progress));

        if (progress >= 1.0)
        {
            CancelStartupSweep();
        }
    }

    private static double EaseInOut(double progress)
    {
        return progress * progress * (3.0 - 2.0 * progress);
    }

    private void UpdateRuntimeDisplay()
    {
        MainTachometer.UpdateGauge(
            runtimeState.CurrentRpm,
            IsStartupSweepActive ? displayedNeedleRpm : runtimeState.CurrentRpm,
            runtimeState.CurrentSpeedKmh,
            runtimeState.CurrentGear,
            vehicle.MaxRpm,
            vehicle.RevLimiterRpm,
            IsStartupSweepActive);

        MainIndicatorPanel.UpdateIndicators(
            runtimeState.IsIgnitionOn,
            runtimeState.IsEngineRunning,
            IsStartupSweepActive,
            ShouldShowShiftUpIndicator());

        ThrottleText.Text = $"Throttle: {runtimeState.ThrottlePercent}%";
        BrakeText.Text = $"Brake: {runtimeState.BrakePercent}%";
        SystemMessageText.Text = sweepStatusMessage ?? runtimeState.SystemMessage;

        if (runtimeState.IsIgnitionOn)
        {
            IgnitionStatusText.Text = "Ignition: ON";
            IgnitionStatusText.Foreground = ActiveTextBrush;
            IgnitionStatusBorder.BorderBrush = ActiveBorderBrush;
        }
        else
        {
            IgnitionStatusText.Text = "Ignition: OFF";
            IgnitionStatusText.Foreground = InactiveTextBrush;
            IgnitionStatusBorder.BorderBrush = InactiveBorderBrush;
        }

        if (runtimeState.IsEngineRunning)
        {
            EngineStatusText.Text = "Engine: RUNNING";
            EngineStatusText.Foreground = ActiveTextBrush;
            EngineStatusBorder.BorderBrush = ActiveBorderBrush;
            SystemMessageText.Foreground = ActiveMessageBrush;
        }
        else
        {
            EngineStatusText.Text = "Engine: OFF";
            EngineStatusText.Foreground = InactiveTextBrush;
            EngineStatusBorder.BorderBrush = InactiveBorderBrush;
            SystemMessageText.Foreground = runtimeState.IsIgnitionOn ? ActiveMessageBrush : InactiveMessageBrush;
        }

        if (runtimeState.IsAcceleratorPressed)
        {
            ThrottleText.Foreground = ActiveTextBrush;
            ThrottleStatusBorder.BorderBrush = ActiveBorderBrush;
        }
        else
        {
            ThrottleText.Foreground = ThrottleInactiveBrush;
            ThrottleStatusBorder.BorderBrush = InactiveBorderBrush;
        }

        if (runtimeState.IsBrakePressed)
        {
            BrakeText.Foreground = ActiveTextBrush;
            BrakeStatusBorder.BorderBrush = ActiveBorderBrush;
        }
        else
        {
            BrakeText.Foreground = ThrottleInactiveBrush;
            BrakeStatusBorder.BorderBrush = InactiveBorderBrush;
        }
    }

    private bool ShouldShowShiftUpIndicator()
    {
        return runtimeState.IsIgnitionOn
            && runtimeState.IsEngineRunning
            && !IsStartupSweepActive
            && runtimeState.CurrentGearNumber >= 1
            && runtimeState.CurrentGearNumber < vehicle.ForwardGearCount
            && runtimeState.CurrentRpm >= 8000;
    }
}
