using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using NirUsb.Domain.Enums;
using NirUsb.Domain.Models;

namespace NirUsb.Infrastructure.Helpers;

public static class UsbHelper {
    public static async Task<UsbDevice?> GetConnectedDevice() {
        OsTypes? os = OsHelper.DetectSystem();
        return os switch {
            OsTypes.Windows => await GetWindowsDevice(),
            _ => null
        };
    }


    public static async Task<bool> WriteKeyOnDevice(char letter, string userId, byte[] data) {
        string path = Path.Combine($"{letter}:\\", $"key_{userId}.dat");

        try {
            await File.WriteAllBytesAsync(path, data).ConfigureAwait(false);
            return true;
        } catch {
            return false;
        }
    }


    public static async Task<byte[]?> ReadKeyFromDevice(char letter, string userId) {
        string path = Path.Combine($"{letter}:\\", $"key_{userId}.dat");
        try {
            if (!File.Exists(path)) {
                return null;
            }

            return await File.ReadAllBytesAsync(path).ConfigureAwait(false);
        } catch {
            return null;
        }
    }


    [SupportedOSPlatform("windows")]
    private static async Task<UsbDevice?> GetWindowsDevice() {
        const string script = @"
            Get-Disk | 
            Where-Object { $_.BusType -eq 'USB' -and $_.OperationalStatus -eq 'Online' } | 
            ForEach-Object {
                $disk = $_;
                $partitions = Get-Partition -DiskNumber $disk.Number | 
                             Where-Object { $_.DriveLetter } |
                             Select-Object -ExpandProperty DriveLetter;
                
                foreach ($letter in $partitions) {
                    [PSCustomObject]@{
                        Id = $disk.SerialNumber
                        Name = $disk.Model
                        Letter = $letter.ToString()
                    }
                }
            } | ConvertTo-Json";

        var processInfo = new ProcessStartInfo {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        using Process? process = Process.Start(processInfo);
        if (process is null) {
            return null;
        }

        string jsonOutput = (await process.StandardOutput.ReadToEndAsync()).Trim();
        if (!process.WaitForExit(5000)) {
            process.Kill();
            return null;
        }

        if (string.IsNullOrWhiteSpace(jsonOutput)) {
            return null;
        }

        JsonSerializerOptions jsonOptions = new() {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        if (jsonOutput.StartsWith('[')) {
            return null;
        }

        var device = JsonSerializer.Deserialize<UsbDevice>(jsonOutput, jsonOptions);
        return device;
    }
}