using System.Collections.Generic;

namespace VehicleMeterSimulator.Models;

public class VehicleProfile
{
    public string Id { get; init; } = string.Empty;

    public string Manufacturer { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string MeterStyleId { get; init; } = string.Empty;

    public string EngineDescription { get; init; } = string.Empty;

    public string PowertrainType { get; init; } = "combustion";

    public string TransmissionType { get; init; } = string.Empty;

    public IReadOnlyList<string> SupportedShiftPositions { get; init; } = [];

    public IReadOnlyList<string> SupportedDriveModes { get; init; } = [];

    public string DefaultShiftPosition { get; init; } = string.Empty;

    public string DefaultDriveMode { get; init; } = string.Empty;

    public int MaxPowerPs { get; init; }

    public int MaxPowerRpm { get; init; }

    public int MaxTorqueNm { get; init; }

    public int MaxTorqueRpm { get; init; }

    public string Transmission { get; init; } = string.Empty;

    public string DriveType { get; init; } = string.Empty;

    public int MaxRpm { get; init; }

    public int IdleRpm { get; init; }

    public int RevLimiterRpm { get; init; }

    public int ShiftUpIndicatorRpm { get; init; }

    public int ForwardGearCount { get; init; }

    public string DefaultDrivingModeId { get; init; } = string.Empty;

    public IReadOnlyList<DrivingModeProfile> DrivingModes { get; init; } = [];

    public string DefaultTransmissionModeId { get; init; } = string.Empty;

    public IReadOnlyList<string> SupportedTransmissionModeIds { get; init; } = [];

    public double MaxSimulationSpeedKmh { get; init; }

    public IReadOnlyList<double> RpmPerKmhByGear { get; init; } = [];

    public IReadOnlyList<double> AccelerationKmhPerSecondByGear { get; init; } = [];

    public double BrakeDecelerationKmhPerSecond { get; init; }

    public double CoastDecelerationKmhPerSecond { get; init; }

    public double MaxReverseSpeedKmh { get; init; }

    public double ReverseRpmPerKmh { get; init; }

    public double ReverseAccelerationKmhPerSecond { get; init; }

    public VehicleAudioProfile? AudioProfile { get; init; }

    public NoteE13AudioProfile? NoteE13AudioProfile { get; init; }

    public string PowerDisplay => $"{MaxPowerPs} PS / {MaxPowerRpm:N0} rpm";

    public string TorqueDisplay => $"{MaxTorqueNm} N\u00B7m / {MaxTorqueRpm:N0} rpm";

    public bool UsesElectricPowerMeter => string.Equals(
        MeterStyleId,
        "note-e13-authentic",
        System.StringComparison.OrdinalIgnoreCase);
}
