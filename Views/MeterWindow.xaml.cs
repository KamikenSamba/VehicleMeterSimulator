using System;
using System.Text;
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
    private readonly NissanNoteE13RuntimeState? noteE13RuntimeState;
    private readonly AudioService audioService;
    private readonly EngineAudioService engineAudioService;
    private readonly NoteE13AudioService noteE13AudioService;
    private readonly AudioTuningExportService audioTuningExportService;
    private readonly AudioTuningSession audioTuningSession;
    private readonly bool hasAvailableAudio;
    private readonly bool usesLfaInstrumentCluster;
    private readonly bool usesNoteE13InstrumentCluster;
    private readonly DispatcherTimer updateTimer;
    private DispatcherTimer? noteSeatbeltWarningTimer;
    private StartupSweepPhase startupSweepPhase = StartupSweepPhase.None;
    private double startupSweepElapsedMilliseconds = 0.0;
    private double displayedNeedleRpm = 0.0;
    private string? sweepStatusMessage;
    private string? transientStatusMessage;
    private int transientStatusMessageTicks;
    private bool isAudioDebugPanelVisible;
    private bool isLfaLeftTurnEnabled;
    private bool isLfaRightTurnEnabled;
    private bool isLfaHazardEnabled;
    private bool isLfaTailLampEnabled;
    private bool isLfaHighBeamEnabled;
    private bool isLfaLampTestActive;
    private bool isLfaMenuPreviewActive;
    private bool lfaBlinkVisible = true;
    private double lfaBlinkElapsedMilliseconds;
    private bool isNoteLeftTurnEnabled;
    private bool isNoteRightTurnEnabled;
    private bool isNoteHazardEnabled;
    private bool isNoteTailLampEnabled;
    private bool isNoteHighBeamEnabled;
    private bool isNotePreviewPanelVisible;
    private int noteSelfCheckTicks;

    public MeterWindow(VehicleProfile vehicle)
    {
        InitializeComponent();

        this.vehicle = vehicle;
        runtimeState = new VehicleRuntimeState();
        runtimeState.InitializeDrivingMode(this.vehicle);
        runtimeState.InitializeTransmissionMode(this.vehicle);
        usesNoteE13InstrumentCluster = this.vehicle.UsesElectricPowerMeter;
        noteE13RuntimeState = usesNoteE13InstrumentCluster ? new NissanNoteE13RuntimeState() : null;
        audioService = new AudioService();
        engineAudioService = new EngineAudioService();
        engineAudioService.Initialize(this.vehicle.AudioProfile);
        noteE13AudioService = new NoteE13AudioService();
        noteE13AudioService.Initialize(this.vehicle.NoteE13AudioProfile);
        audioTuningExportService = new AudioTuningExportService();
        audioTuningSession = AudioTuningSession.FromVehicle(this.vehicle);
        AudioTuningPanel.Initialize(audioTuningSession, this.vehicle);
        AudioTuningPanel.TuningChanged += AudioTuningPanel_TuningChanged;
        AudioTuningPanel.ResetRequested += AudioTuningPanel_ResetRequested;
        AudioTuningPanel.ExportRequested += AudioTuningPanel_ExportRequested;
        hasAvailableAudio = audioService.HasAvailableSound(this.vehicle.AudioProfile);
        usesLfaInstrumentCluster = IsLfaInstrumentClusterVehicle(this.vehicle);
        updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        updateTimer.Tick += UpdateTimer_Tick;

        Title = $"{this.vehicle.Name} - Meter Display";
        VehicleNameTextBlock.Text = this.vehicle.Name;
        TransmissionTextBlock.Text = this.vehicle.Transmission;
        ConfigureMeterDisplay();
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
        noteE13RuntimeState?.SetAcceleratorPressed(false);
        noteE13RuntimeState?.SetBrakePressed(false);
        StopNoteSeatbeltWarningTimer();
        StopNoteLoopingSounds();
        StopUpdateTimer();
        engineAudioService.Dispose();
        noteE13AudioService.Dispose();
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        runtimeState.SetAcceleratorPressed(false);
        runtimeState.SetBrakePressed(false);
        noteE13RuntimeState?.SetAcceleratorPressed(false);
        noteE13RuntimeState?.SetBrakePressed(false);
        if (usesNoteE13InstrumentCluster)
        {
            StopNoteSeatbeltWarningTimer();
            StopNoteLoopingSounds();
        }
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
                    GetAudioControlRpm(),
                    runtimeState.ThrottlePercent,
                    audioService.IsMuted,
                    audioService.MasterVolume,
                    IsAudioPreviewActive());
                UpdateNoteE13Audio(0.0);
                ShowTransientStatusMessage(audioService.IsMuted ? "Audio Muted" : "Audio Unmuted");
                UpdateRuntimeDisplay();
            }

            e.Handled = true;
            return;
        }

        if (IsVolumeUpKey(e.Key))
        {
            ChangeMasterVolume(0.05);
            e.Handled = true;
            return;
        }

        if (IsVolumeDownKey(e.Key))
        {
            ChangeMasterVolume(-0.05);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.V)
        {
            if (!e.IsRepeat)
            {
                if (usesNoteE13InstrumentCluster)
                {
                    ShowTransientStatusMessage("Audio Debug is not used for Nissan Note e-POWER E13");
                    UpdateRuntimeDisplay();
                    e.Handled = true;
                    return;
                }

                isAudioDebugPanelVisible = !isAudioDebugPanelVisible;
                ShowTransientStatusMessage($"Audio Debug Panel: {(isAudioDebugPanelVisible ? "ON" : "OFF")}");
                UpdateRuntimeDisplay();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.F2)
        {
            if (!e.IsRepeat)
            {
                if (usesNoteE13InstrumentCluster)
                {
                    ShowTransientStatusMessage("Audio Tuning is not used for Nissan Note e-POWER E13");
                    UpdateRuntimeDisplay();
                    e.Handled = true;
                    return;
                }

                ToggleAudioTuningPanel();
            }

            e.Handled = true;
            return;
        }

        if (HandleLfaDisplayKey(e))
        {
            e.Handled = true;
            return;
        }

        if (HandleNoteE13Key(e))
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Key.I)
        {
            if (IsStartupSweepActive)
            {
                CancelStartupSweep();
                runtimeState.ToggleIgnition();
                DisableAudioPreview();
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
                DisableAudioPreview();
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
                DisableAudioPreview();
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
        if (usesNoteE13InstrumentCluster && noteE13RuntimeState is not null)
        {
            if (e.Key == Key.A)
            {
                noteE13RuntimeState.SetAcceleratorPressed(false);
                UpdateRuntimeDisplay();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Z)
            {
                noteE13RuntimeState.SetBrakePressed(false);
                UpdateRuntimeDisplay();
                e.Handled = true;
                return;
            }
        }

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
        UpdateLfaBlinkState(updateTimer.Interval.TotalMilliseconds);
        if (usesNoteE13InstrumentCluster && noteE13RuntimeState is not null)
        {
            noteE13RuntimeState.Update(updateTimer.Interval.TotalSeconds);
            if (noteSelfCheckTicks > 0)
            {
                noteSelfCheckTicks--;
            }

            UpdateNoteE13Audio(updateTimer.Interval.TotalMilliseconds);

            engineAudioService.UpdateEngineSound(
                false,
                0,
                0,
                0,
                audioService.IsMuted,
                audioService.MasterVolume,
                false);
            UpdateRuntimeDisplay();
            TickTransientStatusMessage();
            return;
        }

        if (IsStartupSweepActive)
        {
            UpdateStartupSweep(updateTimer.Interval.TotalMilliseconds);
            engineAudioService.UpdateEngineSound(
                false,
                runtimeState.CurrentRpm,
                runtimeState.CurrentRpm,
                runtimeState.ThrottlePercent,
                audioService.IsMuted,
                audioService.MasterVolume,
                false);
            UpdateRuntimeDisplay();
            TickTransientStatusMessage();
            return;
        }

        runtimeState.UpdateSimulation(vehicle, updateTimer.Interval.TotalSeconds);
        if (!runtimeState.IsEngineRunning || !runtimeState.IsIgnitionOn || !engineAudioService.IsAvailable)
        {
            DisableAudioPreview();
        }

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
            GetAudioControlRpm(),
            runtimeState.ThrottlePercent,
            audioService.IsMuted,
            audioService.MasterVolume,
            IsAudioPreviewActive());
        UpdateRuntimeDisplay();
        TickTransientStatusMessage();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        runtimeState.SetAcceleratorPressed(false);
        runtimeState.SetBrakePressed(false);
        noteE13RuntimeState?.SetAcceleratorPressed(false);
        noteE13RuntimeState?.SetBrakePressed(false);
        StopNoteSeatbeltWarningTimer();
        StopNoteLoopingSounds();
        StopUpdateTimer();
        DisableAudioPreview();
        engineAudioService.Dispose();
        noteE13AudioService.Dispose();

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
        if (usesNoteE13InstrumentCluster && noteE13RuntimeState is not null)
        {
            UpdateNoteE13InstrumentCluster();
            UpdateAudioTuningDisplay();
            UpdateAudioDebugDisplay();
            return;
        }

        var currentDrivingMode = runtimeState.GetCurrentDrivingMode(vehicle);

        if (usesLfaInstrumentCluster)
        {
            UpdateLfaInstrumentCluster(currentDrivingMode);
        }
        else
        {
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
        }

        ThrottleText.Text = $"Throttle: {runtimeState.ThrottlePercent}%";
        BrakeText.Text = $"Brake: {runtimeState.BrakePercent}%";
        ParkingBrakeStatusText.Text = $"Parking Brake: {(runtimeState.IsParkingBrakeApplied ? "ON" : "OFF")}";
        DriveModeText.Text = currentDrivingMode.DisplayName;
        TransmissionModeText.Text = runtimeState.GetTransmissionModeDisplayName();
        UpdateAudioStatusDisplay();
        UpdateEngineAudioStatusDisplay();
        UpdateMasterVolumeDisplay();
        UpdateAudioTuningDisplay();
        UpdateAudioDebugDisplay();
        SystemMessageText.Text = sweepStatusMessage ?? transientStatusMessage ?? runtimeState.SystemMessage;

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
            AudioStatusText.Text = "Event Audio: No Files";
            AudioStatusText.Foreground = InactiveTextBrush;
            AudioStatusBorder.BorderBrush = InactiveBorderBrush;
            return;
        }

        if (audioService.IsMuted)
        {
            AudioStatusText.Text = "Event Audio: MUTED";
            AudioStatusText.Foreground = ActiveMessageBrush;
            AudioStatusBorder.BorderBrush = ActiveBorderBrush;
            return;
        }

        AudioStatusText.Text = "Event Audio: ON";
        AudioStatusText.Foreground = ThrottleInactiveBrush;
        AudioStatusBorder.BorderBrush = InactiveBorderBrush;
    }

    private void UpdateEngineAudioStatusDisplay()
    {
        if (!engineAudioService.IsAvailable)
        {
            EngineAudioStatusText.Text = "NO AUDIO FILE";
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

        if (engineAudioService.PlaybackMode == EngineAudioPlaybackMode.MultiLayerCrossfade)
        {
            EngineAudioStatusText.Text = runtimeState.IsEngineRunning && engineAudioService.IsPlaying
                ? "MULTI-LAYER - PLAYING"
                : $"MULTI-LAYER READY ({engineAudioService.ActiveLayerCount})";
            EngineAudioStatusText.Foreground = AutoModeTextBrush;
            EngineAudioStatusBorder.BorderBrush = AutoModeBorderBrush;
            return;
        }

        EngineAudioStatusText.Text = runtimeState.IsEngineRunning && engineAudioService.IsPlaying
            ? "SINGLE LOOP - PLAYING"
            : "SINGLE LOOP";
        EngineAudioStatusText.Foreground = ThrottleInactiveBrush;
        EngineAudioStatusBorder.BorderBrush = InactiveBorderBrush;
    }

    private void UpdateMasterVolumeDisplay()
    {
        var percent = (int)Math.Round(audioService.MasterVolume * 100.0);
        MasterVolumeText.Text = audioService.IsMuted
            ? $"{percent}% (MUTED)"
            : $"{percent}%";

        if (audioService.IsMuted)
        {
            MasterVolumeText.Foreground = ActiveMessageBrush;
            MasterVolumeStatusBorder.BorderBrush = ActiveBorderBrush;
        }
        else
        {
            MasterVolumeText.Foreground = ThrottleInactiveBrush;
            MasterVolumeStatusBorder.BorderBrush = InactiveBorderBrush;
        }
    }

    private void UpdateAudioDebugDisplay()
    {
        AudioDebugPanel.Visibility = isAudioDebugPanelVisible ? Visibility.Visible : Visibility.Collapsed;
        if (!isAudioDebugPanelVisible)
        {
            return;
        }

        var debugInfo = engineAudioService.GetDebugInfo();
        var builder = new StringBuilder();
        builder.AppendLine($"Playback Mode : {debugInfo.PlaybackMode}");
        builder.AppendLine($"Audio File    : {(debugInfo.IsAudioAvailable ? "Available" : "Not Available")}");
        builder.AppendLine($"Actual RPM    : {Math.Round(debugInfo.ActualVehicleRpm):0}");
        builder.AppendLine($"Audio RPM     : {Math.Round(debugInfo.AudioControlRpm):0}");
        builder.AppendLine($"Preview Active: {debugInfo.IsPreviewActive}");
        builder.AppendLine($"Master Volume : {Math.Round(debugInfo.MasterVolume * 100.0):0}%");
        builder.AppendLine($"Muted         : {debugInfo.IsMuted}");
        builder.AppendLine($"Playing       : {debugInfo.IsPlaying}");
        builder.AppendLine($"Pitch Factor  : {debugInfo.CurrentPitchFactor:0.00}");
        builder.AppendLine($"Output Volume : {debugInfo.CurrentOutputVolume:0.00}");

        if (debugInfo.Layers.Count > 0)
        {
            builder.AppendLine($"Layers        : {debugInfo.Layers.Count}");
            foreach (var layer in debugInfo.Layers)
            {
                builder.AppendLine($"{layer.Id,-6} Gain: {layer.CurrentGain:0.00}  Pitch: {layer.CurrentPitchFactor:0.00}");
            }
        }

        AudioDebugText.Text = builder.ToString();
    }

    private void ChangeMasterVolume(double delta)
    {
        audioService.MasterVolume += delta;
        engineAudioService.UpdateEngineSound(
            runtimeState.IsEngineRunning,
            runtimeState.CurrentRpm,
            GetAudioControlRpm(),
            runtimeState.ThrottlePercent,
            audioService.IsMuted,
            audioService.MasterVolume,
            IsAudioPreviewActive());
        UpdateNoteE13Audio(0.0);
        ShowTransientStatusMessage($"Master Volume: {Math.Round(audioService.MasterVolume * 100.0):0}%");
        UpdateRuntimeDisplay();
    }

    private void ShowTransientStatusMessage(string message)
    {
        transientStatusMessage = message;
        transientStatusMessageTicks = 40;
    }

    private void TickTransientStatusMessage()
    {
        if (transientStatusMessageTicks <= 0)
        {
            transientStatusMessage = null;
            return;
        }

        transientStatusMessageTicks--;
        if (transientStatusMessageTicks == 0)
        {
            transientStatusMessage = null;
        }
    }

    private static bool IsVolumeUpKey(Key key)
    {
        return key is Key.OemPlus or Key.Add or Key.PageUp;
    }

    private static bool IsVolumeDownKey(Key key)
    {
        return key is Key.OemMinus or Key.Subtract or Key.PageDown;
    }

    private void PlayVehicleSound(string? relativePath)
    {
        audioService.PlaySound(relativePath);
    }

    private void AudioTuningPanel_TuningChanged(object? sender, EventArgs e)
    {
        if (audioTuningSession.IsPreviewEnabled && !CanUseAudioPreview())
        {
            audioTuningSession.IsPreviewEnabled = false;
            ShowTransientStatusMessage(runtimeState.IsEngineRunning
                ? "Engine audio file is not available"
                : "Start engine before audio preview");
        }

        ApplyAudioTuningSettings();
        UpdateRuntimeDisplay();
    }

    private void AudioTuningPanel_ResetRequested(object? sender, EventArgs e)
    {
        audioTuningSession.ResetFromVehicle(vehicle);
        ApplyAudioTuningSettings();
        AudioTuningPanel.Initialize(audioTuningSession, vehicle);
        ShowTransientStatusMessage("Audio tuning values reset");
        UpdateRuntimeDisplay();
    }

    private void AudioTuningPanel_ExportRequested(object? sender, EventArgs e)
    {
        try
        {
            var outputPath = audioTuningExportService.Export(audioTuningSession);
            ShowTransientStatusMessage($"Audio tuning exported: {System.IO.Path.GetFileName(outputPath)}");
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or NotSupportedException)
        {
            ShowTransientStatusMessage($"Audio tuning export failed: {ex.Message}");
        }

        UpdateRuntimeDisplay();
    }

    private void ToggleAudioTuningPanel()
    {
        audioTuningSession.IsTuningPanelVisible = !audioTuningSession.IsTuningPanelVisible;
        if (!audioTuningSession.IsTuningPanelVisible)
        {
            DisableAudioPreview();
            ShowTransientStatusMessage("Audio Tuning Mode Disabled");
        }
        else
        {
            ShowTransientStatusMessage("Audio Tuning Mode Enabled");
        }

        UpdateRuntimeDisplay();
    }

    private void UpdateAudioTuningDisplay()
    {
        AudioTuningPanel.Visibility = audioTuningSession.IsTuningPanelVisible ? Visibility.Visible : Visibility.Collapsed;
        AudioTuningPanel.SetPlaybackMode(engineAudioService.PlaybackMode.ToString());
        AudioTuningPanel.SetPreviewAvailability(runtimeState.IsEngineRunning, engineAudioService.IsAvailable);
    }

    private void ApplyAudioTuningSettings()
    {
        engineAudioService.ApplyTuningSettings(audioTuningSession);
    }

    private bool IsAudioPreviewActive()
    {
        return audioTuningSession.IsTuningPanelVisible
            && audioTuningSession.IsPreviewEnabled
            && runtimeState.IsEngineRunning
            && engineAudioService.IsAvailable;
    }

    private bool CanUseAudioPreview()
    {
        return audioTuningSession.IsTuningPanelVisible
            && runtimeState.IsEngineRunning
            && engineAudioService.IsAvailable;
    }

    private double GetAudioControlRpm()
    {
        return IsAudioPreviewActive()
            ? audioTuningSession.PreviewRpm
            : runtimeState.CurrentRpm;
    }

    private void DisableAudioPreview()
    {
        if (audioTuningSession.IsPreviewEnabled)
        {
            audioTuningSession.IsPreviewEnabled = false;
        }
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

    private static bool IsLfaInstrumentClusterVehicle(VehicleProfile selectedVehicle)
    {
        return string.Equals(selectedVehicle.Id, "lexus-lfa", StringComparison.OrdinalIgnoreCase)
            || string.Equals(selectedVehicle.MeterStyleId, "lfa-authentic", StringComparison.OrdinalIgnoreCase);
    }

    private void ConfigureMeterDisplay()
    {
        CombustionKeyGuidePanel.Visibility = usesNoteE13InstrumentCluster ? Visibility.Collapsed : Visibility.Visible;
        LfaKeyGuidePanel.Visibility = usesLfaInstrumentCluster ? Visibility.Visible : Visibility.Collapsed;
        NoteKeyGuidePanel.Visibility = usesNoteE13InstrumentCluster ? Visibility.Visible : Visibility.Collapsed;

        if (usesNoteE13InstrumentCluster)
        {
            PowertrainStatusPanel.Visibility = Visibility.Collapsed;
            GenericMeterPanel.Visibility = Visibility.Collapsed;
            InputStatusPanel.Visibility = Visibility.Collapsed;
            LfaInstrumentCluster.Visibility = Visibility.Collapsed;
            NoteE13InstrumentCluster.Visibility = Visibility.Visible;
            return;
        }

        if (usesLfaInstrumentCluster)
        {
            PowertrainStatusPanel.Visibility = Visibility.Collapsed;
            GenericMeterPanel.Visibility = Visibility.Collapsed;
            InputStatusPanel.Visibility = Visibility.Collapsed;
            LfaInstrumentCluster.Visibility = Visibility.Visible;
            NoteE13InstrumentCluster.Visibility = Visibility.Collapsed;
            LfaInstrumentCluster.ConfigureScale(vehicle.MaxRpm, vehicle.RevLimiterRpm);
            return;
        }

        LfaInstrumentCluster.Visibility = Visibility.Collapsed;
        NoteE13InstrumentCluster.Visibility = Visibility.Collapsed;
        MainTachometer.ConfigureStyle(
            vehicle.MeterStyleId,
            vehicle.Name,
            vehicle.MaxRpm,
            vehicle.RevLimiterRpm);
    }

    private bool HandleLfaDisplayKey(KeyEventArgs e)
    {
        if (!usesLfaInstrumentCluster || e.IsRepeat)
        {
            return false;
        }

        switch (e.Key)
        {
            case Key.Q:
                isLfaLeftTurnEnabled = !isLfaLeftTurnEnabled;
                if (isLfaLeftTurnEnabled)
                {
                    isLfaRightTurnEnabled = false;
                    isLfaHazardEnabled = false;
                }

                ShowTransientStatusMessage($"LFA Left Turn Indicator: {(isLfaLeftTurnEnabled ? "ON" : "OFF")}");
                break;
            case Key.E:
                isLfaRightTurnEnabled = !isLfaRightTurnEnabled;
                if (isLfaRightTurnEnabled)
                {
                    isLfaLeftTurnEnabled = false;
                    isLfaHazardEnabled = false;
                }

                ShowTransientStatusMessage($"LFA Right Turn Indicator: {(isLfaRightTurnEnabled ? "ON" : "OFF")}");
                break;
            case Key.H:
                isLfaHazardEnabled = !isLfaHazardEnabled;
                if (isLfaHazardEnabled)
                {
                    isLfaLeftTurnEnabled = false;
                    isLfaRightTurnEnabled = false;
                }

                ShowTransientStatusMessage($"LFA Hazard Lamps: {(isLfaHazardEnabled ? "ON" : "OFF")}");
                break;
            case Key.L:
                isLfaTailLampEnabled = !isLfaTailLampEnabled;
                ShowTransientStatusMessage($"LFA Tail Lamp Indicator: {(isLfaTailLampEnabled ? "ON" : "OFF")}");
                break;
            case Key.B:
                isLfaHighBeamEnabled = !isLfaHighBeamEnabled;
                ShowTransientStatusMessage($"LFA High Beam Indicator: {(isLfaHighBeamEnabled ? "ON" : "OFF")}");
                break;
            case Key.F3:
                isLfaLampTestActive = !isLfaLampTestActive;
                ShowTransientStatusMessage($"LFA Lamp Test Panel: {(isLfaLampTestActive ? "ON" : "OFF")}");
                break;
            case Key.F4:
                isLfaMenuPreviewActive = !isLfaMenuPreviewActive;
                ShowTransientStatusMessage($"LFA Menu Screen Preview: {(isLfaMenuPreviewActive ? "ON" : "OFF")}");
                break;
            default:
                return false;
        }

        UpdateRuntimeDisplay();
        return true;
    }

    private void UpdateLfaBlinkState(double elapsedMilliseconds)
    {
        if (!usesLfaInstrumentCluster)
        {
            return;
        }

        lfaBlinkElapsedMilliseconds += elapsedMilliseconds;
        if (lfaBlinkElapsedMilliseconds < 500.0)
        {
            return;
        }

        lfaBlinkElapsedMilliseconds = 0.0;
        lfaBlinkVisible = !lfaBlinkVisible;
    }

    private void UpdateLfaInstrumentCluster(DrivingModeProfile currentDrivingMode)
    {
        var leftTurnVisible = lfaBlinkVisible && (isLfaLeftTurnEnabled || isLfaHazardEnabled);
        var rightTurnVisible = lfaBlinkVisible && (isLfaRightTurnEnabled || isLfaHazardEnabled);
        var displayMessage = sweepStatusMessage ?? transientStatusMessage ?? runtimeState.SystemMessage;

        LfaInstrumentCluster.UpdateCluster(
            runtimeState.CurrentRpm,
            IsStartupSweepActive ? displayedNeedleRpm : runtimeState.CurrentRpm,
            runtimeState.CurrentSpeedKmh,
            runtimeState.CurrentGear,
            currentDrivingMode.DisplayName,
            runtimeState.GetTransmissionModeDisplayName(),
            displayMessage,
            runtimeState.IsIgnitionOn,
            runtimeState.IsEngineRunning,
            IsStartupSweepActive,
            runtimeState.IsParkingBrakeApplied,
            ShouldShowShiftUpIndicator(),
            leftTurnVisible,
            rightTurnVisible,
            isLfaTailLampEnabled,
            isLfaHighBeamEnabled,
            isLfaLampTestActive,
            isLfaMenuPreviewActive,
            currentDrivingMode.AccentStyleId,
            vehicle.MaxRpm,
            vehicle.RevLimiterRpm);
    }

    private bool HandleNoteE13Key(KeyEventArgs e)
    {
        if (!usesNoteE13InstrumentCluster || noteE13RuntimeState is null || e.IsRepeat)
        {
            return false;
        }

        switch (e.Key)
        {
            case Key.I:
                var wasOff = noteE13RuntimeState.PowerState == E13PowerState.Off;
                noteE13RuntimeState.PressPowerSwitch();
                if (wasOff && noteE13RuntimeState.PowerState != E13PowerState.Off)
                {
                    noteSelfCheckTicks = 24;
                    PlayVehicleSound(vehicle.AudioProfile?.IgnitionOnSound);
                    ScheduleNoteSeatbeltWarning();
                }
                else if (!wasOff && noteE13RuntimeState.PowerState == E13PowerState.Off)
                {
                    StopNoteSeatbeltWarningTimer();
                    StopNoteLoopingSounds();
                    PlayVehicleSound(vehicle.AudioProfile?.SystemStopSound ?? vehicle.AudioProfile?.IgnitionOffSound);
                }

                break;
            case Key.A:
                noteE13RuntimeState.SetAcceleratorPressed(true);
                break;
            case Key.Z:
                noteE13RuntimeState.SetBrakePressed(true);
                break;
            case Key.P:
                noteE13RuntimeState.SelectShiftPosition(E13ShiftPosition.P);
                break;
            case Key.R:
                var wasReverse = noteE13RuntimeState.ShiftPosition == E13ShiftPosition.R;
                noteE13RuntimeState.SelectShiftPosition(E13ShiftPosition.R);
                if (!wasReverse && noteE13RuntimeState.ShiftPosition == E13ShiftPosition.R)
                {
                    PlayVehicleSound(vehicle.AudioProfile?.ReverseEngageSound);
                }

                break;
            case Key.N:
                noteE13RuntimeState.SelectShiftPosition(E13ShiftPosition.N);
                break;
            case Key.D:
                noteE13RuntimeState.SelectShiftPosition(E13ShiftPosition.D);
                break;
            case Key.B:
                noteE13RuntimeState.SelectShiftPosition(E13ShiftPosition.B);
                break;
            case Key.K:
                noteE13RuntimeState.ToggleElectricParkingBrake();
                break;
            case Key.O:
                noteE13RuntimeState.ToggleAutoBrakeHold();
                break;
            case Key.C:
                noteE13RuntimeState.CycleDriveMode();
                break;
            case Key.G:
                noteE13RuntimeState.ToggleChargeMode();
                break;
            case Key.Y:
                noteE13RuntimeState.ToggleMannerMode();
                break;
            case Key.Q:
                isNoteLeftTurnEnabled = !isNoteLeftTurnEnabled;
                if (isNoteLeftTurnEnabled)
                {
                    isNoteRightTurnEnabled = false;
                    isNoteHazardEnabled = false;
                }

                ShowTransientStatusMessage($"Left Turn Indicator: {(isNoteLeftTurnEnabled ? "ON" : "OFF")}");
                break;
            case Key.E:
                isNoteRightTurnEnabled = !isNoteRightTurnEnabled;
                if (isNoteRightTurnEnabled)
                {
                    isNoteLeftTurnEnabled = false;
                    isNoteHazardEnabled = false;
                }

                ShowTransientStatusMessage($"Right Turn Indicator: {(isNoteRightTurnEnabled ? "ON" : "OFF")}");
                break;
            case Key.H:
                isNoteHazardEnabled = !isNoteHazardEnabled;
                if (isNoteHazardEnabled)
                {
                    isNoteLeftTurnEnabled = false;
                    isNoteRightTurnEnabled = false;
                }

                ShowTransientStatusMessage($"Hazard Lamps: {(isNoteHazardEnabled ? "ON" : "OFF")}");
                break;
            case Key.L:
                isNoteTailLampEnabled = !isNoteTailLampEnabled;
                ShowTransientStatusMessage($"Tail Lamp Indicator: {(isNoteTailLampEnabled ? "ON" : "OFF")}");
                break;
            case Key.U:
                isNoteHighBeamEnabled = !isNoteHighBeamEnabled;
                ShowTransientStatusMessage($"High Beam Indicator: {(isNoteHighBeamEnabled ? "ON" : "OFF")}");
                break;
            case Key.F3:
                isNotePreviewPanelVisible = !isNotePreviewPanelVisible;
                ShowTransientStatusMessage($"E13 State Preview Panel: {(isNotePreviewPanelVisible ? "ON" : "OFF")}");
                break;
            default:
                return false;
        }

        UpdateRuntimeDisplay();
        return true;
    }

    private void UpdateNoteE13Audio(double elapsedMilliseconds)
    {
        if (!usesNoteE13InstrumentCluster || noteE13RuntimeState is null)
        {
            return;
        }

        noteE13AudioService.Update(
            noteE13RuntimeState.PowerState,
            noteE13RuntimeState.ShiftPosition,
            noteE13RuntimeState.CurrentSpeedKmh,
            noteE13RuntimeState.IsElectricParkingBrakeOn,
            noteE13RuntimeState.IsAutoBrakeHoldHolding,
            isNoteLeftTurnEnabled,
            isNoteRightTurnEnabled,
            isNoteHazardEnabled,
            elapsedMilliseconds,
            audioService.IsMuted,
            audioService.MasterVolume);
    }

    private void ScheduleNoteSeatbeltWarning()
    {
        StopNoteSeatbeltWarningTimer();

        if (!usesNoteE13InstrumentCluster
            || noteE13RuntimeState is null
            || string.IsNullOrWhiteSpace(vehicle.AudioProfile?.SeatbeltWarningSound))
        {
            return;
        }

        var startupDuration = audioService.GetSoundDuration(vehicle.AudioProfile.IgnitionOnSound) ?? TimeSpan.Zero;
        noteSeatbeltWarningTimer = new DispatcherTimer
        {
            Interval = startupDuration + TimeSpan.FromSeconds(1)
        };

        noteSeatbeltWarningTimer.Tick += (_, _) =>
        {
            StopNoteSeatbeltWarningTimer();
            if (noteE13RuntimeState.PowerState != E13PowerState.Off)
            {
                PlayVehicleSound(vehicle.AudioProfile?.SeatbeltWarningSound);
            }
        };
        noteSeatbeltWarningTimer.Start();
    }

    private void StopNoteSeatbeltWarningTimer()
    {
        if (noteSeatbeltWarningTimer is null)
        {
            return;
        }

        noteSeatbeltWarningTimer.Stop();
        noteSeatbeltWarningTimer = null;
    }

    private void StopNoteLoopingSounds()
    {
        noteE13AudioService.StopAll();
    }

    private void UpdateNoteE13InstrumentCluster()
    {
        if (noteE13RuntimeState is null)
        {
            return;
        }

        AudioTuningPanel.Visibility = Visibility.Collapsed;
        AudioDebugPanel.Visibility = Visibility.Collapsed;

        var turnSignalLampVisible = noteE13AudioService.IsTurnSignalLampVisible;
        var leftTurnVisible = turnSignalLampVisible && (isNoteLeftTurnEnabled || isNoteHazardEnabled);
        var rightTurnVisible = turnSignalLampVisible && (isNoteRightTurnEnabled || isNoteHazardEnabled);
        NoteE13InstrumentCluster.UpdateCluster(
            noteE13RuntimeState,
            transientStatusMessage ?? noteE13RuntimeState.SystemMessage,
            noteSelfCheckTicks > 0,
            leftTurnVisible,
            rightTurnVisible,
            isNoteTailLampEnabled,
            isNoteHighBeamEnabled,
            isNotePreviewPanelVisible,
            noteE13AudioService.TurnSignalStatus,
            noteE13AudioService.ReverseStatus,
            noteE13AudioService.ForwardApproachStatus);
    }
}
