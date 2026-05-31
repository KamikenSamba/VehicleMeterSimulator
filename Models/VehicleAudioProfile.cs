namespace VehicleMeterSimulator.Models;

public class VehicleAudioProfile
{
    public string? IgnitionOnSound { get; init; }

    public string? IgnitionOffSound { get; init; }

    public string? EngineStartSound { get; init; }

    public string? EngineStopSound { get; init; }

    public string? ShiftUpSound { get; init; }

    public string? ShiftDownSound { get; init; }

    public string? ReverseEngageSound { get; init; }

    public string? ReverseDisengageSound { get; init; }

    public string? ParkingBrakeAppliedSound { get; init; }

    public string? ParkingBrakeReleasedSound { get; init; }

    public string? EngineLoopSound { get; init; }

    public int EngineLoopReferenceRpm { get; init; }

    public double EngineLoopMinPitchFactor { get; init; }

    public double EngineLoopMaxPitchFactor { get; init; }

    public double EngineLoopBaseVolume { get; init; }

    public double EngineLoopThrottleVolume { get; init; }
}
