using System.Collections.Generic;

namespace VehicleMeterSimulator.Models;

public class EngineAudioDebugInfo
{
    public string PlaybackMode { get; init; } = "None";

    public bool IsAudioAvailable { get; init; }

    public bool IsPlaying { get; init; }

    public bool IsMuted { get; init; }

    public double MasterVolume { get; init; }

    public double CurrentRpm { get; init; }

    public double ActualVehicleRpm { get; init; }

    public double AudioControlRpm { get; init; }

    public bool IsPreviewActive { get; init; }

    public double CurrentPitchFactor { get; init; }

    public double CurrentOutputVolume { get; init; }

    public List<EngineAudioLayerDebugInfo> Layers { get; init; } = new();
}
