namespace VehicleMeterSimulator.Models;

public class EngineAudioLayerDebugInfo
{
    public string Id { get; init; } = string.Empty;

    public bool IsAvailable { get; init; }

    public double CurrentGain { get; init; }

    public double CurrentPitchFactor { get; init; }
}
