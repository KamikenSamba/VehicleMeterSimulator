namespace VehicleMeterSimulator.Models;

public class NoteE13AudioProfile
{
    public string? TurnSignalSound { get; init; }

    public string? ReverseLoopSound { get; init; }

    public string? ForwardApproachLoopSound { get; init; }

    public int TurnSignalPeriodMs { get; init; } = 700;

    public int TurnSignalLampOnMs { get; init; } = 350;

    public int TurnSignalLampPeriodMs { get; init; } = 0;

    public int TurnSignalClickPlaybackMs { get; init; } = 180;

    public double TurnSignalVolume { get; init; } = 0.85;

    public double ReverseLoopVolume { get; init; } = 0.75;

    public int ReverseFadeMilliseconds { get; init; } = 80;

    public double ForwardApproachBaseVolume { get; init; } = 0.65;

    public double ForwardApproachStartSpeedKmh { get; init; } = 0.5;

    public double ForwardApproachFullVolumeSpeedKmh { get; init; } = 5.0;

    public double ForwardApproachFadeOutStartSpeedKmh { get; init; } = 18.0;

    public double ForwardApproachStopSpeedKmh { get; init; } = 25.0;

    public int ForwardApproachFadeMilliseconds { get; init; } = 180;
}
