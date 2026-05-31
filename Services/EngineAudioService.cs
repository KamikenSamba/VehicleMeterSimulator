using System;
using System.Diagnostics;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using VehicleMeterSimulator.Models;

namespace VehicleMeterSimulator.Services;

public sealed class EngineAudioService : IDisposable
{
    private const double PitchSmoothingFactor = 0.15;
    private const double VolumeSmoothingFactor = 0.20;

    private readonly string soundsDirectory;
    private VehicleAudioProfile? audioProfile;
    private AudioFileReader? audioFileReader;
    private SmbPitchShiftingSampleProvider? pitchProvider;
    private WaveOutEvent? outputDevice;
    private double currentPitchFactor = 1.0;
    private double currentVolume = 0.0;

    public EngineAudioService()
        : this(Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds"))
    {
    }

    public EngineAudioService(string soundsDirectory)
    {
        this.soundsDirectory = Path.GetFullPath(soundsDirectory);
    }

    public bool IsAvailable { get; private set; }

    public bool IsPlaying => outputDevice?.PlaybackState == PlaybackState.Playing && currentVolume > 0.01;

    public void Initialize(VehicleAudioProfile? audioProfile)
    {
        Stop();
        DisposeAudioResources();

        this.audioProfile = audioProfile;
        IsAvailable = false;
        currentPitchFactor = 1.0;
        currentVolume = 0.0;

        if (audioProfile is null || string.IsNullOrWhiteSpace(audioProfile.EngineLoopSound))
        {
            return;
        }

        var fullPath = ResolveSoundPath(audioProfile.EngineLoopSound);
        if (fullPath is null || !File.Exists(fullPath))
        {
            Debug.WriteLine($"Engine loop file was not found: {audioProfile.EngineLoopSound}");
            return;
        }

        try
        {
            audioFileReader = new AudioFileReader(fullPath)
            {
                Volume = 0.0f
            };
            var loopProvider = new LoopingSampleProvider(audioFileReader);
            pitchProvider = new SmbPitchShiftingSampleProvider(loopProvider)
            {
                PitchFactor = 1.0f
            };
            outputDevice = new WaveOutEvent();
            outputDevice.Init(pitchProvider);
            IsAvailable = true;
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
        {
            Debug.WriteLine($"Engine loop initialization failed: {fullPath}. {ex.Message}");
            DisposeAudioResources();
        }
    }

    public void UpdateEngineSound(
        bool isEngineRunning,
        double currentRpm,
        int throttlePercent,
        bool isMuted)
    {
        if (!IsAvailable || audioProfile is null || audioFileReader is null || outputDevice is null || pitchProvider is null)
        {
            return;
        }

        try
        {
            if (!isEngineRunning)
            {
                currentVolume += (0.0 - currentVolume) * VolumeSmoothingFactor;
                audioFileReader.Volume = (float)currentVolume;
                if (currentVolume <= 0.01)
                {
                    Stop();
                }

                return;
            }

            if (outputDevice.PlaybackState != PlaybackState.Playing && !isMuted)
            {
                outputDevice.Play();
            }

            var referenceRpm = Math.Max(audioProfile.EngineLoopReferenceRpm, 1);
            var rawPitchFactor = currentRpm / referenceRpm;
            var targetPitchFactor = Math.Clamp(
                rawPitchFactor,
                audioProfile.EngineLoopMinPitchFactor,
                audioProfile.EngineLoopMaxPitchFactor);
            currentPitchFactor += (targetPitchFactor - currentPitchFactor) * PitchSmoothingFactor;
            pitchProvider.PitchFactor = (float)currentPitchFactor;

            var activeVolume = throttlePercent > 0
                ? audioProfile.EngineLoopThrottleVolume
                : audioProfile.EngineLoopBaseVolume;
            var targetVolume = isMuted ? 0.0 : activeVolume;
            currentVolume += (targetVolume - currentVolume) * VolumeSmoothingFactor;
            audioFileReader.Volume = (float)Math.Clamp(currentVolume, 0.0, 1.0);

            if (isMuted)
            {
                Stop();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Engine loop update failed. {ex.Message}");
            Stop();
        }
    }

    public void Stop()
    {
        if (outputDevice?.PlaybackState != PlaybackState.Stopped)
        {
            outputDevice?.Stop();
        }

        if (audioFileReader is not null)
        {
            audioFileReader.Volume = 0.0f;
        }

        currentVolume = 0.0;
    }

    public void Dispose()
    {
        Stop();
        DisposeAudioResources();
    }

    private string? ResolveSoundPath(string relativePath)
    {
        try
        {
            var combinedPath = Path.GetFullPath(Path.Combine(soundsDirectory, relativePath));
            var rootWithSeparator = soundsDirectory.EndsWith(Path.DirectorySeparatorChar)
                ? soundsDirectory
                : soundsDirectory + Path.DirectorySeparatorChar;

            if (!combinedPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(combinedPath, soundsDirectory, StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"Rejected unsafe engine loop path: {relativePath}");
                return null;
            }

            return combinedPath;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Debug.WriteLine($"Invalid engine loop path: {relativePath}. {ex.Message}");
            return null;
        }
    }

    private void DisposeAudioResources()
    {
        outputDevice?.Dispose();
        outputDevice = null;
        audioFileReader?.Dispose();
        audioFileReader = null;
        pitchProvider = null;
        IsAvailable = false;
    }

    private sealed class LoopingSampleProvider : ISampleProvider
    {
        private readonly AudioFileReader source;

        public LoopingSampleProvider(AudioFileReader source)
        {
            this.source = source;
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            var totalRead = 0;

            while (totalRead < count)
            {
                var read = source.Read(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                {
                    source.Position = 0;
                    continue;
                }

                totalRead += read;
            }

            return totalRead;
        }
    }
}
