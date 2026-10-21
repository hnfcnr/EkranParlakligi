param(
    [Parameter(Position=0)]
    [int]$Brightness = 100
)

$target = [Math]::Max(0, [Math]::Min(100, $Brightness))

$code = @"
using System;
using System.Runtime.InteropServices;

public class MonitorBrightnessControl {
    [DllImport("user32.dll")]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

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

    public static int SetAll(uint brightness) {
        int count = 0;
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) => {
            uint num = 0;
            if (GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out num) && num > 0) {
                PHYSICAL_MONITOR[] monitors = new PHYSICAL_MONITOR[num];
                if (GetPhysicalMonitorsFromHMONITOR(hMonitor, num, monitors)) {
                    foreach (var m in monitors) {
                        uint min = 0, cur = 0, max = 0;
                        if (GetMonitorBrightness(m.hPhysicalMonitor, out min, out cur, out max)) {
                            uint target = Math.Min(max, Math.Max(min, brightness));
                            if (SetMonitorBrightness(m.hPhysicalMonitor, target)) {
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

    public static int GetCurrent() {
        int val = -1;
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) => {
            uint num = 0;
            if (GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out num) && num > 0) {
                PHYSICAL_MONITOR[] monitors = new PHYSICAL_MONITOR[num];
                if (GetPhysicalMonitorsFromHMONITOR(hMonitor, num, monitors)) {
                    foreach (var m in monitors) {
                        uint min = 0, cur = 0, max = 0;
                        if (GetMonitorBrightness(m.hPhysicalMonitor, out min, out cur, out max)) {
                            val = (int)cur;
                            break;
                        }
                    }
                    DestroyPhysicalMonitors(num, monitors);
                }
            }
            return val < 0;
        }, IntPtr.Zero);
        return val;
    }
}
"@

if (-not ([System.Management.Automation.PSTypeName]'MonitorBrightnessControl').Type) {
    Add-Type -TypeDefinition $code
}

$updated = [MonitorBrightnessControl]::SetAll([uint32]$target)
Write-Output "Parlaklik %$target olarak ayarlandi ($updated monitor guncellendi)."
