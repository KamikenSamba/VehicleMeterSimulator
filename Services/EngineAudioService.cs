using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using VehicleMeterSimulator.Models;

namespace VehicleMeterSimulator.Services;

public enum EngineAudioPlaybackMode
{
    None,
    SingleLoopFallback,
    MultiLayerCrossfade
}

public sealed class EngineAudioService : IDisposable
{
    private const double PitchSmoothingFactor = 0.15;
    private const double VolumeSmoothingFactor = 0.20;
    private const double SilentVolumeThreshold = 0.01;

    private readonly string soundsDirectory;
    private readonly List<EngineAudioLayerRuntime> activeLayers = new();
    private VehicleAudioProfile? audioProfile;
    private AudioFileReader? singleLoopReader;
    private SmbPitchShiftingSampleProvider? singleLoopPitchProvider;
    private WaveOutEvent? outputDevice;
    private double singleLoopPitchFactor = 1.0;
    private double singleLoopVolume = 0.0;
    private bool lastIsMuted;
    private double lastMasterVolume = 0.75;
    private double lastCurrentRpm;
    private double lastActualVehicleRpm;
    private double lastAudioControlRpm;
    private bool lastIsPreviewActive;

    public EngineAudioService()
        : this(Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds"))
    {
    }

    public EngineAudioService(string soundsDirectory)
    {
        this.soundsDirectory = Path.GetFullPath(soundsDirectory);
    }

    public bool IsAvailable => PlaybackMode != EngineAudioPlaybackMode.None;

    public bool IsPlaying =>
        outputDevice?.PlaybackState == PlaybackState.Playing
        && (singleLoopVolume > SilentVolumeThreshold || activeLayers.Any(layer => layer.CurrentVolume > SilentVolumeThreshold));

    public EngineAudioPlaybackMode PlaybackMode { get; private set; } = EngineAudioPlaybackMode.None;

    public int ActiveLayerCount => activeLayers.Count;

    public string ActiveLayerSummary { get; private set; } = string.Empty;

    public void Initialize(VehicleAudioProfile? audioProfile)
    {
        Stop();
        DisposeAudioResources();

        this.audioProfile = audioProfile;
        PlaybackMode = EngineAudioPlaybackMode.None;
        singleLoopPitchFactor = 1.0;
        singleLoopVolume = 0.0;
        ActiveLayerSummary = string.Empty;

        if (audioProfile is null)
        {
            return;
        }

        var loadedLayers = LoadConfiguredLayers(audioProfile);
        if (loadedLayers.Count >= 2 && TryInitializeLayerMixer(loadedLayers))
        {
            PlaybackMode = EngineAudioPlaybackMode.MultiLayerCrossfade;
            return;
        }

        foreach (var layer in loadedLayers)
        {
            layer.Dispose();
        }

        TryInitializeSingleLoop(audioProfile);
    }

    public void UpdateEngineSound(
        bool isEngineRunning,
        double currentRpm,
        int throttlePercent,
        bool isMuted,
        double masterVolume)
    {
        UpdateEngineSound(
            isEngineRunning,
            currentRpm,
            currentRpm,
            throttlePercent,
            isMuted,
            masterVolume,
            false);
    }

    public void UpdateEngineSound(
        bool isEngineRunning,
        double actualRpm,
        double audioControlRpm,
        int throttlePercent,
        bool isMuted,
        double masterVolume,
        bool isPreviewActive = false)
    {
        lastIsMuted = isMuted;
        lastMasterVolume = Math.Clamp(masterVolume, 0.0, 1.0);
        lastCurrentRpm = audioControlRpm;
        lastActualVehicleRpm = actualRpm;
        lastAudioControlRpm = audioControlRpm;
        lastIsPreviewActive = isPreviewActive;

        if (!IsAvailable || audioProfile is null || outputDevice is null)
        {
            return;
        }

        try
        {
            if (!isEngineRunning || isMuted)
            {
                FadeToSilence();
                if (IsFullySilent())
                {
                    Stop();
                }

                return;
            }

            if (outputDevice.PlaybackState != PlaybackState.Playing)
            {
                outputDevice.Play();
            }

            if (PlaybackMode == EngineAudioPlaybackMode.MultiLayerCrossfade)
            {
                UpdateLayeredSound(audioControlRpm, throttlePercent, lastMasterVolume);
                return;
            }

            UpdateSingleLoopSound(audioControlRpm, throttlePercent, lastMasterVolume);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Engine audio update failed. {ex.Message}");
            Stop();
        }
    }

    public void Stop()
    {
        if (outputDevice?.PlaybackState != PlaybackState.Stopped)
        {
            outputDevice?.Stop();
        }

        if (singleLoopReader is not null)
        {
            singleLoopReader.Volume = 0.0f;
        }

        singleLoopVolume = 0.0;
        foreach (var layer in activeLayers)
        {
            layer.CurrentVolume = 0.0;
            layer.VolumeProvider.Volume = 0.0f;
        }

        ActiveLayerSummary = string.Empty;
    }

    public EngineAudioDebugInfo GetDebugInfo()
    {
        return new EngineAudioDebugInfo
        {
            PlaybackMode = PlaybackMode.ToString(),
            IsAudioAvailable = IsAvailable,
            IsPlaying = IsPlaying,
            IsMuted = lastIsMuted,
            MasterVolume = lastMasterVolume,
            CurrentRpm = lastCurrentRpm,
            ActualVehicleRpm = lastActualVehicleRpm,
            AudioControlRpm = lastAudioControlRpm,
            IsPreviewActive = lastIsPreviewActive,
            CurrentPitchFactor = singleLoopPitchFactor,
            CurrentOutputVolume = PlaybackMode == EngineAudioPlaybackMode.MultiLayerCrossfade
                ? Math.Clamp(activeLayers.Sum(layer => layer.CurrentVolume), 0.0, 1.0)
                : singleLoopVolume,
            Layers = activeLayers
                .Select(layer => new EngineAudioLayerDebugInfo
                {
                    Id = layer.Profile.Id,
                    IsAvailable = true,
                    CurrentGain = layer.CurrentVolume,
                    CurrentPitchFactor = layer.CurrentPitchFactor
                })
                .ToList()
        };
    }

    public void ApplyTuningSettings(AudioTuningSession tuningSession)
    {
        if (audioProfile is null)
        {
            return;
        }

        audioProfile = tuningSession.BuildAudioProfile(audioProfile);

        foreach (var activeLayer in activeLayers)
        {
            var tunedLayer = audioProfile.EngineAudioLayers
                .FirstOrDefault(layer => string.Equals(layer.Id, activeLayer.Profile.Id, StringComparison.OrdinalIgnoreCase));
            if (tunedLayer is not null)
            {
                activeLayer.Profile = tunedLayer;
            }
        }
    }

    public void Dispose()
    {
        Stop();
        DisposeAudioResources();
    }

    private List<EngineAudioLayerRuntime> LoadConfiguredLayers(VehicleAudioProfile audioProfile)
    {
        var loadedLayers = new List<EngineAudioLayerRuntime>();
        if (audioProfile.EngineAudioLayers is null || audioProfile.EngineAudioLayers.Count == 0)
        {
            return loadedLayers;
        }

        WaveFormat? targetFormat = null;
        foreach (var layerProfile in audioProfile.EngineAudioLayers)
        {
            if (string.IsNullOrWhiteSpace(layerProfile.SoundPath))
            {
                continue;
            }

            var fullPath = ResolveSoundPath(layerProfile.SoundPath);
            if (fullPath is null || !File.Exists(fullPath))
            {
                Debug.WriteLine($"Engine audio layer file was not found: {layerProfile.SoundPath}");
                continue;
            }

            try
            {
                var reader = new AudioFileReader(fullPath);
                if (targetFormat is null)
                {
                    targetFormat = reader.WaveFormat;
                }
                else if (!WaveFormatsMatch(targetFormat, reader.WaveFormat))
                {
                    Debug.WriteLine($"Engine audio layer format does not match the mixer format: {layerProfile.SoundPath}");
                    reader.Dispose();
                    continue;
                }

                var loopProvider = new LoopingSampleProvider(reader);
                var pitchProvider = new SmbPitchShiftingSampleProvider(loopProvider)
                {
                    PitchFactor = 1.0f
                };
                var volumeProvider = new VolumeSampleProvider(pitchProvider)
                {
                    Volume = 0.0f
                };

                loadedLayers.Add(new EngineAudioLayerRuntime(layerProfile, reader, pitchProvider, volumeProvider));
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
            {
                Debug.WriteLine($"Engine audio layer initialization failed: {layerProfile.SoundPath}. {ex.Message}");
            }
        }

        return loadedLayers;
    }

    private bool TryInitializeLayerMixer(List<EngineAudioLayerRuntime> loadedLayers)
    {
        try
        {
            var mixer = new MixingSampleProvider(loadedLayers.Select(layer => layer.VolumeProvider))
            {
                ReadFully = true
            };

            outputDevice = new WaveOutEvent();
            outputDevice.Init(mixer);
            activeLayers.AddRange(loadedLayers);
            Debug.WriteLine($"Engine audio initialized in multi-layer mode with {activeLayers.Count} layers.");
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Debug.WriteLine($"Engine audio multi-layer mixer initialization failed. {ex.Message}");
            DisposeAudioResources();
            return false;
        }
    }

    private void TryInitializeSingleLoop(VehicleAudioProfile audioProfile)
    {
        if (string.IsNullOrWhiteSpace(audioProfile.EngineLoopSound))
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
            singleLoopReader = new AudioFileReader(fullPath)
            {
                Volume = 0.0f
            };
            var loopProvider = new LoopingSampleProvider(singleLoopReader);
            singleLoopPitchProvider = new SmbPitchShiftingSampleProvider(loopProvider)
            {
                PitchFactor = 1.0f
            };
            outputDevice = new WaveOutEvent();
            outputDevice.Init(singleLoopPitchProvider);
            PlaybackMode = EngineAudioPlaybackMode.SingleLoopFallback;
            Debug.WriteLine("Engine audio initialized in single-loop fallback mode.");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
        {
            Debug.WriteLine($"Engine loop initialization failed: {fullPath}. {ex.Message}");
            DisposeAudioResources();
        }
    }

    private void UpdateSingleLoopSound(double currentRpm, int throttlePercent, double masterVolume)
    {
        if (audioProfile is null || singleLoopReader is null || singleLoopPitchProvider is null)
        {
            return;
        }

        var referenceRpm = Math.Max(audioProfile.EngineLoopReferenceRpm, 1);
        var rawPitchFactor = currentRpm / referenceRpm;
        var targetPitchFactor = Math.Clamp(
            rawPitchFactor,
            audioProfile.EngineLoopMinPitchFactor,
            audioProfile.EngineLoopMaxPitchFactor);
        singleLoopPitchFactor += (targetPitchFactor - singleLoopPitchFactor) * PitchSmoothingFactor;
        singleLoopPitchProvider.PitchFactor = (float)singleLoopPitchFactor;

        var targetVolume = (throttlePercent > 0
            ? audioProfile.EngineLoopThrottleVolume
            : audioProfile.EngineLoopBaseVolume) * masterVolume;
        singleLoopVolume += (targetVolume - singleLoopVolume) * VolumeSmoothingFactor;
        singleLoopReader.Volume = (float)Math.Clamp(singleLoopVolume, 0.0, 1.0);
        ActiveLayerSummary = string.Empty;
    }

    private void UpdateLayeredSound(double currentRpm, int throttlePercent, double masterVolume)
    {
        if (audioProfile is null || activeLayers.Count == 0)
        {
            return;
        }

        var rawGains = activeLayers
            .Select(layer => CalculateLayerGain(layer.Profile, currentRpm))
            .ToArray();
        var rawGainSum = rawGains.Sum();
        var throttleVolumeFactor = throttlePercent > 0
            ? audioProfile.EngineLoopThrottleVolume
            : audioProfile.EngineLoopBaseVolume;
        var normalizeDivisor = rawGainSum > 1.0 ? rawGainSum : 1.0;

        for (var i = 0; i < activeLayers.Count; i++)
        {
            var layer = activeLayers[i];
            var normalizedGain = rawGains[i] / normalizeDivisor;
            var targetVolume = normalizedGain
                * layer.Profile.BaseVolume
                * audioProfile.EngineLayersMasterVolume
                * throttleVolumeFactor
                * masterVolume;
            layer.CurrentVolume += (targetVolume - layer.CurrentVolume) * VolumeSmoothingFactor;
            layer.VolumeProvider.Volume = (float)Math.Clamp(layer.CurrentVolume, 0.0, 1.0);

            var referenceRpm = Math.Max(layer.Profile.ReferenceRpm, 1);
            var rawPitchFactor = currentRpm / referenceRpm;
            var targetPitchFactor = Math.Clamp(
                rawPitchFactor,
                layer.Profile.MinimumPitchFactor,
                layer.Profile.MaximumPitchFactor);
            layer.CurrentPitchFactor += (targetPitchFactor - layer.CurrentPitchFactor) * PitchSmoothingFactor;
            layer.PitchProvider.PitchFactor = (float)layer.CurrentPitchFactor;
        }

        ActiveLayerSummary = BuildActiveLayerSummary();
    }

    private void FadeToSilence()
    {
        if (singleLoopReader is not null)
        {
            singleLoopVolume += (0.0 - singleLoopVolume) * VolumeSmoothingFactor;
            singleLoopReader.Volume = (float)Math.Clamp(singleLoopVolume, 0.0, 1.0);
        }

        foreach (var layer in activeLayers)
        {
            layer.CurrentVolume += (0.0 - layer.CurrentVolume) * VolumeSmoothingFactor;
            layer.VolumeProvider.Volume = (float)Math.Clamp(layer.CurrentVolume, 0.0, 1.0);
        }
    }

    private bool IsFullySilent()
    {
        return singleLoopVolume <= SilentVolumeThreshold
            && activeLayers.All(layer => layer.CurrentVolume <= SilentVolumeThreshold);
    }

    private string BuildActiveLayerSummary()
    {
        var active = activeLayers
            .Where(layer => layer.CurrentVolume > 0.02)
            .OrderByDescending(layer => layer.CurrentVolume)
            .Take(2)
            .Select(layer => $"{layer.Profile.Id} {Math.Round(layer.CurrentVolume * 100)}%")
            .ToList();

        return active.Count == 0 ? string.Empty : string.Join(" / ", active);
    }

    private static double CalculateLayerGain(EngineAudioLayerProfile layer, double currentRpm)
    {
        if (currentRpm < layer.MinimumRpm || currentRpm > layer.MaximumRpm)
        {
            return 0.0;
        }

        if (Math.Abs(currentRpm - layer.PeakRpm) < 0.001)
        {
            return 1.0;
        }

        if (currentRpm < layer.PeakRpm)
        {
            var divisor = layer.PeakRpm - layer.MinimumRpm;
            return divisor <= 0
                ? 1.0
                : Math.Clamp((currentRpm - layer.MinimumRpm) / divisor, 0.0, 1.0);
        }

        var fallingDivisor = layer.MaximumRpm - layer.PeakRpm;
        return fallingDivisor <= 0
            ? 0.0
            : Math.Clamp((layer.MaximumRpm - currentRpm) / fallingDivisor, 0.0, 1.0);
    }

    private static bool WaveFormatsMatch(WaveFormat first, WaveFormat second)
    {
        return first.SampleRate == second.SampleRate
            && first.Channels == second.Channels
            && first.Encoding == second.Encoding;
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
                Debug.WriteLine($"Rejected unsafe engine audio path: {relativePath}");
                return null;
            }

            return combinedPath;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Debug.WriteLine($"Invalid engine audio path: {relativePath}. {ex.Message}");
            return null;
        }
    }

    private void DisposeAudioResources()
    {
        outputDevice?.Dispose();
        outputDevice = null;
        singleLoopReader?.Dispose();
        singleLoopReader = null;
        singleLoopPitchProvider = null;

        foreach (var layer in activeLayers)
        {
            layer.Dispose();
        }

        activeLayers.Clear();
        PlaybackMode = EngineAudioPlaybackMode.None;
        ActiveLayerSummary = string.Empty;
        lastCurrentRpm = 0.0;
        lastActualVehicleRpm = 0.0;
        lastAudioControlRpm = 0.0;
        lastIsPreviewActive = false;
        singleLoopPitchFactor = 1.0;
    }

    private sealed class EngineAudioLayerRuntime : IDisposable
    {
        public EngineAudioLayerRuntime(
            EngineAudioLayerProfile profile,
            AudioFileReader reader,
            SmbPitchShiftingSampleProvider pitchProvider,
            VolumeSampleProvider volumeProvider)
        {
            Profile = profile;
            Reader = reader;
            PitchProvider = pitchProvider;
            VolumeProvider = volumeProvider;
        }

        public EngineAudioLayerProfile Profile { get; set; }

        public AudioFileReader Reader { get; }

        public SmbPitchShiftingSampleProvider PitchProvider { get; }

        public VolumeSampleProvider VolumeProvider { get; }

        public double CurrentPitchFactor { get; set; } = 1.0;

        public double CurrentVolume { get; set; }

        public void Dispose()
        {
            Reader.Dispose();
        }
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
                    if (source.Length == 0)
                    {
                        break;
                    }

                    source.Position = 0;
                    continue;
                }

                totalRead += read;
            }

            return totalRead;
        }
    }
}
