using System.Runtime.InteropServices;

namespace RawHidTouchCapture
{
    internal static class Program
    {
        // ===================== CONSTANTS =====================
        private const int WM_INPUT = 0x00FF;
        private const int WM_DESTROY = 0x0002;

        private const uint RIM_TYPEHID = 2;
        private const uint RID_INPUT = 0x10000003;
        private const uint RIDI_DEVICEINFO = 0x2000000b;

        private const uint RIDEV_INPUTSINK = 0x00000100;

        private const ushort HID_USAGE_PAGE_DIGITIZER = 0x0D;
        private const ushort HID_USAGE_TOUCH_SCREEN = 0x04;

        // ===================== LOG FILE =====================
        private static readonly string LogDir =
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
                "RawHidTouch");

        private static readonly string LogFile =
            Path.Combine(LogDir, "hid_touch.log");

        // ===================== STRUCTS =====================
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSW
        {
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWHID
        {
            public uint dwSizeHid;
            public uint dwCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RID_DEVICE_INFO_HID
        {
            public uint dwVendorId;
            public uint dwProductId;
            public uint dwVersionNumber;
            public ushort usUsagePage;
            public ushort usUsage;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RID_DEVICE_INFO
        {
            public uint cbSize;
            public uint dwType;
            public RID_DEVICE_INFO_HID hid;
        }

        // ===================== WIN32 =====================
        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassW(ref WNDCLASSW wc);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowExW(
            uint ex, string cls, string name, uint style,
            int x, int y, int w, int h,
            IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);

        [DllImport("user32.dll")]
        private static extern bool GetMessage(out MSG msg, IntPtr hWnd, uint min, uint max);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG msg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG msg);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wp, IntPtr lp);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string name);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(
            RAWINPUTDEVICE[] devices, uint count, uint size);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputData(
            IntPtr hRawInput, uint cmd, IntPtr data,
            ref uint size, uint headerSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputDeviceInfo(
            IntPtr hDevice, uint cmd, IntPtr data, ref uint size);

        // ===================== MAIN =====================
        [STAThread]
        private static void Main()
        {
            Directory.CreateDirectory(LogDir);
            Log("=== RAW HID TOUCH CAPTURE STARTED ===");

            WndProc proc = WindowProc;

            WNDCLASSW wc = new()
            {
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(proc),
                hInstance = GetModuleHandle(null),
                lpszClassName = "RawHidTouchHiddenWindow"
            };

            RegisterClassW(ref wc);

            IntPtr hwnd = CreateWindowExW(
                0, wc.lpszClassName, "",
                0, 0, 0, 0, 0,
                IntPtr.Zero, IntPtr.Zero, wc.hInstance, IntPtr.Zero);

            RegisterTouchHid(hwnd);

            while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0))
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }

        // ===================== REGISTRATION =====================
        private static void RegisterTouchHid(IntPtr hwnd)
        {
            RAWINPUTDEVICE[] devices =
            {
                new RAWINPUTDEVICE
                {
                    usUsagePage = HID_USAGE_PAGE_DIGITIZER,
                    usUsage = HID_USAGE_TOUCH_SCREEN,
                    dwFlags = RIDEV_INPUTSINK,
                    hwndTarget = hwnd
                }
            };

            RegisterRawInputDevices(devices, 1,
                (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
        }

        // ===================== WINDOW PROC =====================
        private static IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_INPUT)
            {
                HandleRawInput(lParam);
                return IntPtr.Zero;
            }

            if (msg == WM_DESTROY)
            {
                Log("=== RAW HID TOUCH CAPTURE STOPPED ===");
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        // ===================== RAW INPUT =====================
        private static void HandleRawInput(IntPtr lParam)
        {
            uint size = 0;
            GetRawInputData(lParam, RID_INPUT, IntPtr.Zero,
                ref size, (uint)Marshal.SizeOf<RAWINPUTHEADER>());

            if (size == 0) return;

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                GetRawInputData(lParam, RID_INPUT, buffer,
                    ref size, (uint)Marshal.SizeOf<RAWINPUTHEADER>());

                RAWINPUTHEADER header =
                    Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);

                if (header.dwType != RIM_TYPEHID)
                    return;

                IntPtr hidPtr = IntPtr.Add(buffer,
                    Marshal.SizeOf<RAWINPUTHEADER>());

                RAWHID hid = Marshal.PtrToStructure<RAWHID>(hidPtr);

                IntPtr dataPtr = IntPtr.Add(hidPtr,
                    Marshal.SizeOf<RAWHID>());

                int bytes = (int)(hid.dwSizeHid * hid.dwCount);
                byte[] report = new byte[bytes];

                Marshal.Copy(dataPtr, report, 0, bytes);

                Log($"HID REPORT ({bytes}): {BitConverter.ToString(report)}");
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        // ===================== LOGGING =====================
        private static void Log(string text)
        {
            File.AppendAllText(LogFile,
                $"{DateTime.Now:HH:mm:ss.fff} {text}{Environment.NewLine}");
        }
    }
}
