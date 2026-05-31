using System.Collections.Generic;

namespace VehicleMeterSimulator.Models;

public class VehicleProfile
{
    public required string Id { get; init; }

    public required string Manufacturer { get; init; }

    public required string Name { get; init; }

    public required string MeterStyleId { get; init; }

    public required string EngineDescription { get; init; }

    public int MaxPowerPs { get; init; }

    public int MaxPowerRpm { get; init; }

    public int MaxTorqueNm { get; init; }

    public int MaxTorqueRpm { get; init; }

    public required string Transmission { get; init; }

    public required string DriveType { get; init; }

    public int MaxRpm { get; init; }

    public int IdleRpm { get; init; }

    public int RevLimiterRpm { get; init; }

    public int ShiftUpIndicatorRpm { get; init; }

    public int ForwardGearCount { get; init; }

    public required string DefaultDrivingModeId { get; init; }

    public required IReadOnlyList<DrivingModeProfile> DrivingModes { get; init; }

    public required string DefaultTransmissionModeId { get; init; }

    public required IReadOnlyList<string> SupportedTransmissionModeIds { get; init; }

    public double MaxSimulationSpeedKmh { get; init; }

    public required IReadOnlyList<double> RpmPerKmhByGear { get; init; }

    public required IReadOnlyList<double> AccelerationKmhPerSecondByGear { get; init; }

    public double BrakeDecelerationKmhPerSecond { get; init; }

    public double CoastDecelerationKmhPerSecond { get; init; }

    public double MaxReverseSpeedKmh { get; init; }

    public double ReverseRpmPerKmh { get; init; }

    public double ReverseAccelerationKmhPerSecond { get; init; }

    public VehicleAudioProfile? AudioProfile { get; init; }

    public string PowerDisplay => $"{MaxPowerPs} PS / {MaxPowerRpm:N0} rpm";

    public string TorqueDisplay => $"{MaxTorqueNm} N\u00B7m / {MaxTorqueRpm:N0} rpm";
}
