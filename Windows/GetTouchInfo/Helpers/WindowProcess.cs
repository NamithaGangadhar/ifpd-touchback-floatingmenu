using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace TouchDataCaptureService.Helpers
{
    public static class WindowProcess
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        // Add these Win32 API imports
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT pt);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool inheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpBaseName, uint nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0;  // Screen width
        private const int SM_CYSCREEN = 1;  // Screen height

        private static WindowProcessInfo currentWindowProcessInfo = new WindowProcessInfo("Unknown", 0, IntPtr.Zero, "Unknown");

        // Convert HID logical coordinates to screen pixel coordinates
        public static (int screenX, int screenY) ConvertHidToScreenCoordinates(
            int hidX, int hidY,
            int logicalMinX, int logicalMaxX,
            int logicalMinY, int logicalMaxY)
        {
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            // Map HID range to screen range
            double normalizedX = (double)(hidX - logicalMinX) / (logicalMaxX - logicalMinX);
            double normalizedY = (double)(hidY - logicalMinY) / (logicalMaxY - logicalMinY);

            int screenX = (int)(normalizedX * screenWidth);
            int screenY = (int)(normalizedY * screenHeight);

            return (screenX, screenY);
        }

        public static WindowProcessInfo GetProcessAtPoint(int x, int y, bool getWindowTitle = false)
        {
            try
            {
                POINT point = new POINT { X = x, Y = y };
                IntPtr windowHandle = WindowFromPoint(point);

                if (windowHandle == IntPtr.Zero)
                {
                    // Fallback to foreground window
                    windowHandle = GetForegroundWindow();
                }

                if (windowHandle != IntPtr.Zero)
                {
                    // Get process ID
                    GetWindowThreadProcessId(windowHandle, out uint processId);
                    if (processId != currentWindowProcessInfo.ProcessId)
                    {
                        Debug.WriteLine($"New process detected: {processId}");
                        currentWindowProcessInfo.ProcessId = processId;

                    string processName = "Unknown";

                    try
                    {
                        using (Process process = Process.GetProcessById((int)processId))
                        {
                            processName = process.ProcessName;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Process might have exited
                        processName = "Exited";
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error getting process info: {ex.Message}");
                        processName = "AccessDenied";
                    }

                    currentWindowProcessInfo.ProcessName = processName;
                    currentWindowProcessInfo.WindowHandle = windowHandle;

                    // Get window title
                    if (getWindowTitle)
                    {
                        StringBuilder windowTitleBuilder = new StringBuilder(256);
                        GetWindowText(windowHandle, windowTitleBuilder, windowTitleBuilder.Capacity);
                        string windowTitle = windowTitleBuilder.ToString();
                        currentWindowProcessInfo.WindowTitle = windowTitle;
                    }

                    return currentWindowProcessInfo;
                }
                else
                {
                    Debug.WriteLine($"No change in process, return existing info");
                    return currentWindowProcessInfo;
                }
            }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting process info: {ex.Message}");
            }

            return new WindowProcessInfo("Unknown", 0, IntPtr.Zero, "");
        }
    }

    public class WindowProcessInfo
    {
        public string ProcessName { get; set; }
        public uint ProcessId { get; set; }
        public IntPtr WindowHandle { get; set; }
        public string WindowTitle { get; set; } = "Unknown";
        public WindowProcessInfo(string processName, uint processId, IntPtr windowHandle, string windowTitle)
        {
            ProcessName = processName;
            ProcessId = processId;
            WindowHandle = windowHandle;
            WindowTitle = windowTitle;
        }
    }
}
