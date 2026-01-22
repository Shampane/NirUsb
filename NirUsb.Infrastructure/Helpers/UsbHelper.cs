using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using NirUsb.Domain.Enums;
using NirUsb.Domain.Models;

namespace NirUsb.Infrastructure.Helpers;

public static class UsbHelper {
    public static async Task<UsbDevice?> GetConnectedDevice() {
        return OsHelper.DetectSystem() switch {
            OsTypes.Windows => await GetWindowsDevice(),
            _ => null
        };
    }


    public static async Task<bool> WriteKeyOnDevice(char letter, string userId, byte[] data) {
        string path = Path.Combine($"{letter}:\\", $"key_{userId}.dat");

        try {
            await File.WriteAllBytesAsync(path, data).ConfigureAwait(false);
            return true;
        } catch (IOException) {
            return false;
        }
    }


    public static IEnumerable<string> EnumerateDatFiles(char letter) {
        string root = $"{letter}:/";
        if (!Directory.Exists(root)) {
            return [];
        }

        return Directory
            .EnumerateFiles(root, "*.dat", SearchOption.TopDirectoryOnly)
            .Where(f => {
                    var info = new FileInfo(f);
                    return info is { Exists: true, Length: < 2 * 1024 };
                }
            );
    }


    [SupportedOSPlatform("windows")]
    private static async Task<UsbDevice?> GetWindowsDevice() {
        const string script = """
                              Get-Disk | Where-Object { $_.BusType -eq 'USB' } | ForEach-Object {
                                  $disk = $_
                                  $letter = (Get-Partition -DiskNumber $disk.Number | Where-Object DriveLetter).DriveLetter
                                  if ($letter) {
                                      [PSCustomObject]@{
                                          Id = $disk.SerialNumber
                                          Name = $disk.Model
                                          Letter = $letter[0].ToString()
                                      }
                                  }
                              } | ConvertTo-Json
                              """;

        var processInfo = new ProcessStartInfo {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        try {
            using Process? process = Process.Start(processInfo);
            if (process is null) {
                return null;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            string jsonOutput = await process.StandardOutput.ReadToEndAsync(cts.Token);

            await process.WaitForExitAsync(cts.Token);

            if (string.IsNullOrWhiteSpace(jsonOutput)) {
                return null;
            }

            JsonSerializerOptions jsonOptions = new() {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            if (!jsonOutput.TrimStart().StartsWith('[')) {
                return JsonSerializer.Deserialize<UsbDevice>(jsonOutput, jsonOptions);
            }

            var devices = JsonSerializer.Deserialize<List<UsbDevice>>(jsonOutput, jsonOptions);
            return devices?.FirstOrDefault();
        } catch {
            return null;
        }
    }
}