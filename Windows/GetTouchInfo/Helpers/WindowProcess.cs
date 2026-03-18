/******************************************************************************
# Copyright (C) 2026 Intel Corporation
# SPDX-License-Identifier: Apache-2.0
#
# This software and the related documents are Intel copyrighted materials,
# and your use of them is governed by the express license under which they
# were provided to you ("License"). Unless the License provides otherwise,
# you may not use, modify, copy, publish, distribute, disclose or transmit
# this software or the related documents without Intel's prior written
# permission.
#
# This software and the related documents are provided as is, with no
# express or implied warranties, other than those that are expressly stated
# in the License.
******************************************************************************/

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

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private const int SM_CXSCREEN = 0;  // Screen width
        private const int SM_CYSCREEN = 1;  // Screen height

        private static int screenWidth = 0;
        private static int screenHeight = 0;
        private static int logicalMinX = 0;
        private static int logicalMaxX = 0;
        private static int logicalMinY = 0;
        private static int logicalMaxY = 0;

        // ⚡ Process name cache: ProcessID -> ProcessName
        private static readonly Dictionary<uint, string> ProcessInfoDict = new Dictionary<uint, string>();
        private static readonly object _cacheLock = new object();

        // ⚡ PC Cast window handle cache
        private static readonly object _pcCastLock = new object();
        private static IntPtr _pcCastWindowHandle = IntPtr.Zero;
        private static uint _cachedProcessId = 0;

        private static WindowProcessInfo currentWindowProcessInfo = new WindowProcessInfo("Unknown", 0, IntPtr.Zero, "Unknown");

        public static void InitializeScreenMetrics(int _logicalMinX, int _logicalMaxX, int _logicalMinY, int _logicalMaxY)
        {
            screenWidth = GetSystemMetrics(SM_CXSCREEN);
            screenHeight = GetSystemMetrics(SM_CYSCREEN);
            logicalMinX = _logicalMinX;
            logicalMaxX = _logicalMaxX;
            logicalMinY = _logicalMinY;
            logicalMaxY = _logicalMaxY;
            Debug.WriteLine($"Screen Metrics Initialized: Screen({screenWidth}x{screenHeight}), LogicalX({logicalMinX}-{logicalMaxX}), LogicalY({logicalMinY}-{logicalMaxY})");
        }

        // Convert HID logical coordinates to screen pixel coordinates
        public static (int screenX, int screenY) ConvertHidToScreenCoordinates(
            int hidX, int hidY)
        {
            // Map HID range to screen range, guarding against zero logical ranges
            double rangeX = logicalMaxX - logicalMinX;
            double rangeY = logicalMaxY - logicalMinY;
            double normalizedX = 0.0;
            double normalizedY = 0.0;

            if (rangeX != 0)
                normalizedX = (double)(hidX - logicalMinX) / rangeX;
            
            if (rangeY != 0)
                normalizedY = (double)(hidY - logicalMinY) / rangeY;
            
            int screenX = (int)(normalizedX * screenWidth);
            int screenY = (int)(normalizedY * screenHeight);
            
            // Clamp to valid screen coordinates (0..width-1 / 0..height-1)
            if (screenWidth > 0)
                screenX = System.Math.Max(0, System.Math.Min(screenWidth - 1, screenX));
            else
                screenX = 0;
            
            if (screenHeight > 0)
                screenY = System.Math.Max(0, System.Math.Min(screenHeight - 1, screenY));
            
            else
                screenY = 0;
            
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
                    windowHandle = GetForegroundWindow();
                }

                if (windowHandle == IntPtr.Zero)
                {
                    return new WindowProcessInfo("Unknown", 0, IntPtr.Zero, "");
                }

                // ✅ Check if same window - FAST PATH
                if (windowHandle == currentWindowProcessInfo.WindowHandle && 
                    IsWindow(currentWindowProcessInfo.WindowHandle))
                {
                    return currentWindowProcessInfo;
                }

                // ✅ Window changed - update everything
                GetWindowThreadProcessId(windowHandle, out uint processId);
                
                // ✅ Always use cache (fast when cached, updates when not)
                string processName = GetProcessNameCached(processId);

                // ✅ Get window title only when requested
                string windowTitle = "";
                if (getWindowTitle)
                {
                    StringBuilder windowTitleBuilder = new StringBuilder(256);
                    int length = GetWindowText(windowHandle, windowTitleBuilder, windowTitleBuilder.Capacity);
                    windowTitle = length > 0 ? windowTitleBuilder.ToString() : "";
                }

                // ✅ Create NEW object and cache it
                currentWindowProcessInfo = new WindowProcessInfo(processName, processId, windowHandle, windowTitle);
                
                Debug.WriteLine($"Window updated: PID={processId}, Process={processName}, Handle={windowHandle:X8}");

                return currentWindowProcessInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting process info: {ex.Message}");
                return new WindowProcessInfo("Unknown", 0, IntPtr.Zero, "");
            }
        }

        /// <summary>
        /// ⚡ OPTIMIZED: Fast check using cached window handle
        /// </summary>
        public static bool IsSourceDeviceCasted(uint processId)
        {
            lock (_pcCastLock)
            {
                // Fast path: Check if cached handle is still valid
                if (_pcCastWindowHandle != IntPtr.Zero && 
                    _cachedProcessId == processId &&
                    IsWindow(_pcCastWindowHandle) && 
                    IsWindowVisible(_pcCastWindowHandle))
                {
                    return true;
                }

                // Slow path: Handle is invalid or process changed, re-scan
                _pcCastWindowHandle = FindPCCastWindow(processId);
                _cachedProcessId = processId;

                bool isOpen = _pcCastWindowHandle != IntPtr.Zero;
                Debug.WriteLine($"PC Cast window {(isOpen ? "FOUND" : "NOT FOUND")} for PID {processId} (Handle: {_pcCastWindowHandle:X8})");
                
                return isOpen;
            }
        }

        /// <summary>
        /// Finds the PC Cast window handle for a given process
        /// </summary>
        private static IntPtr FindPCCastWindow(uint processId)
        {
            IntPtr foundHandle = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out uint windowProcessId);

                if (windowProcessId == processId && IsWindowVisible(hWnd))
                {
                    StringBuilder title = new StringBuilder(256);
                    int length = GetWindowText(hWnd, title, title.Capacity);
                    string windowTitle = length > 0 ? title.ToString() : "";

                    if (windowTitle.Contains("PC Cast", StringComparison.OrdinalIgnoreCase))
                    {
                        foundHandle = hWnd;
                        return false; // Stop enumeration
                    }
                }

                return true; // Continue enumeration
            }, IntPtr.Zero);

            return foundHandle;
        }

        /// <summary>
        /// Manually invalidate the cached PC Cast window handle (optional)
        /// </summary>
        public static void InvalidatePCCastCache()
        {
            lock (_pcCastLock)
            {
                _pcCastWindowHandle = IntPtr.Zero;
                _cachedProcessId = 0;
                Debug.WriteLine("PC Cast cache invalidated");
            }
        }

        /// <summary>
        /// Gets process name from cache if available, otherwise fetches and caches it
        /// </summary>
        private static string GetProcessNameCached(uint processId)
        {
            lock (_cacheLock)
            {
                // Check if process name is already cached
                if (ProcessInfoDict.TryGetValue(processId, out string? cachedName))
                {
                    Debug.WriteLine($"Process name retrieved from cache: {processId} -> {cachedName}");
                    return cachedName;
                }

                // Not in cache, fetch it
                string processName = "Unknown";
                try
                {
                    using (Process process = Process.GetProcessById((int)processId))
                    {
                        processName = process.ProcessName;
                    }
                    
                    // Cache the result
                    ProcessInfoDict[processId] = processName;
                    Debug.WriteLine($"Process name cached: {processId} -> {processName}");
                }
                catch (ArgumentException)
                {
                    // Process might have exited
                    processName = "Exited";
                    ProcessInfoDict[processId] = processName;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting process info: {ex.Message}");
                    processName = "AccessDenied";
                    // Don't cache errors - might be temporary
                }

                return processName;
            }
        }

        /// <summary>
        /// Clears the process name cache (useful for cleanup or testing)
        /// </summary>
        public static void ClearProcessCache()
        {
            lock (_cacheLock)
            {
                ProcessInfoDict.Clear();
                Debug.WriteLine("Process cache cleared");
            }
        }

        /// <summary>
        /// Removes a specific process from the cache (useful when a process exits)
        /// </summary>
        public static void RemoveFromProcessCache(uint processId)
        {
            lock (_cacheLock)
            {
                if (ProcessInfoDict.Remove(processId))
                {
                    Debug.WriteLine($"Process {processId} removed from cache");
                }
            }
        }

        /// <summary>
        /// Gets the current cache statistics
        /// </summary>
        public static int GetCacheSize()
        {
            lock (_cacheLock)
            {
                return ProcessInfoDict.Count;
            }
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
