using System;
using System.IO;
using System.Text.Json;
using VehicleMeterSimulator.Models;

namespace VehicleMeterSimulator.Services;

public class AudioTuningExportService
{
    private readonly string exportDirectory;

    public AudioTuningExportService()
        : this(Path.Combine(Directory.GetCurrentDirectory(), "TuningExports"))
    {
    }

    public AudioTuningExportService(string exportDirectory)
    {
        this.exportDirectory = exportDirectory;
    }

    public string Export(AudioTuningSession session)
    {
        Directory.CreateDirectory(exportDirectory);

        var fileName = $"{DateTime.Now:yyyyMMdd-HHmmss}-{SanitizeFileName(session.VehicleId)}-audio-tuning.json";
        var outputPath = Path.Combine(exportDirectory, fileName);
        var exportModel = new
        {
            exportedAt = DateTime.Now,
            note = "Temporary audio tuning values. Review manually before applying to Data/Vehicles JSON.",
            vehicleId = session.VehicleId,
            vehicleName = session.VehicleName,
            audioTuning = new
            {
                engineLayersMasterVolume = session.EngineLayersMasterVolume,
                engineLoopBaseVolume = session.EngineLoopBaseVolume,
                engineLoopThrottleVolume = session.EngineLoopThrottleVolume,
                engineAudioLayers = session.EngineAudioLayers
            }
        };

        var json = JsonSerializer.Serialize(exportModel, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(outputPath, json);
        return outputPath;
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "vehicle";
        }

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidChar, '-');
        }

        return value;
    }
}
