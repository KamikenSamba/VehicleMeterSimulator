namespace VehicleMeterSimulator.Models;

public class EngineAudioLayerProfile
{
    public string Id { get; init; } = string.Empty;

    public string? SoundPath { get; init; }

    public int ReferenceRpm { get; init; }

    public int MinimumRpm { get; init; }

    public int PeakRpm { get; init; }

    public int MaximumRpm { get; init; }

    public double MinimumPitchFactor { get; init; }

    public double MaximumPitchFactor { get; init; }

    public double BaseVolume { get; init; }
}
