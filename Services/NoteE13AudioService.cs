using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using NAudio.Wave;
using VehicleMeterSimulator.Models;

namespace VehicleMeterSimulator.Services;

public sealed class NoteE13AudioService : IDisposable
{
    private readonly string soundsDirectory;
    private WaveOutEvent? turnSignalOutput;
    private AudioFileReader? turnSignalReader;
    private MediaPlayer? reverseLoopPlayer;
    private MediaPlayer? forwardApproachLoopPlayer;
    private NoteE13AudioProfile? audioProfile;
    private string? turnSignalSoundPath;
    private string? reverseLoopSoundPath;
    private string? forwardApproachLoopSoundPath;
    private bool wasTurnSignalRequested;
    private double turnSignalElapsedMs;
    private double turnSignalLampElapsedMs;
    private double currentReverseVolume;
    private double currentForwardApproachVolume;

    public NoteE13AudioService()
        : this(Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds"))
    {
    }

    public NoteE13AudioService(string soundsDirectory)
    {
        this.soundsDirectory = Path.GetFullPath(soundsDirectory);
    }

    public bool IsTurnSignalFileAvailable { get; private set; }

    public bool IsReverseFileAvailable { get; private set; }

    public bool IsForwardApproachFileAvailable { get; private set; }

    public bool IsTurnSignalPlaying { get; private set; }

    public bool IsReversePlaying { get; private set; }

    public bool IsForwardApproachPlaying { get; private set; }

    public bool IsTurnSignalLampVisible { get; private set; }

    public string TurnSignalStatus { get; private set; } = "NO FILE";

    public string ReverseStatus { get; private set; } = "NO FILE";

    public string ForwardApproachStatus { get; private set; } = "NO FILE";

    public void Initialize(NoteE13AudioProfile? profile)
    {
        StopAll();
        audioProfile = profile;
        turnSignalSoundPath = ResolveAvailableSoundPath(profile?.TurnSignalSound);
        reverseLoopSoundPath = ResolveAvailableSoundPath(profile?.ReverseLoopSound);
        forwardApproachLoopSoundPath = ResolveAvailableSoundPath(profile?.ForwardApproachLoopSound);
        IsTurnSignalFileAvailable = turnSignalSoundPath is not null;
        IsReverseFileAvailable = reverseLoopSoundPath is not null;
        IsForwardApproachFileAvailable = forwardApproachLoopSoundPath is not null;
        UpdateStatuses(false, false, false, false, 1.0);
    }

    public void Update(
        E13PowerState powerState,
        E13ShiftPosition shiftPosition,
        double currentSpeedKmh,
        bool isElectricParkingBrakeOn,
        bool isAutoBrakeHoldHolding,
        bool isLeftTurnRequested,
        bool isRightTurnRequested,
        bool isHazardRequested,
        double elapsedMilliseconds,
        bool isMuted,
        double masterVolume)
    {
        var safeMasterVolume = Math.Clamp(masterVolume, 0.0, 1.0);
        var isTurnSignalRequested = powerState != E13PowerState.Off
            && (isLeftTurnRequested || isRightTurnRequested || isHazardRequested);
        var isReverseRequested = powerState == E13PowerState.Ready
            && shiftPosition == E13ShiftPosition.R;
        var isForwardApproachRequested = powerState == E13PowerState.Ready
            && shiftPosition is E13ShiftPosition.D or E13ShiftPosition.B
            && currentSpeedKmh >= (audioProfile?.ForwardApproachStartSpeedKmh ?? 0.5)
            && currentSpeedKmh < (audioProfile?.ForwardApproachStopSpeedKmh ?? 25.0)
            && !isElectricParkingBrakeOn
            && !isAutoBrakeHoldHolding;

        UpdateTurnSignal(isTurnSignalRequested, elapsedMilliseconds, isMuted, safeMasterVolume);
        UpdateReverseLoop(isReverseRequested, isMuted, safeMasterVolume);
        UpdateForwardApproachLoop(
            isForwardApproachRequested,
            currentSpeedKmh,
            elapsedMilliseconds,
            isMuted,
            safeMasterVolume);
        UpdateStatuses(
            isTurnSignalRequested,
            isReverseRequested,
            isForwardApproachRequested,
            isMuted,
            safeMasterVolume);
    }

    public void StopAll()
    {
        StopTurnSignalPlayer();
        StopReverseLoop();
        StopForwardApproachLoop();
        IsTurnSignalPlaying = false;
        IsReversePlaying = false;
        IsForwardApproachPlaying = false;
        IsTurnSignalLampVisible = false;
        wasTurnSignalRequested = false;
        turnSignalElapsedMs = 0.0;
        turnSignalLampElapsedMs = 0.0;
        currentReverseVolume = 0.0;
        currentForwardApproachVolume = 0.0;
    }

    public void Dispose()
    {
        StopAll();
    }

    private void UpdateTurnSignal(
        bool isRequested,
        double elapsedMilliseconds,
        bool isMuted,
        double masterVolume)
    {
        if (!isRequested)
        {
            StopTurnSignalPlayer();
            IsTurnSignalPlaying = false;
            IsTurnSignalLampVisible = false;
            wasTurnSignalRequested = false;
            turnSignalElapsedMs = 0.0;
            turnSignalLampElapsedMs = 0.0;
            return;
        }

        var profile = audioProfile;
        var period = Math.Max(2, profile?.TurnSignalPeriodMs ?? 700);
        var lampPeriod = Math.Max(2, profile?.TurnSignalLampPeriodMs > 0 ? profile.TurnSignalLampPeriodMs : period);
        var requestedLampOnMs = profile?.TurnSignalLampOnMs ?? 350;
        var lampOnMs = Math.Clamp(requestedLampOnMs, 1, lampPeriod - 1);
        var clickPlaybackMs = Math.Clamp(profile?.TurnSignalClickPlaybackMs ?? 180, 1, period - 1);

        if (!wasTurnSignalRequested)
        {
            turnSignalElapsedMs = 0.0;
            turnSignalLampElapsedMs = 0.0;
            PlayTurnSignalClick(isMuted, masterVolume);
        }
        else
        {
            var safeElapsedMilliseconds = Math.Max(0.0, elapsedMilliseconds);
            turnSignalElapsedMs += safeElapsedMilliseconds;
            turnSignalLampElapsedMs += safeElapsedMilliseconds;
            while (turnSignalElapsedMs >= period)
            {
                turnSignalElapsedMs -= period;
                PlayTurnSignalClick(isMuted, masterVolume);
            }

            while (turnSignalLampElapsedMs >= lampPeriod)
            {
                turnSignalLampElapsedMs -= lampPeriod;
            }
        }

        IsTurnSignalLampVisible = turnSignalLampElapsedMs < lampOnMs;
        if (turnSignalElapsedMs >= clickPlaybackMs || isMuted)
        {
            StopTurnSignalPlayback();
        }

        IsTurnSignalPlaying = IsTurnSignalFileAvailable && !isMuted;
        wasTurnSignalRequested = true;
    }

    private void PlayTurnSignalClick(bool isMuted, double masterVolume)
    {
        if (isMuted || turnSignalSoundPath is null)
        {
            return;
        }

        try
        {
            if (turnSignalReader is null || turnSignalOutput is null)
            {
                turnSignalReader = new AudioFileReader(turnSignalSoundPath);
                turnSignalOutput = new WaveOutEvent();
                turnSignalOutput.Init(turnSignalReader);
            }

            turnSignalOutput.Stop();
            turnSignalReader.Position = 0;
            turnSignalReader.Volume = (float)Math.Clamp((audioProfile?.TurnSignalVolume ?? 0.85) * masterVolume, 0.0, 1.0);
            turnSignalOutput.Play();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or NotSupportedException or System.Runtime.InteropServices.ExternalException)
        {
            Debug.WriteLine($"Note E13 turn signal sound error: {ex.Message}");
            StopTurnSignalPlayer();
        }
    }

    private void UpdateReverseLoop(bool isRequested, bool isMuted, double masterVolume)
    {
        if (!isRequested || isMuted || reverseLoopSoundPath is null)
        {
            StopReverseLoop();
            currentReverseVolume = 0.0;
            return;
        }

        var targetVolume = Math.Clamp((audioProfile?.ReverseLoopVolume ?? 0.75) * masterVolume, 0.0, 1.0);
        currentReverseVolume += (targetVolume - currentReverseVolume) * GetReverseFadeStep();

        if (reverseLoopPlayer is null)
        {
            StartReverseLoop();
        }

        if (reverseLoopPlayer is not null)
        {
            reverseLoopPlayer.Volume = currentReverseVolume;
            IsReversePlaying = true;
        }
    }

    private void UpdateForwardApproachLoop(
        bool isRequested,
        double speedKmh,
        double elapsedMilliseconds,
        bool isMuted,
        double masterVolume)
    {
        if (isMuted || forwardApproachLoopSoundPath is null)
        {
            StopForwardApproachLoop();
            currentForwardApproachVolume = 0.0;
            return;
        }

        var targetVolume = isRequested
            ? GetForwardApproachTargetVolume(speedKmh) * masterVolume
            : 0.0;
        targetVolume = Math.Clamp(targetVolume, 0.0, 1.0);
        currentForwardApproachVolume += (targetVolume - currentForwardApproachVolume)
            * GetForwardApproachFadeStep(elapsedMilliseconds);

        if (isRequested && targetVolume > 0.0 && forwardApproachLoopPlayer is null)
        {
            StartForwardApproachLoop();
        }

        if (forwardApproachLoopPlayer is not null)
        {
            forwardApproachLoopPlayer.Volume = currentForwardApproachVolume;
            IsForwardApproachPlaying = currentForwardApproachVolume > 0.01;
            if (!isRequested && currentForwardApproachVolume <= 0.01)
            {
                StopForwardApproachLoop();
            }
        }
        else
        {
            IsForwardApproachPlaying = false;
        }
    }

    private void StartReverseLoop()
    {
        if (reverseLoopSoundPath is null)
        {
            return;
        }

        try
        {
            var player = new MediaPlayer
            {
                Volume = 0.0
            };

            player.MediaEnded += (_, _) =>
            {
                player.Position = TimeSpan.Zero;
                player.Play();
            };
            player.MediaFailed += (_, args) =>
            {
                Debug.WriteLine($"Note E13 reverse loop failed: {args.ErrorException.Message}");
                StopReverseLoop();
            };

            reverseLoopPlayer = player;
            player.Open(new Uri(reverseLoopSoundPath, UriKind.Absolute));
            player.Play();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UriFormatException)
        {
            Debug.WriteLine($"Note E13 reverse loop error: {ex.Message}");
            StopReverseLoop();
        }
    }

    private void StartForwardApproachLoop()
    {
        if (forwardApproachLoopSoundPath is null)
        {
            return;
        }

        try
        {
            var player = new MediaPlayer
            {
                Volume = 0.0
            };

            player.MediaEnded += (_, _) =>
            {
                player.Position = TimeSpan.Zero;
                player.Play();
            };
            player.MediaFailed += (_, args) =>
            {
                Debug.WriteLine($"Note E13 forward approach loop failed: {args.ErrorException.Message}");
                StopForwardApproachLoop();
            };

            forwardApproachLoopPlayer = player;
            player.Open(new Uri(forwardApproachLoopSoundPath, UriKind.Absolute));
            player.Play();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UriFormatException)
        {
            Debug.WriteLine($"Note E13 forward approach loop error: {ex.Message}");
            StopForwardApproachLoop();
        }
    }

    private double GetForwardApproachTargetVolume(double speedKmh)
    {
        var profile = audioProfile;
        var baseVolume = Math.Clamp(profile?.ForwardApproachBaseVolume ?? 0.65, 0.0, 1.0);
        var startSpeed = profile?.ForwardApproachStartSpeedKmh ?? 0.5;
        var fullVolumeSpeed = profile?.ForwardApproachFullVolumeSpeedKmh ?? 5.0;
        var fadeOutStartSpeed = profile?.ForwardApproachFadeOutStartSpeedKmh ?? 18.0;
        var stopSpeed = profile?.ForwardApproachStopSpeedKmh ?? 25.0;

        if (speedKmh < startSpeed)
        {
            return 0.0;
        }

        if (speedKmh < fullVolumeSpeed)
        {
            var progress = (speedKmh - startSpeed) / (fullVolumeSpeed - startSpeed);
            return baseVolume * Math.Clamp(progress, 0.0, 1.0);
        }

        if (speedKmh < fadeOutStartSpeed)
        {
            return baseVolume;
        }

        if (speedKmh < stopSpeed)
        {
            var progress = (speedKmh - fadeOutStartSpeed) / (stopSpeed - fadeOutStartSpeed);
            return baseVolume * (1.0 - Math.Clamp(progress, 0.0, 1.0));
        }

        return 0.0;
    }

    private double GetReverseFadeStep()
    {
        var fadeMilliseconds = audioProfile?.ReverseFadeMilliseconds ?? 80;
        return fadeMilliseconds <= 0 ? 1.0 : Math.Clamp(50.0 / fadeMilliseconds, 0.1, 1.0);
    }

    private double GetForwardApproachFadeStep(double elapsedMilliseconds)
    {
        var fadeMilliseconds = audioProfile?.ForwardApproachFadeMilliseconds ?? 180;
        if (fadeMilliseconds <= 0)
        {
            return 1.0;
        }

        return Math.Clamp(Math.Max(0.0, elapsedMilliseconds) / fadeMilliseconds, 0.08, 1.0);
    }

    private void StopReverseLoop()
    {
        if (reverseLoopPlayer is null)
        {
            IsReversePlaying = false;
            return;
        }

        reverseLoopPlayer.Stop();
        reverseLoopPlayer.Close();
        reverseLoopPlayer = null;
        IsReversePlaying = false;
    }

    private void StopForwardApproachLoop()
    {
        if (forwardApproachLoopPlayer is null)
        {
            IsForwardApproachPlaying = false;
            return;
        }

        forwardApproachLoopPlayer.Stop();
        forwardApproachLoopPlayer.Close();
        forwardApproachLoopPlayer = null;
        IsForwardApproachPlaying = false;
    }

    private void UpdateStatuses(
        bool isTurnSignalRequested,
        bool isReverseRequested,
        bool isForwardApproachRequested,
        bool isMuted,
        double masterVolume)
    {
        TurnSignalStatus = GetStatus(IsTurnSignalFileAvailable, isTurnSignalRequested, IsTurnSignalPlaying, isMuted, masterVolume, "PLAYING");
        ReverseStatus = GetStatus(IsReverseFileAvailable, isReverseRequested, IsReversePlaying, isMuted, masterVolume, "PLAYING");
        ForwardApproachStatus = GetStatus(
            IsForwardApproachFileAvailable,
            isForwardApproachRequested,
            IsForwardApproachPlaying,
            isMuted,
            masterVolume,
            "PLAYING");
    }

    private static string GetStatus(
        bool isFileAvailable,
        bool isRequested,
        bool isPlaying,
        bool isMuted,
        double masterVolume,
        string activeText)
    {
        if (!isFileAvailable)
        {
            return "NO FILE";
        }

        if (isMuted || masterVolume <= 0.0)
        {
            return "MUTED";
        }

        return isRequested && isPlaying ? activeText : "READY";
    }

    private string? ResolveAvailableSoundPath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        try
        {
            var fullPath = Path.GetFullPath(Path.Combine(soundsDirectory, relativePath));
            var rootWithSeparator = soundsDirectory.EndsWith(Path.DirectorySeparatorChar)
                ? soundsDirectory
                : soundsDirectory + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fullPath, soundsDirectory, StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"Rejected unsafe Note E13 audio path: {relativePath}");
                return null;
            }

            if (!File.Exists(fullPath))
            {
                Debug.WriteLine($"Note E13 audio file was not found: {relativePath}");
                return null;
            }

            return fullPath;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Debug.WriteLine($"Invalid Note E13 audio path: {relativePath}. {ex.Message}");
            return null;
        }
    }

    private void StopTurnSignalPlayer()
    {
        StopTurnSignalPlayback();
        turnSignalOutput?.Dispose();
        turnSignalOutput = null;
        turnSignalReader?.Dispose();
        turnSignalReader = null;
    }

    private void StopTurnSignalPlayback()
    {
        if (turnSignalOutput is null)
        {
            return;
        }

        turnSignalOutput.Stop();
        if (turnSignalReader is not null)
        {
            turnSignalReader.Position = 0;
        }
    }
}
