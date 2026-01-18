using System.Management;
using System.Runtime.Versioning;
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


    [SupportedOSPlatform("windows")]
    private static List<UsbDevice> GetAllOnWindows() {
        List<UsbDevice> devices = [];
        ManagementObjectSearcher driveQuery =
            new("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");

        foreach (ManagementBaseObject? o in driveQuery.Get()) {
            var drive = (ManagementObject)o;
            string id = drive["SerialNumber"].ToString()!;
            string model = drive["Model"].ToString()!;

            foreach (ManagementBaseObject? managementBaseObject in drive.GetRelated(
                "Win32_DiskPartition"
            )) {
                var partition = (ManagementObject)managementBaseObject;
                foreach (ManagementBaseObject? o1 in partition.GetRelated("Win32_LogicalDisk")) {
                    var logical = (ManagementObject)o1;
                    string letter = logical["Name"].ToString()!;
                    UsbDevice device = new() {
                        Name = model,
                        Id = id,
                        Letter = letter
                    };
                    devices.Add(device);
                }
            }
        }

        return devices;
    }


    /*
    private static List<UsbDevice> GetAllOnWindows() {
        List<UsbDevice> devices = [];

        var startInfo = new ProcessStartInfo {
            FileName = "wmic.exe",
            Arguments =
                "path Win32_DiskDrive where InterfaceType='USB' get Model,SerialNumber,DeviceID /value",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        using Process? process = Process.Start(startInfo);
        if (process is null) {
            return [];
        }

        string? currentName = null;
        string? currentId = null;

        while (process.StandardOutput.ReadLine() is { } line) {
            ReadOnlySpan<char> span = line.AsSpan().Trim();
            if (span.IsEmpty) {
                continue;
            }

            if (span.StartsWith("Model=", StringComparison.OrdinalIgnoreCase)) {
                currentName = span[6..].ToString().Trim();
            } else if (span.StartsWith("SerialNumber=", StringComparison.OrdinalIgnoreCase)) {
                currentId = span[13..].ToString().Trim();
            }

            if (currentName == null || currentId == null) {
                continue;
            }

            devices.Add(
                new UsbDevice {
                    Name = ExtractName(currentName),
                    Id = ExtractId(currentId)
                }
            );

            currentName = null;
            currentId = null;
        }

        return devices;
    }


    private static string ExtractName(string name) {
        int firstSpace = name.IndexOf(' ');
        if (firstSpace == -1) {
            return name;
        }

        string part = name[firstSpace..];
        return part.Trim();
    }


    private static string ExtractId(string id) {
        int lastSlash = id.LastIndexOf('\\');
        if (lastSlash == -1) {
            return id;
        }

        string part = id[(lastSlash + 1)..];
        return part.Split('&')[0];
    }
    */
}