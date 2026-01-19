using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using NirUsb.Domain.Enums;
using NirUsb.Domain.Models;

namespace NirUsb.Infrastructure.Helpers;

public static class UsbHelper {
    public static List<UsbDevice> GetDevices() {
        OsTypes? os = OsHelper.GetSystem();
        return os switch {
            OsTypes.Windows => GetAllOnWindows(),
            _ => []
        };
    }


    public static async Task<bool> WriteKeyToDevice(string letter, string userId, byte[] data) {
        string path = Path.Combine($"{letter}:\\", $"key_{userId}.dat");

        try {
            await File.WriteAllBytesAsync(path, data);
            return true;
        } catch {
            return false;
        }
    }


    public static async Task<byte[]?> ReadKeyFromDevice(string letter, string userId) {
        string path = Path.Combine($"{letter}:\\", $"key_{userId}.dat");
        try {
            if (!File.Exists(path)) {
                return null;
            }

            return await File.ReadAllBytesAsync(path);
        } catch {
            return null;
        }
    }


    [SupportedOSPlatform("windows")]
    private static List<UsbDevice> GetAllOnWindows() {
        List<UsbDevice> devices = [];
        const string script =
            "Get-Partition | Where-Object { $_.DriveLetter } | Get-Disk | Where-Object { $_.BusType -eq 'USB' } | Select-Object -Property SerialNumber, Model, @{N='Letter';E={(Get-Partition -DiskNumber $_.DiskNumber | Where-Object { $_.DriveLetter }).DriveLetter}} | ConvertTo-Json";

        var startInfo = new ProcessStartInfo {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        using Process? process = Process.Start(startInfo);
        if (process is null) {
            return [];
        }

        string output = process.StandardOutput.ReadToEnd();

        if (string.IsNullOrWhiteSpace(output)) {
            return devices.DistinctBy(d => d.Id).ToList();
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        string trimmedOutput = output.Trim();

        if (trimmedOutput.StartsWith('{')) {
            var single = JsonSerializer.Deserialize<RawUsbData>(trimmedOutput, options);
            if (single is null) {
                return [];
            }

            devices.Add(
                new UsbDevice {
                    Id = single.SerialNumber.Trim(),
                    Name = single.Model.Trim(),
                    Letter = single.Letter
                }
            );
        } else if (trimmedOutput.StartsWith('[')) {
            var rawData = JsonSerializer.Deserialize<List<RawUsbData>>(trimmedOutput, options);
            if (rawData is null) {
                return [];
            }

            foreach (RawUsbData rawDevice in rawData) {
                devices.Add(
                    new UsbDevice {
                        Id = rawDevice.SerialNumber.Trim(),
                        Name = rawDevice.Model.Trim(),
                        Letter = rawDevice.Letter
                    }
                );
            }
        } else {
            return [];
        }

        return devices.DistinctBy(d => d.Id).ToList();
    }


    private class RawUsbData {
        public required string SerialNumber { get; init; }
        public required string Model { get; init; }
        public required string Letter { get; set; }
    }
}