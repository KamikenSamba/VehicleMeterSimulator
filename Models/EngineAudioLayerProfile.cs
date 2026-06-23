namespace VehicleMeterSimulator.Models;

public class EngineAudioLayerProfile
{
    public string Id { get; set; } = string.Empty;

    public string? SoundPath { get; set; }

    public int ReferenceRpm { get; set; }

    public int MinimumRpm { get; set; }

    public int PeakRpm { get; set; }

    public int MaximumRpm { get; set; }

    public double MinimumPitchFactor { get; set; }

    public double MaximumPitchFactor { get; set; }

    public double BaseVolume { get; set; }
}
