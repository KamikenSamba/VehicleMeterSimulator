using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VehicleMeterSimulator.Models;
using VehicleMeterSimulator.Services;

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
    private static readonly Brush SportModeTextBrush = new SolidColorBrush(Color.FromRgb(255, 166, 76));
    private static readonly Brush SportModeBorderBrush = new SolidColorBrush(Color.FromRgb(202, 92, 35));
    private static readonly Brush AutoModeTextBrush = new SolidColorBrush(Color.FromRgb(138, 210, 240));
    private static readonly Brush AutoModeBorderBrush = new SolidColorBrush(Color.FromRgb(62, 139, 174));
    private static readonly Brush ThrottleInactiveBrush = new SolidColorBrush(Color.FromRgb(189, 189, 184));

    private readonly VehicleProfile vehicle;
    private readonly VehicleRuntimeState runtimeState;
    private readonly AudioService audioService;
    private readonly EngineAudioService engineAudioService;
    private readonly bool hasAvailableAudio;
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
        runtimeState.InitializeDrivingMode(this.vehicle);
        runtimeState.InitializeTransmissionMode(this.vehicle);
        audioService = new AudioService();
        engineAudioService = new EngineAudioService();
        engineAudioService.Initialize(this.vehicle.AudioProfile);
        hasAvailableAudio = audioService.HasAvailableSound(this.vehicle.AudioProfile);
        updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        updateTimer.Tick += UpdateTimer_Tick;

        Title = $"{this.vehicle.Name} - Meter Display";
        VehicleNameTextBlock.Text = this.vehicle.Name;
        TransmissionTextBlock.Text = this.vehicle.Transmission;
        MainTachometer.ConfigureStyle(
            this.vehicle.MeterStyleId,
            this.vehicle.Name,
            this.vehicle.MaxRpm,
            this.vehicle.RevLimiterRpm);
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
        engineAudioService.Dispose();
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        runtimeState.SetAcceleratorPressed(false);
        runtimeState.SetBrakePressed(false);
        UpdateRuntimeDisplay();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.M)
        {
            if (!e.IsRepeat)
            {
                audioService.ToggleMute();
                engineAudioService.UpdateEngineSound(
                    runtimeState.IsEngineRunning,
                    runtimeState.CurrentRpm,
                    runtimeState.ThrottlePercent,
                    audioService.IsMuted);
                UpdateRuntimeDisplay();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.I)
        {
            if (IsStartupSweepActive)
            {
                CancelStartupSweep();
                runtimeState.ToggleIgnition();
                PlayVehicleSound(vehicle.AudioProfile?.IgnitionOffSound);
                UpdateRuntimeDisplay();
                e.Handled = true;
                return;
            }

            var wasIgnitionOn = runtimeState.IsIgnitionOn;
            runtimeState.ToggleIgnition();
            if (!wasIgnitionOn && runtimeState.IsIgnitionOn)
            {
                StartStartupSweep();
                PlayVehicleSound(vehicle.AudioProfile?.IgnitionOnSound);
            }
            else if (wasIgnitionOn && !runtimeState.IsIgnitionOn)
            {
                PlayVehicleSound(vehicle.AudioProfile?.IgnitionOffSound);
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

        if (e.Key == Key.D)
        {
            if (!e.IsRepeat)
            {
                runtimeState.TryCycleDrivingMode(vehicle);
                UpdateRuntimeDisplay();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.T)
        {
            if (!e.IsRepeat)
            {
                runtimeState.TryCycleTransmissionMode(vehicle);
                UpdateRuntimeDisplay();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.S)
        {
            var wasEngineRunning = runtimeState.IsEngineRunning;
            runtimeState.ToggleEngine();
            if (!wasEngineRunning && runtimeState.IsEngineRunning)
            {
                PlayVehicleSound(vehicle.AudioProfile?.EngineStartSound);
            }
            else if (wasEngineRunning && !runtimeState.IsEngineRunning && runtimeState.IsIgnitionOn)
            {
                PlayVehicleSound(vehicle.AudioProfile?.EngineStopSound);
            }

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
                var previousGearNumber = runtimeState.CurrentGearNumber;
                runtimeState.TryShiftUp(vehicle);
                if (runtimeState.CurrentGearNumber > previousGearNumber
                    && !(runtimeState.IsAutomaticTransmission && previousGearNumber == 0))
                {
                    PlayVehicleSound(vehicle.AudioProfile?.ShiftUpSound);
                }

                UpdateRuntimeDisplay();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down)
        {
            if (!e.IsRepeat)
            {
                var previousGearNumber = runtimeState.CurrentGearNumber;
                runtimeState.TryShiftDown(vehicle);
                if (runtimeState.CurrentGearNumber < previousGearNumber
                    && !(runtimeState.IsAutomaticTransmission && runtimeState.CurrentGearNumber == 0))
                {
                    PlayVehicleSound(vehicle.AudioProfile?.ShiftDownSound);
                }

                UpdateRuntimeDisplay();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.R)
        {
            if (!e.IsRepeat)
            {
                var previousGearNumber = runtimeState.CurrentGearNumber;
                runtimeState.TryToggleReverse();
                if (previousGearNumber == 0 && runtimeState.CurrentGearNumber == -1)
                {
                    PlayVehicleSound(vehicle.AudioProfile?.ReverseEngageSound);
                }
                else if (previousGearNumber == -1 && runtimeState.CurrentGearNumber == 0)
                {
                    PlayVehicleSound(vehicle.AudioProfile?.ReverseDisengageSound);
                }

                UpdateRuntimeDisplay();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.P)
        {
            if (!e.IsRepeat)
            {
                var wasParkingBrakeApplied = runtimeState.IsParkingBrakeApplied;
                runtimeState.TryToggleParkingBrake();
                if (!wasParkingBrakeApplied && runtimeState.IsParkingBrakeApplied)
                {
                    PlayVehicleSound(vehicle.AudioProfile?.ParkingBrakeAppliedSound);
                }
                else if (wasParkingBrakeApplied && !runtimeState.IsParkingBrakeApplied)
                {
                    PlayVehicleSound(vehicle.AudioProfile?.ParkingBrakeReleasedSound);
                }

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
            engineAudioService.UpdateEngineSound(
                false,
                runtimeState.CurrentRpm,
                runtimeState.ThrottlePercent,
                audioService.IsMuted);
            UpdateRuntimeDisplay();
            return;
        }

        runtimeState.UpdateSimulation(vehicle, updateTimer.Interval.TotalSeconds);
        var automaticShiftResult = runtimeState.UpdateAutomaticTransmission(
            vehicle,
            updateTimer.Interval.TotalMilliseconds);
        if (automaticShiftResult == AutomaticShiftResult.ShiftUp)
        {
            PlayVehicleSound(vehicle.AudioProfile?.ShiftUpSound);
        }
        else if (automaticShiftResult == AutomaticShiftResult.ShiftDown)
        {
            PlayVehicleSound(vehicle.AudioProfile?.ShiftDownSound);
        }

        engineAudioService.UpdateEngineSound(
            runtimeState.IsEngineRunning,
            runtimeState.CurrentRpm,
            runtimeState.ThrottlePercent,
            audioService.IsMuted);
        UpdateRuntimeDisplay();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        runtimeState.SetAcceleratorPressed(false);
        runtimeState.SetBrakePressed(false);
        StopUpdateTimer();
        engineAudioService.Dispose();

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
        var currentDrivingMode = runtimeState.GetCurrentDrivingMode(vehicle);

        MainTachometer.UpdateGauge(
            runtimeState.CurrentRpm,
            IsStartupSweepActive ? displayedNeedleRpm : runtimeState.CurrentRpm,
            runtimeState.CurrentSpeedKmh,
            runtimeState.CurrentGear,
            vehicle.MaxRpm,
            vehicle.RevLimiterRpm,
            IsStartupSweepActive);
        MainTachometer.ApplyDrivingModeAccent(currentDrivingMode.AccentStyleId);

        MainIndicatorPanel.UpdateIndicators(
            runtimeState.IsIgnitionOn,
            runtimeState.IsEngineRunning,
            IsStartupSweepActive,
            runtimeState.IsParkingBrakeApplied,
            ShouldShowShiftUpIndicator());

        ThrottleText.Text = $"Throttle: {runtimeState.ThrottlePercent}%";
        BrakeText.Text = $"Brake: {runtimeState.BrakePercent}%";
        ParkingBrakeStatusText.Text = $"Parking Brake: {(runtimeState.IsParkingBrakeApplied ? "ON" : "OFF")}";
        DriveModeText.Text = currentDrivingMode.DisplayName;
        TransmissionModeText.Text = runtimeState.GetTransmissionModeDisplayName();
        UpdateAudioStatusDisplay();
        UpdateEngineAudioStatusDisplay();
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

        if (runtimeState.IsParkingBrakeApplied)
        {
            ParkingBrakeStatusText.Foreground = ActiveTextBrush;
            ParkingBrakeStatusBorder.BorderBrush = ActiveBorderBrush;
        }
        else
        {
            ParkingBrakeStatusText.Foreground = ThrottleInactiveBrush;
            ParkingBrakeStatusBorder.BorderBrush = InactiveBorderBrush;
        }

        if (string.Equals(currentDrivingMode.AccentStyleId, "sport", StringComparison.OrdinalIgnoreCase))
        {
            DriveModeText.Foreground = SportModeTextBrush;
            DriveModeStatusBorder.BorderBrush = SportModeBorderBrush;
        }
        else
        {
            DriveModeText.Foreground = ThrottleInactiveBrush;
            DriveModeStatusBorder.BorderBrush = InactiveBorderBrush;
        }

        if (runtimeState.IsAutomaticTransmission)
        {
            TransmissionModeText.Foreground = AutoModeTextBrush;
            TransmissionModeStatusBorder.BorderBrush = AutoModeBorderBrush;
        }
        else
        {
            TransmissionModeText.Foreground = ThrottleInactiveBrush;
            TransmissionModeStatusBorder.BorderBrush = InactiveBorderBrush;
        }
    }

    private void UpdateAudioStatusDisplay()
    {
        if (!hasAvailableAudio)
        {
            AudioStatusText.Text = "Audio: No Files";
            AudioStatusText.Foreground = InactiveTextBrush;
            AudioStatusBorder.BorderBrush = InactiveBorderBrush;
            return;
        }

        if (audioService.IsMuted)
        {
            AudioStatusText.Text = "Audio: MUTED";
            AudioStatusText.Foreground = ActiveMessageBrush;
            AudioStatusBorder.BorderBrush = ActiveBorderBrush;
            return;
        }

        AudioStatusText.Text = "Audio: ON";
        AudioStatusText.Foreground = ThrottleInactiveBrush;
        AudioStatusBorder.BorderBrush = InactiveBorderBrush;
    }

    private void UpdateEngineAudioStatusDisplay()
    {
        if (!engineAudioService.IsAvailable)
        {
            EngineAudioStatusText.Text = "NO LOOP FILE";
            EngineAudioStatusText.Foreground = InactiveTextBrush;
            EngineAudioStatusBorder.BorderBrush = InactiveBorderBrush;
            return;
        }

        if (audioService.IsMuted)
        {
            EngineAudioStatusText.Text = "MUTED";
            EngineAudioStatusText.Foreground = ActiveMessageBrush;
            EngineAudioStatusBorder.BorderBrush = ActiveBorderBrush;
            return;
        }

        if (runtimeState.IsEngineRunning && engineAudioService.IsPlaying)
        {
            EngineAudioStatusText.Text = "PLAYING";
            EngineAudioStatusText.Foreground = AutoModeTextBrush;
            EngineAudioStatusBorder.BorderBrush = AutoModeBorderBrush;
            return;
        }

        EngineAudioStatusText.Text = "READY";
        EngineAudioStatusText.Foreground = ThrottleInactiveBrush;
        EngineAudioStatusBorder.BorderBrush = InactiveBorderBrush;
    }

    private void PlayVehicleSound(string? relativePath)
    {
        audioService.PlaySound(relativePath);
    }

    private bool ShouldShowShiftUpIndicator()
    {
        return runtimeState.IsIgnitionOn
            && runtimeState.IsEngineRunning
            && !IsStartupSweepActive
            && !runtimeState.IsAutomaticTransmission
            && runtimeState.CurrentGearNumber >= 1
            && runtimeState.CurrentGearNumber < vehicle.ForwardGearCount
            && runtimeState.CurrentRpm >= runtimeState.GetCurrentDrivingMode(vehicle).ShiftUpIndicatorRpm;
    }
}
