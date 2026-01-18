using System.Runtime.InteropServices;
using NirUsb.Domain.Enums;

namespace NirUsb.Infrastructure.Helpers;

public static class OsHelper {
    public static OsTypes? GetSystem() {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OsTypes.Windows :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OsTypes.Linux :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OsTypes.MacOs :
            null;
    }
}