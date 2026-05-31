using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media;
using VehicleMeterSimulator.Models;

namespace VehicleMeterSimulator.Services;

public class AudioService
{
    private readonly string soundsDirectory;
    private readonly List<MediaPlayer> activePlayers = [];
    private double volume = 0.75;

    public AudioService()
        : this(Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds"))
    {
    }

    public AudioService(string soundsDirectory)
    {
        this.soundsDirectory = Path.GetFullPath(soundsDirectory);
    }

    public bool IsMuted { get; private set; } = false;

    public double Volume
    {
        get => volume;
        set => volume = Math.Clamp(value, 0.0, 1.0);
    }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;

        if (IsMuted)
        {
            StopActivePlayers();
        }
    }

    public void PlaySound(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || IsMuted)
        {
            return;
        }

        var fullPath = ResolveSoundPath(relativePath);
        if (fullPath is null || !File.Exists(fullPath))
        {
            Debug.WriteLine($"Sound file was not found: {relativePath}");
            return;
        }

        try
        {
            var player = new MediaPlayer
            {
                Volume = Volume
            };

            player.MediaEnded += (_, _) => DisposePlayer(player);
            player.MediaFailed += (_, args) =>
            {
                Debug.WriteLine($"Sound playback failed: {fullPath}. {args.ErrorException.Message}");
                DisposePlayer(player);
            };

            activePlayers.Add(player);
            player.Open(new Uri(fullPath, UriKind.Absolute));
            player.Play();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UriFormatException)
        {
            Debug.WriteLine($"Sound playback error: {relativePath}. {ex.Message}");
        }
    }

    public bool HasAvailableSound(VehicleAudioProfile? audioProfile)
    {
        if (audioProfile is null)
        {
            return false;
        }

        return EnumerateSoundPaths(audioProfile)
            .Any(relativePath =>
            {
                var fullPath = ResolveSoundPath(relativePath);
                return fullPath is not null && File.Exists(fullPath);
            });
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
                Debug.WriteLine($"Rejected unsafe sound path: {relativePath}");
                return null;
            }

            return combinedPath;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Debug.WriteLine($"Invalid sound path: {relativePath}. {ex.Message}");
            return null;
        }
    }

    private void StopActivePlayers()
    {
        foreach (var player in activePlayers.ToList())
        {
            DisposePlayer(player);
        }
    }

    private void DisposePlayer(MediaPlayer player)
    {
        player.Stop();
        player.Close();
        activePlayers.Remove(player);
    }

    private static IEnumerable<string> EnumerateSoundPaths(VehicleAudioProfile audioProfile)
    {
        if (!string.IsNullOrWhiteSpace(audioProfile.IgnitionOnSound))
        {
            yield return audioProfile.IgnitionOnSound;
        }

        if (!string.IsNullOrWhiteSpace(audioProfile.IgnitionOffSound))
        {
            yield return audioProfile.IgnitionOffSound;
        }

        if (!string.IsNullOrWhiteSpace(audioProfile.EngineStartSound))
        {
            yield return audioProfile.EngineStartSound;
        }

        if (!string.IsNullOrWhiteSpace(audioProfile.EngineStopSound))
        {
            yield return audioProfile.EngineStopSound;
        }

        if (!string.IsNullOrWhiteSpace(audioProfile.ShiftUpSound))
        {
            yield return audioProfile.ShiftUpSound;
        }

        if (!string.IsNullOrWhiteSpace(audioProfile.ShiftDownSound))
        {
            yield return audioProfile.ShiftDownSound;
        }

        if (!string.IsNullOrWhiteSpace(audioProfile.ReverseEngageSound))
        {
            yield return audioProfile.ReverseEngageSound;
        }

        if (!string.IsNullOrWhiteSpace(audioProfile.ReverseDisengageSound))
        {
            yield return audioProfile.ReverseDisengageSound;
        }

        if (!string.IsNullOrWhiteSpace(audioProfile.ParkingBrakeAppliedSound))
        {
            yield return audioProfile.ParkingBrakeAppliedSound;
        }

        if (!string.IsNullOrWhiteSpace(audioProfile.ParkingBrakeReleasedSound))
        {
            yield return audioProfile.ParkingBrakeReleasedSound;
        }
    }
}
