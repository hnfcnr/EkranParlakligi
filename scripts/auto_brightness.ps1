param(
    [Parameter(Position=0)]
    [int]$Target = -1
)

# 08:00 - 18:00 arasi: %100 (Gunduz Modu)
# 18:00 - 08:00 arasi: %10 (Aksam Modu)
$currentHour = (Get-Date).Hour

if ($Target -lt 0 -or $Target -gt 100) {
    if ($currentHour -ge 8 -and $currentHour -lt 18) {
        $targetVal = 100
        $modeName = "Gunduz Modu (%100)"
    } else {
        $targetVal = 10
        $modeName = "Aksam Modu (%10)"
    }
} else {
    $targetVal = $Target
    $modeName = "Manuel Hedef (%$Target)"
}

$code = @"
using System;
using System.Runtime.InteropServices;

public class AutoBrightnessHelper {
    [DllImport("user32.dll")]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);
    public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect { public int left, top, right, bottom; }

    [DllImport("dxva2.dll", SetLastError = true)]
    public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct PHYSICAL_MONITOR {
        public IntPtr hPhysicalMonitor;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }

    [DllImport("dxva2.dll", SetLastError = true)]
    public static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", SetLastError = true)]
    public static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", SetLastError = true)]
    public static extern bool GetMonitorBrightness(IntPtr hMonitor, out uint pdwMinimumBrightness, out uint pdwCurrentBrightness, out uint pdwMaximumBrightness);

    [DllImport("dxva2.dll", SetLastError = true)]
    public static extern bool SetMonitorBrightness(IntPtr hMonitor, uint dwNewBrightness);

    public static int Apply(uint target) {
        int count = 0;
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) => {
            uint num = 0;
            if (GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out num) && num > 0) {
                PHYSICAL_MONITOR[] monitors = new PHYSICAL_MONITOR[num];
                if (GetPhysicalMonitorsFromHMONITOR(hMonitor, num, monitors)) {
                    foreach (var m in monitors) {
                        uint min = 0, cur = 0, max = 0;
                        if (GetMonitorBrightness(m.hPhysicalMonitor, out min, out cur, out max)) {
                            uint safeTarget = Math.Min(max, Math.Max(min, target));
                            if (cur != safeTarget) {
                                if (SetMonitorBrightness(m.hPhysicalMonitor, safeTarget)) {
                                    count++;
                                }
                            } else {
                                count++;
                            }
                        }
                    }
                    DestroyPhysicalMonitors(num, monitors);
                }
            }
            return true;
        }, IntPtr.Zero);
        return count;
    }
}
"@

if (-not ([System.Management.Automation.PSTypeName]'AutoBrightnessHelper').Type) {
    Add-Type -TypeDefinition $code
}

$updatedMonitors = [AutoBrightnessHelper]::Apply([uint32]$targetVal)
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$logLine = "[$timestamp] Otomatik Ekran Parlakligi: Saat ${currentHour}:00 -> $modeName -> Hedef: %$targetVal ($updatedMonitors monitor senkronize)"

# Standard AppData log directory
$logDir = Join-Path $env:APPDATA "EkranParlakligi\logs"
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}
$logPath = Join-Path $logDir "auto_brightness.log"
Add-Content -Path $logPath -Value $logLine -Encoding UTF8

Write-Output $logLine
