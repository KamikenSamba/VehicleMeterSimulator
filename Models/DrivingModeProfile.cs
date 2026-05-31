namespace VehicleMeterSimulator.Models;

public class DrivingModeProfile
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public double AccelerationMultiplier { get; init; }

    public double CoastDecelerationMultiplier { get; init; }

    public int ShiftUpIndicatorRpm { get; init; }

    public int AutomaticUpshiftRpm { get; init; }

    public int AutomaticDownshiftRpm { get; init; }

    public int AutomaticShiftCooldownMilliseconds { get; init; }

    public required string AccentStyleId { get; init; }
}
