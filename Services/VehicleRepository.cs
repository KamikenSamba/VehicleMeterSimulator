using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VehicleMeterSimulator.Models;

namespace VehicleMeterSimulator.Services;

public class VehicleRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string vehiclesDirectory;

    public VehicleRepository()
        : this(Path.Combine(AppContext.BaseDirectory, "Data", "Vehicles"))
    {
    }

    public VehicleRepository(string vehiclesDirectory)
    {
        this.vehiclesDirectory = vehiclesDirectory;
    }

    public List<VehicleProfile> LoadVehicles()
    {
        if (!Directory.Exists(vehiclesDirectory))
        {
            throw new InvalidOperationException(
                $"Vehicle configuration folder was not found: {vehiclesDirectory}");
        }

        var jsonFiles = Directory
            .GetFiles(vehiclesDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (jsonFiles.Count == 0)
        {
            throw new InvalidOperationException(
                $"No vehicle configuration files were found in: {vehiclesDirectory}");
        }

        var vehicles = new List<VehicleProfile>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in jsonFiles)
        {
            var vehicle = LoadVehicle(filePath);
            ValidateVehicle(vehicle, filePath);

            if (!ids.Add(vehicle.Id))
            {
                throw CreateConfigurationException(filePath, $"Duplicate vehicle Id was found: {vehicle.Id}");
            }

            vehicles.Add(vehicle);
        }

        return vehicles
            .OrderBy(vehicle => vehicle.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static VehicleProfile LoadVehicle(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<VehicleProfile>(json, JsonOptions)
                ?? throw CreateConfigurationException(filePath, "The JSON file did not contain a vehicle profile.");
        }
        catch (JsonException ex)
        {
            throw CreateConfigurationException(filePath, $"Invalid JSON format. {ex.Message}");
        }
        catch (IOException ex)
        {
            throw CreateConfigurationException(filePath, $"Could not read the file. {ex.Message}");
        }
    }

    private static void ValidateVehicle(VehicleProfile vehicle, string filePath)
    {
        if (string.IsNullOrWhiteSpace(vehicle.Id))
        {
            throw CreateConfigurationException(filePath, "Id is required.");
        }

        if (string.IsNullOrWhiteSpace(vehicle.Name))
        {
            throw CreateConfigurationException(filePath, "Name is required.");
        }

        if (vehicle.MaxRpm <= 0)
        {
            throw CreateConfigurationException(filePath, "MaxRpm must be greater than 0.");
        }

        if (vehicle.IdleRpm < 0)
        {
            throw CreateConfigurationException(filePath, "IdleRpm must be greater than or equal to 0.");
        }

        if (vehicle.RevLimiterRpm <= vehicle.IdleRpm)
        {
            throw CreateConfigurationException(filePath, "RevLimiterRpm must be greater than IdleRpm.");
        }

        if (vehicle.MaxRpm < vehicle.RevLimiterRpm)
        {
            throw CreateConfigurationException(filePath, "RevLimiterRpm must be less than or equal to MaxRpm.");
        }

        if (vehicle.ShiftUpIndicatorRpm <= vehicle.IdleRpm)
        {
            throw CreateConfigurationException(filePath, "ShiftUpIndicatorRpm must be greater than IdleRpm.");
        }

        if (vehicle.ShiftUpIndicatorRpm > vehicle.RevLimiterRpm)
        {
            throw CreateConfigurationException(filePath, "ShiftUpIndicatorRpm must be less than or equal to RevLimiterRpm.");
        }

        if (vehicle.ForwardGearCount < 1)
        {
            throw CreateConfigurationException(filePath, "ForwardGearCount must be greater than or equal to 1.");
        }

        if (string.IsNullOrWhiteSpace(vehicle.MeterStyleId))
        {
            throw CreateConfigurationException(filePath, "MeterStyleId is required.");
        }

        ValidateDrivingModes(vehicle, filePath);
        ValidateTransmissionModes(vehicle, filePath);

        if (vehicle.RpmPerKmhByGear is null || vehicle.RpmPerKmhByGear.Count < vehicle.ForwardGearCount + 1)
        {
            throw CreateConfigurationException(
                filePath,
                "RpmPerKmhByGear must contain at least ForwardGearCount + 1 values.");
        }

        if (vehicle.AccelerationKmhPerSecondByGear is null
            || vehicle.AccelerationKmhPerSecondByGear.Count < vehicle.ForwardGearCount + 1)
        {
            throw CreateConfigurationException(
                filePath,
                "AccelerationKmhPerSecondByGear must contain at least ForwardGearCount + 1 values.");
        }

        if (vehicle.MaxSimulationSpeedKmh <= 0)
        {
            throw CreateConfigurationException(filePath, "MaxSimulationSpeedKmh must be greater than 0.");
        }

        if (vehicle.BrakeDecelerationKmhPerSecond <= 0)
        {
            throw CreateConfigurationException(filePath, "BrakeDecelerationKmhPerSecond must be greater than 0.");
        }

        if (vehicle.CoastDecelerationKmhPerSecond < 0)
        {
            throw CreateConfigurationException(filePath, "CoastDecelerationKmhPerSecond must be greater than or equal to 0.");
        }

        if (vehicle.MaxReverseSpeedKmh <= 0)
        {
            throw CreateConfigurationException(filePath, "MaxReverseSpeedKmh must be greater than 0.");
        }

        if (vehicle.ReverseRpmPerKmh <= 0)
        {
            throw CreateConfigurationException(filePath, "ReverseRpmPerKmh must be greater than 0.");
        }

        if (vehicle.ReverseAccelerationKmhPerSecond <= 0)
        {
            throw CreateConfigurationException(filePath, "ReverseAccelerationKmhPerSecond must be greater than 0.");
        }

        ValidateEngineLoopAudio(vehicle.AudioProfile, vehicle, filePath);
    }

    private static void ValidateEngineLoopAudio(VehicleAudioProfile? audioProfile, VehicleProfile vehicle, string filePath)
    {
        if (audioProfile is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(audioProfile.EngineLoopSound))
        {
            if (audioProfile.EngineLoopReferenceRpm <= 0)
            {
                throw CreateConfigurationException(filePath, "EngineLoopReferenceRpm must be greater than 0.");
            }

            if (audioProfile.EngineLoopMinPitchFactor <= 0)
            {
                throw CreateConfigurationException(filePath, "EngineLoopMinPitchFactor must be greater than 0.");
            }

            if (audioProfile.EngineLoopMaxPitchFactor < audioProfile.EngineLoopMinPitchFactor)
            {
                throw CreateConfigurationException(
                    filePath,
                    "EngineLoopMaxPitchFactor must be greater than or equal to EngineLoopMinPitchFactor.");
            }

            if (audioProfile.EngineLoopBaseVolume is < 0.0 or > 1.0)
            {
                throw CreateConfigurationException(filePath, "EngineLoopBaseVolume must be between 0.0 and 1.0.");
            }

            if (audioProfile.EngineLoopThrottleVolume is < 0.0 or > 1.0)
            {
                throw CreateConfigurationException(filePath, "EngineLoopThrottleVolume must be between 0.0 and 1.0.");
            }

            if (audioProfile.EngineLoopThrottleVolume < audioProfile.EngineLoopBaseVolume)
            {
                throw CreateConfigurationException(
                    filePath,
                    "EngineLoopThrottleVolume must be greater than or equal to EngineLoopBaseVolume.");
            }
        }

        ValidateEngineAudioLayers(audioProfile, vehicle, filePath);
    }

    private static void ValidateEngineAudioLayers(VehicleAudioProfile audioProfile, VehicleProfile vehicle, string filePath)
    {
        if (audioProfile.EngineLayersMasterVolume is < 0.0 or > 1.0)
        {
            throw CreateConfigurationException(
                filePath,
                "EngineLayersMasterVolume must be between 0.0 and 1.0.");
        }

        if (audioProfile.EngineAudioLayers is null || audioProfile.EngineAudioLayers.Count == 0)
        {
            return;
        }

        var layerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var layer in audioProfile.EngineAudioLayers)
        {
            if (string.IsNullOrWhiteSpace(layer.Id))
            {
                throw CreateConfigurationException(filePath, "Engine audio layer Id is required.");
            }

            if (!layerIds.Add(layer.Id))
            {
                throw CreateConfigurationException(filePath, $"Duplicate engine audio layer Id was found: {layer.Id}");
            }

            if (string.IsNullOrWhiteSpace(layer.SoundPath))
            {
                throw CreateConfigurationException(
                    filePath,
                    $"Engine audio layer \"{layer.Id}\" requires SoundPath.");
            }

            if (layer.ReferenceRpm <= 0)
            {
                throw CreateConfigurationException(
                    filePath,
                    $"Engine audio layer \"{layer.Id}\" must have ReferenceRpm greater than 0.");
            }

            if (layer.MinimumRpm < 0)
            {
                throw CreateConfigurationException(
                    filePath,
                    $"Engine audio layer \"{layer.Id}\" must have MinimumRpm greater than or equal to 0.");
            }

            var isIdleLayer = string.Equals(layer.Id, "idle", StringComparison.OrdinalIgnoreCase);
            if (isIdleLayer)
            {
                if (layer.PeakRpm < layer.MinimumRpm)
                {
                    throw CreateConfigurationException(
                        filePath,
                        $"Engine audio layer \"{layer.Id}\" must have PeakRpm greater than or equal to MinimumRpm.");
                }
            }
            else if (layer.PeakRpm <= layer.MinimumRpm)
            {
                throw CreateConfigurationException(
                    filePath,
                    $"Engine audio layer \"{layer.Id}\" must have PeakRpm greater than MinimumRpm.");
            }

            if (layer.MaximumRpm <= layer.PeakRpm)
            {
                throw CreateConfigurationException(
                    filePath,
                    $"Engine audio layer \"{layer.Id}\" must have MaximumRpm greater than PeakRpm.");
            }

            if (layer.MaximumRpm <= 0)
            {
                throw CreateConfigurationException(
                    filePath,
                    $"Engine audio layer \"{layer.Id}\" must have MaximumRpm greater than 0.");
            }

            if (layer.MaximumRpm > vehicle.MaxRpm)
            {
                throw CreateConfigurationException(
                    filePath,
                    $"Engine audio layer \"{layer.Id}\" has MaximumRpm greater than vehicle MaxRpm.");
            }

            if (layer.MinimumPitchFactor <= 0)
            {
                throw CreateConfigurationException(
                    filePath,
                    $"Engine audio layer \"{layer.Id}\" must have MinimumPitchFactor greater than 0.");
            }

            if (layer.MaximumPitchFactor < layer.MinimumPitchFactor)
            {
                throw CreateConfigurationException(
                    filePath,
                    $"Engine audio layer \"{layer.Id}\" must have MaximumPitchFactor greater than or equal to MinimumPitchFactor.");
            }

            if (layer.BaseVolume is < 0.0 or > 1.0)
            {
                throw CreateConfigurationException(
                    filePath,
                    $"Engine audio layer \"{layer.Id}\" must have BaseVolume between 0.0 and 1.0.");
            }
        }
    }

    private static void ValidateDrivingModes(VehicleProfile vehicle, string filePath)
    {
        if (string.IsNullOrWhiteSpace(vehicle.DefaultDrivingModeId))
        {
            throw CreateConfigurationException(filePath, "DefaultDrivingModeId is required.");
        }

        if (vehicle.DrivingModes is null || vehicle.DrivingModes.Count == 0)
        {
            throw CreateConfigurationException(filePath, "DrivingModes must contain at least one mode.");
        }

        var modeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasDefaultMode = false;

        foreach (var mode in vehicle.DrivingModes)
        {
            if (string.IsNullOrWhiteSpace(mode.Id))
            {
                throw CreateConfigurationException(filePath, "Driving mode Id is required.");
            }

            if (!modeIds.Add(mode.Id))
            {
                throw CreateConfigurationException(filePath, $"Duplicate driving mode Id was found: {mode.Id}");
            }

            if (string.IsNullOrWhiteSpace(mode.DisplayName))
            {
                throw CreateConfigurationException(filePath, $"Driving mode \"{mode.Id}\" requires DisplayName.");
            }

            if (mode.AccelerationMultiplier <= 0)
            {
                throw CreateConfigurationException(
                    filePath,
                    $"Driving mode \"{mode.Id}\" has AccelerationMultiplier less than or equal to 0.");
            }

            if (mode.CoastDecelerationMultiplier < 0)
            {
                throw CreateConfigurationException(
                    filePath,
                    $"Driving mode \"{mode.Id}\" has CoastDecelerationMultiplier less than 0.");
            }

            if (mode.ShiftUpIndicatorRpm <= vehicle.IdleRpm)
            {
                throw CreateConfigurationException(
                    filePath,
                    $"Driving mode \"{mode.Id}\" has ShiftUpIndicatorRpm less than or equal to IdleRpm.");
            }

            if (mode.ShiftUpIndicatorRpm > vehicle.RevLimiterRpm)
            {
                throw CreateConfigurationException(
                    filePath,
                    $"Driving mode \"{mode.Id}\" has ShiftUpIndicatorRpm greater than RevLimiterRpm.");
            }

            if (string.IsNullOrWhiteSpace(mode.AccentStyleId))
            {
                throw CreateConfigurationException(filePath, $"Driving mode \"{mode.Id}\" requires AccentStyleId.");
            }

            if (mode.AutomaticUpshiftRpm > 0 || mode.AutomaticDownshiftRpm > 0 || mode.AutomaticShiftCooldownMilliseconds > 0)
            {
                ValidateAutomaticShiftSettings(vehicle, filePath, mode);
            }

            if (string.Equals(mode.Id, vehicle.DefaultDrivingModeId, StringComparison.OrdinalIgnoreCase))
            {
                hasDefaultMode = true;
            }
        }

        if (!hasDefaultMode)
        {
            throw CreateConfigurationException(
                filePath,
                $"DefaultDrivingModeId \"{vehicle.DefaultDrivingModeId}\" was not found in DrivingModes.");
        }
    }

    private static void ValidateTransmissionModes(VehicleProfile vehicle, string filePath)
    {
        if (string.IsNullOrWhiteSpace(vehicle.DefaultTransmissionModeId))
        {
            throw CreateConfigurationException(filePath, "DefaultTransmissionModeId is required.");
        }

        if (vehicle.SupportedTransmissionModeIds is null || vehicle.SupportedTransmissionModeIds.Count == 0)
        {
            throw CreateConfigurationException(filePath, "SupportedTransmissionModeIds must contain at least one mode.");
        }

        var transmissionModeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var modeId in vehicle.SupportedTransmissionModeIds)
        {
            if (string.IsNullOrWhiteSpace(modeId))
            {
                throw CreateConfigurationException(filePath, "SupportedTransmissionModeIds contains an empty value.");
            }

            if (!transmissionModeIds.Add(modeId))
            {
                throw CreateConfigurationException(filePath, $"Duplicate transmission mode Id was found: {modeId}");
            }
        }

        if (!transmissionModeIds.Contains("manual"))
        {
            throw CreateConfigurationException(filePath, "SupportedTransmissionModeIds must contain \"manual\".");
        }

        if (!transmissionModeIds.Contains(vehicle.DefaultTransmissionModeId))
        {
            throw CreateConfigurationException(
                filePath,
                $"DefaultTransmissionModeId \"{vehicle.DefaultTransmissionModeId}\" was not found in SupportedTransmissionModeIds.");
        }

        if (transmissionModeIds.Contains("automatic"))
        {
            foreach (var mode in vehicle.DrivingModes)
            {
                ValidateAutomaticShiftSettings(vehicle, filePath, mode);
            }
        }
    }

    private static void ValidateAutomaticShiftSettings(
        VehicleProfile vehicle,
        string filePath,
        DrivingModeProfile mode)
    {
        if (mode.AutomaticUpshiftRpm <= vehicle.IdleRpm)
        {
            throw CreateConfigurationException(
                filePath,
                $"Driving mode \"{mode.Id}\" has AutomaticUpshiftRpm less than or equal to IdleRpm.");
        }

        if (mode.AutomaticUpshiftRpm > vehicle.RevLimiterRpm)
        {
            throw CreateConfigurationException(
                filePath,
                $"Driving mode \"{mode.Id}\" has AutomaticUpshiftRpm greater than RevLimiterRpm.");
        }

        if (mode.AutomaticDownshiftRpm < vehicle.IdleRpm)
        {
            throw CreateConfigurationException(
                filePath,
                $"Driving mode \"{mode.Id}\" has AutomaticDownshiftRpm less than IdleRpm.");
        }

        if (mode.AutomaticDownshiftRpm >= mode.AutomaticUpshiftRpm)
        {
            throw CreateConfigurationException(
                filePath,
                $"Driving mode \"{mode.Id}\" has AutomaticDownshiftRpm greater than or equal to AutomaticUpshiftRpm.");
        }

        if (mode.AutomaticShiftCooldownMilliseconds < 0)
        {
            throw CreateConfigurationException(
                filePath,
                $"Driving mode \"{mode.Id}\" has AutomaticShiftCooldownMilliseconds less than 0.");
        }
    }

    private static InvalidOperationException CreateConfigurationException(string filePath, string message)
    {
        return new InvalidOperationException(
            $"Vehicle configuration error in {Path.GetFileName(filePath)}:{Environment.NewLine}{message}");
    }
}
