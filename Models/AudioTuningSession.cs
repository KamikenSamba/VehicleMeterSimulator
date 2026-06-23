using System;
using System.Collections.Generic;
using System.Linq;

namespace VehicleMeterSimulator.Models;

public class AudioTuningSession
{
    public string VehicleId { get; set; } = string.Empty;

    public string VehicleName { get; set; } = string.Empty;

    public bool IsTuningPanelVisible { get; set; }

    public bool IsPreviewEnabled { get; set; }

    public int PreviewRpm { get; set; }

    public double EngineLayersMasterVolume { get; set; }

    public double EngineLoopBaseVolume { get; set; }

    public double EngineLoopThrottleVolume { get; set; }

    public List<EngineAudioLayerProfile> EngineAudioLayers { get; set; } = new();

    public static AudioTuningSession FromVehicle(VehicleProfile vehicle)
    {
        var audioProfile = vehicle.AudioProfile;
        return new AudioTuningSession
        {
            VehicleId = vehicle.Id,
            VehicleName = vehicle.Name,
            PreviewRpm = vehicle.IdleRpm,
            EngineLayersMasterVolume = audioProfile?.EngineLayersMasterVolume ?? 0.8,
            EngineLoopBaseVolume = audioProfile?.EngineLoopBaseVolume ?? 0.35,
            EngineLoopThrottleVolume = audioProfile?.EngineLoopThrottleVolume ?? 0.75,
            EngineAudioLayers = audioProfile?.EngineAudioLayers
                .Select(CopyLayer)
                .ToList() ?? new List<EngineAudioLayerProfile>()
        };
    }

    public void ResetFromVehicle(VehicleProfile vehicle)
    {
        var freshSession = FromVehicle(vehicle);
        IsPreviewEnabled = false;
        PreviewRpm = freshSession.PreviewRpm;
        EngineLayersMasterVolume = freshSession.EngineLayersMasterVolume;
        EngineLoopBaseVolume = freshSession.EngineLoopBaseVolume;
        EngineLoopThrottleVolume = freshSession.EngineLoopThrottleVolume;
        EngineAudioLayers = freshSession.EngineAudioLayers;
    }

    public VehicleAudioProfile BuildAudioProfile(VehicleAudioProfile? source)
    {
        return new VehicleAudioProfile
        {
            IgnitionOnSound = source?.IgnitionOnSound,
            IgnitionOffSound = source?.IgnitionOffSound,
            SystemStopSound = source?.SystemStopSound,
            SeatbeltWarningSound = source?.SeatbeltWarningSound,
            TurnSignalSound = source?.TurnSignalSound,
            EngineStartSound = source?.EngineStartSound,
            EngineStopSound = source?.EngineStopSound,
            ShiftUpSound = source?.ShiftUpSound,
            ShiftDownSound = source?.ShiftDownSound,
            ReverseEngageSound = source?.ReverseEngageSound,
            ReverseDisengageSound = source?.ReverseDisengageSound,
            ReverseWarningSound = source?.ReverseWarningSound,
            ParkingBrakeAppliedSound = source?.ParkingBrakeAppliedSound,
            ParkingBrakeReleasedSound = source?.ParkingBrakeReleasedSound,
            EngineLoopSound = source?.EngineLoopSound,
            EngineLoopReferenceRpm = source?.EngineLoopReferenceRpm ?? 1,
            EngineLoopMinPitchFactor = source?.EngineLoopMinPitchFactor ?? 0.75,
            EngineLoopMaxPitchFactor = source?.EngineLoopMaxPitchFactor ?? 1.8,
            EngineLoopBaseVolume = Math.Clamp(EngineLoopBaseVolume, 0.0, 1.0),
            EngineLoopThrottleVolume = Math.Clamp(EngineLoopThrottleVolume, 0.0, 1.0),
            EngineLayersMasterVolume = Math.Clamp(EngineLayersMasterVolume, 0.0, 1.0),
            EngineAudioLayers = EngineAudioLayers.Select(CopyLayer).ToList()
        };
    }

    private static EngineAudioLayerProfile CopyLayer(EngineAudioLayerProfile layer)
    {
        return new EngineAudioLayerProfile
        {
            Id = layer.Id,
            SoundPath = layer.SoundPath,
            ReferenceRpm = layer.ReferenceRpm,
            MinimumRpm = layer.MinimumRpm,
            PeakRpm = layer.PeakRpm,
            MaximumRpm = layer.MaximumRpm,
            MinimumPitchFactor = layer.MinimumPitchFactor,
            MaximumPitchFactor = layer.MaximumPitchFactor,
            BaseVolume = layer.BaseVolume
        };
    }
}
