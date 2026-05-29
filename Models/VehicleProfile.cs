using System.Collections.Generic;

namespace VehicleMeterSimulator.Models;

public class VehicleProfile
{
    public required string Name { get; init; }

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

    public int ForwardGearCount { get; init; }

    public double MaxSimulationSpeedKmh { get; init; }

    public required IReadOnlyList<double> RpmPerKmhByGear { get; init; }

    public required IReadOnlyList<double> AccelerationKmhPerSecondByGear { get; init; }

    public double BrakeDecelerationKmhPerSecond { get; init; }

    public double CoastDecelerationKmhPerSecond { get; init; }

    public string PowerDisplay => $"{MaxPowerPs} PS @ {MaxPowerRpm:N0} rpm";

    public string TorqueDisplay => $"{MaxTorqueNm} N\u00B7m @ {MaxTorqueRpm:N0} rpm";

    public static List<VehicleProfile> CreateDefaultVehicles()
    {
        return
        [
            new VehicleProfile
            {
                Name = "Lexus LFA",
                EngineDescription = "4.8L V10",
                MaxPowerPs = 560,
                MaxPowerRpm = 8700,
                MaxTorqueNm = 480,
                MaxTorqueRpm = 7000,
                Transmission = "6-speed ASG",
                DriveType = "FR",
                MaxRpm = 10000,
                IdleRpm = 1000,
                RevLimiterRpm = 9000,
                ForwardGearCount = 6,
                MaxSimulationSpeedKmh = 325.0,
                RpmPerKmhByGear = [0.0, 130.0, 82.0, 60.0, 45.0, 35.0, 27.0],
                AccelerationKmhPerSecondByGear = [0.0, 26.0, 20.0, 15.0, 11.0, 8.0, 6.0],
                BrakeDecelerationKmhPerSecond = 55.0,
                CoastDecelerationKmhPerSecond = 3.0
            }
        ];
    }
}
