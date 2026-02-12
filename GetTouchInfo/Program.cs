using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO.Ports;
using System.Text;

namespace TouchDataCaptureService
{
    internal static class Program
    {
        // ===================== LOG MODE =====================
        private enum LogMode
        {
            RawOnly,
            DecodedOnly,
            RawAndDecoded
        }

        // CHANGE MODE HERE
        private static readonly LogMode CurrentLogMode = LogMode.RawAndDecoded;

        // ===================== SERIAL CONFIGURATION =====================
        // Make these configurable - can be overridden by config file or command line
        private static string SerialPortName = "COM5"; // Default value
        private static readonly int SerialBaudRate = 921600; // Changed to 921600
        private static SerialPort? _serialPort;
        private static Thread? _serialReaderThread;
        private static volatile bool _serialThreadRunning = false;

        // Configuration file path
        private static readonly string ConfigFile = Path.Combine(AppContext.BaseDirectory, "config.txt");

        // ===================== CONSTANTS =====================
        private const int WM_INPUT = 0x00FF;

        private const uint RIM_TYPEHID = 2;
        private const uint RID_INPUT = 0x10000003;

        private const uint RIDEV_INPUTSINK = 0x00000100;

        private const ushort HID_USAGE_PAGE_DIGITIZER = 0x0D;
        private const ushort HID_USAGE_TOUCH_SCREEN = 0x04;
        private const ushort HID_USAGE_PAGE_GENERIC_DESKTOP = 0x01;

        // HID Usage constants
        private const ushort HID_USAGE_GENERIC_X = 0x30;
        private const ushort HID_USAGE_GENERIC_Y = 0x31;
        private const ushort HID_USAGE_DIGITIZER_TIP_SWITCH = 0x42;
        private const ushort HID_USAGE_DIGITIZER_CONTACT_ID = 0x51;
        private const ushort HID_USAGE_DIGITIZER_TIP_PRESSURE = 0x30;
        private const ushort HID_USAGE_DIGITIZER_CONTACT_COUNT = 0x54;
        private const ushort HID_USAGE_DIGITIZER_IN_RANGE = 0x32;
        private const ushort HID_USAGE_DIGITIZER_CONFIDENCE = 0x47;
        private const ushort HID_USAGE_DIGITIZER_WIDTH = 0x48;
        private const ushort HID_USAGE_DIGITIZER_HEIGHT = 0x49;
        private const ushort HID_USAGE_DIGITIZER_AZIMUTH = 0x3F;
        private const ushort HID_USAGE_DIGITIZER_ALTITUDE = 0x40;
        private const ushort HID_USAGE_DIGITIZER_TWIST = 0x41;

        // HID API constants
        private const uint HIDP_STATUS_SUCCESS = 0x00110000;
        private const uint RIDI_PREPARSEDDATA = 0x20000005;

        // ===================== ENUMS =====================
        private enum HIDP_REPORT_TYPE
        {
            HidP_Input,
            HidP_Output,
            HidP_Feature
        }

        // ===================== STRUCTS =====================
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

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
            public string? lpszMenuName;
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
            // followed by raw bytes
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

        // ===================== DECODED TOUCH DATA =====================
        public class DecodedTouchData
        {
            public bool IsValid { get; set; }
            public string Summary { get; set; } = "";

            // Basic touch data
            public int X { get; set; }
            public int Y { get; set; }
            public int ContactId { get; set; }
            public bool TipSwitch { get; set; }

            // Extended touch data
            public bool InRange { get; set; }
            public bool Confidence { get; set; }
            public int Pressure { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int Azimuth { get; set; }
            public int Altitude { get; set; }
            public int Twist { get; set; }

            // Timing and device info
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public IntPtr DeviceHandle { get; set; }
            public byte ReportId { get; set; }
            public uint ContactCount { get; set; }
        }

        // ===================== WIN32 API =====================
        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassW(ref WNDCLASSW lpWndClass);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowExW(
            uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
            int x, int y, int w, int h,
            IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

        [DllImport("user32.dll")]
        private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint min, uint max);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string? name);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(
            RAWINPUTDEVICE[] devices, uint count, uint size);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputData(
            IntPtr hRawInput, uint command, IntPtr data,
            ref uint size, uint headerSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputDeviceInfo(
            IntPtr hDevice, uint command, IntPtr data, ref uint size);

        // HID API functions
        [DllImport("hid.dll", SetLastError = true)]
        private static extern uint HidP_GetUsageValue(
            HIDP_REPORT_TYPE reportType, ushort usagePage, ushort linkCollection,
            ushort usage, out uint usageValue, IntPtr preparsedData,
            IntPtr report, uint reportLength);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern uint HidP_GetUsages(
            HIDP_REPORT_TYPE reportType, ushort usagePage, ushort linkCollection,
            [In, Out] ushort[] usageList, ref uint usageLength, IntPtr preparsedData,
            IntPtr report, uint reportLength);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetPreparsedData(IntPtr hidDeviceObject, out IntPtr preparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();

        private const uint ERROR_SUCCESS = 0;

        // ===================== GLOBALS =====================
        private static readonly string RawLogFile =
            Path.Combine(AppContext.BaseDirectory, "hid_raw.log");

        private static readonly string DecodedLogFile =
            Path.Combine(AppContext.BaseDirectory, "hid_decoded.log");

        private static readonly string DetailedLogFile =
            Path.Combine(AppContext.BaseDirectory, "hid_detailed.log");

        private static readonly string SerialLogFile =
            Path.Combine(AppContext.BaseDirectory, "serial_data.log");

        private static WndProc? _wndProc;

        // Store preparsed data for each device
        private static readonly Dictionary<IntPtr, IntPtr> devicePreparsedData = new();
        private static readonly Dictionary<IntPtr, string> deviceNames = new();

        // Track if headers have been written
        private static bool _decodedHeaderWritten = false;
        private static bool _detailedHeaderWritten = false;
        private static bool _serialHeaderWritten = false;

        // ===================== CONFIGURATION =====================
        private static void LoadConfiguration()
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    string[] lines = File.ReadAllLines(ConfigFile);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                            continue;

                        string[] parts = line.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim().ToUpper();
                            string value = parts[1].Trim();

                            switch (key)
                            {
                                case "COMPORT":
                                case "SERIALPORT":
                                    SerialPortName = value;
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    // Create default config file
                    CreateDefaultConfigFile();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading configuration: {ex.Message}");
                Debug.WriteLine("Using default settings.");
            }
        }

        private static void CreateDefaultConfigFile()
        {
            try
            {
                string[] defaultConfig = {
                    "# Touch Data Capture Service Configuration",
                    "# Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    "#",
                    "# Serial Port Configuration",
                    "# Available ports can be found in Device Manager",
                    "# Common values: COM1, COM2, COM3, COM4, COM5, etc.",
                    "COMPORT=COM5",
                    "#",
                    "# Note: Baud rate is fixed at 921600 in the application",
                    "#",
                    "# Example configurations:",
                    "# COMPORT=COM1",
                    "# COMPORT=COM3",
                    "# COMPORT=COM10",
                    ""
                };

                File.WriteAllLines(ConfigFile, defaultConfig);
                Debug.WriteLine($"Created default configuration file: {ConfigFile}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating default config file: {ex.Message}");
            }
        }

        private static void ProcessCommandLineArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToUpper())
                {
                    case "-PORT":
                    case "--PORT":
                    case "-COM":
                    case "--COM":
                        if (i + 1 < args.Length)
                        {
                            SerialPortName = args[i + 1];
                            i++; // Skip next argument as it's the value
                        }
                        break;
                    case "-H":
                    case "--HELP":
                        ShowHelp();
                        Environment.Exit(0);
                        break;
                }
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Touch Data Capture Service");
            Console.WriteLine("Usage: TouchDataCaptureService.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -port <COMx>     Set serial port (e.g., -port COM3)");
            Console.WriteLine("  --port <COMx>    Set serial port (e.g., --port COM3)");
            Console.WriteLine("  -h, --help       Show this help message");
            Console.WriteLine();
            Console.WriteLine("Configuration:");
            Console.WriteLine($"  Config file: {ConfigFile}");
            Console.WriteLine("  Baud rate: 921600 (fixed)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  TouchDataCaptureService.exe");
            Console.WriteLine("  TouchDataCaptureService.exe -port COM3");
            Console.WriteLine("  TouchDataCaptureService.exe --port COM10");
        }

        // ===================== MAIN =====================
        [STAThread]
        private static void Main(string[] args)
        {
            // Check if help is requested first
            bool helpRequested = args.Any(arg =>
                arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("/help", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("/?", StringComparison.OrdinalIgnoreCase));

            if (helpRequested)
            {
                // Allocate console for help output
                AllocConsole();
                ShowHelp();
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                FreeConsole();
                return;
            }

            // For debugging arguments (remove in production)
            if (args.Length > 0)
            {
                AllocConsole();
                Console.WriteLine($"Arguments received: {string.Join(", ", args)}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                FreeConsole();
            }

            // Load configuration from file
            LoadConfiguration();

            // Process command line arguments (overrides config file)
            ProcessCommandLineArgs(args);

            // Initialize log files and write headers for decoded data
            InitializeLogFiles();

            // Initialize serial communication
            InitializeSerial();

            _wndProc = WindowProc;

            WNDCLASSW wc = new()
            {
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                hInstance = GetModuleHandle(null),
                lpszClassName = "RawHidTouchWindow"
            };

            RegisterClassW(ref wc);

            IntPtr hwnd = CreateWindowExW(
                0, wc.lpszClassName, "",
                0, 0, 0, 0, 0,
                IntPtr.Zero, IntPtr.Zero, wc.hInstance, IntPtr.Zero);

            RegisterHid(hwnd);

            Debug.WriteLine("=== ENHANCED RAW HID TOUCH CAPTURE WITH SERIAL STARTED ===");
            Debug.WriteLine($"Log Mode: {CurrentLogMode}");
            Debug.WriteLine($"Raw Log: {RawLogFile}");
            Debug.WriteLine($"Decoded Log: {DecodedLogFile}");
            Debug.WriteLine($"Detailed Log: {DetailedLogFile}");
            Debug.WriteLine($"Serial Log: {SerialLogFile}");
            Debug.WriteLine($"Serial Port: {SerialPortName} @ {SerialBaudRate} baud");
            Debug.WriteLine($"Config File: {ConfigFile}");
            Debug.WriteLine("Touch the screen to see logs...\n");

            // Start serial reader thread
            StartSerialReaderThread();

            while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0))
            {
                DispatchMessage(ref msg);
            }

            // Cleanup
            CleanupSerial();
        }

        // ===================== SERIAL COMMUNICATION =====================
        private static void InitializeSerial()
        {
            try
            {
                _serialPort = new SerialPort(SerialPortName, SerialBaudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000,
                    Handshake = Handshake.None,
                    DtrEnable = true,
                    RtsEnable = true
                };

                _serialPort.Open();
                Debug.WriteLine($"Serial port {SerialPortName} opened successfully at {SerialBaudRate} baud");

                // Write serial log header
                WriteSerialLogHeader();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open serial port {SerialPortName}: {ex.Message}");
                _serialPort = null;
            }
        }

        private static void StartSerialReaderThread()
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                Debug.WriteLine("Serial port not available, skipping serial reader thread");
                return;
            }

            _serialThreadRunning = true;
            _serialReaderThread = new Thread(SerialReaderWorker)
            {
                IsBackground = true,
                Name = "SerialReader"
            };
            _serialReaderThread.Start();
            Debug.WriteLine("Serial reader thread started");
        }

        private static void SerialReaderWorker()
        {
            var buffer = new StringBuilder();

            while (_serialThreadRunning && _serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    if (_serialPort.BytesToRead > 0)
                    {
                        string data = _serialPort.ReadExisting();
                        buffer.Append(data);

                        // Process complete lines
                        string bufferContent = buffer.ToString();
                        string[] lines = bufferContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        if (bufferContent.EndsWith('\r') || bufferContent.EndsWith('\n'))
                        {
                            // All lines are complete
                            foreach (string line in lines)
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    LogSerialData($"RX: {line.Trim()}");
                                }
                            }
                            buffer.Clear();
                        }
                        else if (lines.Length > 1)
                        {
                            // Process all complete lines except the last one
                            for (int i = 0; i < lines.Length - 1; i++)
                            {
                                if (!string.IsNullOrWhiteSpace(lines[i]))
                                {
                                    LogSerialData($"RX: {lines[i].Trim()}");
                                }
                            }
                            // Keep the last incomplete line in buffer
                            buffer.Clear();
                            buffer.Append(lines[lines.Length - 1]);
                        }
                    }

                    Thread.Sleep(10); // Small delay to prevent excessive CPU usage
                }
                catch (TimeoutException)
                {
                    // Normal timeout, continue
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Serial reader error: {ex.Message}");
                    LogSerialData($"ERROR: {ex.Message}");
                    Thread.Sleep(1000); // Wait before retrying
                }
            }

            Debug.WriteLine("Serial reader thread stopped");
        }

        public static void SendSerialData(string data)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    _serialPort.WriteLine(data);
                    LogSerialData($"TX: {data}");
                    Debug.WriteLine($"Serial TX: {data}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to send serial data: {ex.Message}");
                    LogSerialData($"TX_ERROR: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("Serial port not available for sending data");
            }
        }

        public static void SendTouchDataViaSerial(DecodedTouchData touchData)
        {
            if (_serialPort != null && _serialPort.IsOpen && touchData.IsValid)
            {
                try
                {
                    // Create a compact touch data message for serial transmission
                    string message = $"TOUCH,{touchData.X},{touchData.Y},{touchData.ContactId},{(touchData.TipSwitch ? 1 : 0)},{touchData.Pressure}";
                    SendSerialData(message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to send touch data via serial: {ex.Message}");
                }
            }
        }

        private static void CleanupSerial()
        {
            _serialThreadRunning = false;

            if (_serialReaderThread != null && _serialReaderThread.IsAlive)
            {
                _serialReaderThread.Join(2000); // Wait up to 2 seconds for thread to finish
            }

            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                    Debug.WriteLine("Serial port closed");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error closing serial port: {ex.Message}");
                }
            }
        }

        // ===================== LOG INITIALIZATION =====================
        private static void InitializeLogFiles()
        {
            // Clear existing log files
            if (File.Exists(RawLogFile))
                File.Delete(RawLogFile);
            if (File.Exists(DecodedLogFile))
                File.Delete(DecodedLogFile);
            if (File.Exists(DetailedLogFile))
                File.Delete(DetailedLogFile);
            if (File.Exists(SerialLogFile))
                File.Delete(SerialLogFile);

            // Write headers for decoded logs only (not for raw logs)
            if (CurrentLogMode is LogMode.DecodedOnly or LogMode.RawAndDecoded)
            {
                WriteDecodedLogHeader();
                WriteDetailedLogHeader();
            }
        }

        private static void WriteDecodedLogHeader()
        {
            var header = new List<string>
            {
                "# HID Touch Data Decoded Log",
                $"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"# Log Mode: {CurrentLogMode}",
                $"# Serial Port: {SerialPortName} @ {SerialBaudRate} baud",
                "#",
                "# Column Format:",
                "# Timestamp [Type] Device ReportID ContactCount X Y ContactID TipSwitch [Optional: InRange Confidence Pressure Width Height Azimuth Altitude Twist]",
                "#",
                "# Legend:",
                "# Dev = Device Handle (hex)",
                "# RID = Report ID",
                "# CNT = Contact Count",
                "# CID = Contact ID",
                "# TIP = Tip Switch (0/1)",
                "# RNG = In Range (0/1)",
                "# CONF = Confidence (0/1)",
                "# PRESS = Pressure",
                "# W = Width",
                "# H = Height",
                "# AZ = Azimuth",
                "# ALT = Altitude",
                "# TWIST = Twist",
                "#",
                "# ========================================",
                ""
            };

            File.WriteAllLines(DecodedLogFile, header);
            _decodedHeaderWritten = true;
        }

        private static void WriteDetailedLogHeader()
        {
            var header = new List<string>
            {
                "# HID Touch Data Detailed Log",
                $"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"# Log Mode: {CurrentLogMode}",
                $"# Serial Port: {SerialPortName} @ {SerialBaudRate} baud",
                "#",
                "# This log contains detailed breakdown of each touch event",
                "# with all available parameters in human-readable format.",
                "#",
                "# Format: Timestamp [DETAILED] Parameter1: Value1 | Parameter2: Value2 | ...",
                "#",
                "# Parameters:",
                "# - Timestamp: Event timestamp",
                "# - Device: Device handle (hex)",
                "# - ReportID: HID report identifier",
                "# - ContactCount: Number of simultaneous contacts",
                "# - Position: (X, Y) coordinates",
                "# - ContactID: Unique contact identifier",
                "# - TipSwitch: Touch contact state (True/False)",
                "# - InRange: Proximity detection (True/False)",
                "# - Confidence: Touch confidence (True/False)",
                "# - Pressure: Touch pressure (if available)",
                "# - Width/Height: Contact dimensions (if available)",
                "# - Azimuth/Altitude/Twist: Orientation data (if available)",
                "#",
                "# ========================================",
                ""
            };

            File.WriteAllLines(DetailedLogFile, header);
            _detailedHeaderWritten = true;
        }

        private static void WriteSerialLogHeader()
        {
            var header = new List<string>
            {
                "# Serial Communication Log",
                $"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"# Port: {SerialPortName}",
                $"# Baud Rate: {SerialBaudRate}",
                "#",
                "# Format: Timestamp [Direction] Data",
                "# TX = Transmitted data",
                "# RX = Received data",
                "# ERROR = Communication errors",
                "#",
                "# ========================================",
                ""
            };

            File.WriteAllLines(SerialLogFile, header);
            _serialHeaderWritten = true;
        }

        // ===================== REGISTRATION =====================
        private static void RegisterHid(IntPtr hwnd)
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

            bool success = RegisterRawInputDevices(devices, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
            Debug.WriteLine($"Raw input registration: {(success ? "SUCCESS" : "FAILED")}");
        }

        // ===================== WINDOW PROC =====================
        private static IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_INPUT)
            {
                HandleRawInput(lParam);
                return IntPtr.Zero;
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        // ===================== RAW INPUT HANDLER =====================
        private static void HandleRawInput(IntPtr lParam)
        {
            uint size = 0;
            GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref size,
                (uint)Marshal.SizeOf<RAWINPUTHEADER>());

            if (size == 0)
                return;

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                GetRawInputData(lParam, RID_INPUT, buffer, ref size,
                    (uint)Marshal.SizeOf<RAWINPUTHEADER>());

                RAWINPUTHEADER header = Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);

                if (header.dwType != RIM_TYPEHID)
                    return;

                // Ensure we have preparsed data for this device
                EnsurePreparsedData(header.hDevice);

                IntPtr hidPtr = IntPtr.Add(buffer, Marshal.SizeOf<RAWINPUTHEADER>());
                RAWHID hid = Marshal.PtrToStructure<RAWHID>(hidPtr);

                IntPtr dataPtr = IntPtr.Add(hidPtr, Marshal.SizeOf<RAWHID>());
                int bytes = (int)(hid.dwSizeHid * hid.dwCount);

                byte[] report = new byte[bytes];
                Marshal.Copy(dataPtr, report, 0, bytes);

                // ---------- RAW LOGGING (NO HEADERS) ----------
                if (CurrentLogMode is LogMode.RawOnly or LogMode.RawAndDecoded)
                {
                    string deviceName = deviceNames.GetValueOrDefault(header.hDevice, "UNKNOWN");
                    string raw = BitConverter.ToString(report);
                    LogRaw($"[RAW] Device:{deviceName} Size:{bytes} Data:{raw}");
                }

                // ---------- HID DECODING (WITH HEADERS) ----------
                if (CurrentLogMode is LogMode.DecodedOnly or LogMode.RawAndDecoded)
                {
                    uint contactCount = 0;
                    IntPtr preparsedData = devicePreparsedData[header.hDevice];
                    HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, HID_USAGE_PAGE_DIGITIZER, 0,
                        HID_USAGE_DIGITIZER_CONTACT_COUNT, out contactCount,
                        preparsedData, dataPtr, (uint)bytes);
                    for (ushort i = 0; i <= contactCount; i++)
                    {
                        var decoded = DecodeHIDReport(header.hDevice, dataPtr, (uint)bytes, i);
                        if (decoded.IsValid)
                        {
                            LogDecoded($"[HID] {decoded.Summary}");
                            LogDetailed(decoded);

                            // Send touch data via serial
                            SendTouchDataViaSerial(decoded);
                        }
                        else
                        {
                            // Fallback to basic decoding
                            string? fallback = DecodeTouchReportFallback(report);
                            if (fallback != null)
                            {
                                LogDecoded($"[FALLBACK] {fallback}");
                            }
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        // ===================== DEVICE MANAGEMENT =====================
        private static void EnsurePreparsedData(IntPtr hDevice)
        {
            if (devicePreparsedData.ContainsKey(hDevice))
                return;

            try
            {
                // Get device info
                uint deviceInfoSize = (uint)Marshal.SizeOf<RID_DEVICE_INFO>();
                IntPtr deviceInfoPtr = Marshal.AllocHGlobal((int)deviceInfoSize);

                var result = GetRawInputDeviceInfo(hDevice, RIDI_PREPARSEDDATA, IntPtr.Zero, ref deviceInfoSize);
                if (result == 0)
                {
                    uint error = GetLastError();
                    if (error != ERROR_SUCCESS)
                    {
                        // Handle error condition
                        // Check if device handle is valid, command is correct, etc.
                        Debug.WriteLine($"GetRawInputDeviceInfo failed with error: {error}");
                        // You might want to throw an exception or handle the error appropriately
                    }
                    else
                    {
                        // Otherwise, it may be a legitimate "no data" result
                        Debug.WriteLine("No data returned (legitimate case)");
                    }
                }
                try
                {
                    // result will be zero for the first call to get size, no need fo condition check

                    IntPtr preparsedDataPtr = Marshal.AllocHGlobal((int)deviceInfoSize);

                    if (GetRawInputDeviceInfo(hDevice, RIDI_PREPARSEDDATA, preparsedDataPtr, ref deviceInfoSize) > 0)
                    {
                        devicePreparsedData[hDevice] = preparsedDataPtr;

                        // Try to get a friendly device name
                        string deviceName = $"HID_{hDevice.ToString("X8")}";
                        deviceNames[hDevice] = deviceName;

                        Debug.WriteLine($"Registered HID device: {deviceName}");
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(deviceInfoPtr);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get preparsed data for device {hDevice:X8}: {ex.Message}");
            }
        }

        // ===================== HID DECODER =====================
        private static DecodedTouchData DecodeHIDReport(IntPtr hDevice, IntPtr reportData, uint reportSize, ushort linkCollection = 0)
        {
            var result = new DecodedTouchData
            {
                DeviceHandle = hDevice,
                Timestamp = DateTime.Now
            };

            if (!devicePreparsedData.ContainsKey(hDevice))
            {
                result.Summary = "NO_PREPARSED_DATA";
                return result;
            }

            IntPtr preparsedData = devicePreparsedData[hDevice];

            try
            {
                // Get the report ID first (if present)
                if (reportSize > 0)
                {
                    result.ReportId = Marshal.ReadByte(reportData);
                }

                // Get contact count first (for multi-touch)
                HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, HID_USAGE_PAGE_DIGITIZER, linkCollection,
                    HID_USAGE_DIGITIZER_CONTACT_COUNT, out uint contactCount, preparsedData, reportData, reportSize);
                result.ContactCount = contactCount;

                // Get X coordinate
                if (HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, HID_USAGE_PAGE_GENERIC_DESKTOP, linkCollection,
                    HID_USAGE_GENERIC_X, out uint x, preparsedData, reportData, reportSize) == HIDP_STATUS_SUCCESS)
                {
                    result.X = (int)x;
                }

                // Get Y coordinate
                if (HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, HID_USAGE_PAGE_GENERIC_DESKTOP, linkCollection,
                    HID_USAGE_GENERIC_Y, out uint y, preparsedData, reportData, reportSize) == HIDP_STATUS_SUCCESS)
                {
                    result.Y = (int)y;
                }

                // Get Contact ID
                if (HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, HID_USAGE_PAGE_DIGITIZER, linkCollection,
                    HID_USAGE_DIGITIZER_CONTACT_ID, out uint contactId, preparsedData, reportData, reportSize) == HIDP_STATUS_SUCCESS)
                {
                    result.ContactId = (int)contactId;
                }

                // Get Pressure
                if (HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, HID_USAGE_PAGE_DIGITIZER, linkCollection,
                    HID_USAGE_DIGITIZER_TIP_PRESSURE, out uint pressure, preparsedData, reportData, reportSize) == HIDP_STATUS_SUCCESS)
                {
                    result.Pressure = (int)pressure;
                }

                // Get Width/Height if available
                if (HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, HID_USAGE_PAGE_DIGITIZER, linkCollection,
                    HID_USAGE_DIGITIZER_WIDTH, out uint width, preparsedData, reportData, reportSize) == HIDP_STATUS_SUCCESS)
                {
                    result.Width = (int)width;
                }

                if (HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, HID_USAGE_PAGE_DIGITIZER, linkCollection,
                    HID_USAGE_DIGITIZER_HEIGHT, out uint height, preparsedData, reportData, reportSize) == HIDP_STATUS_SUCCESS)
                {
                    result.Height = (int)height;
                }

                // Get orientation data
                if (HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, HID_USAGE_PAGE_DIGITIZER, linkCollection,
                    HID_USAGE_DIGITIZER_AZIMUTH, out uint azimuth, preparsedData, reportData, reportSize) == HIDP_STATUS_SUCCESS)
                {
                    result.Azimuth = (int)azimuth;
                }

                if (HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, HID_USAGE_PAGE_DIGITIZER, linkCollection,
                    HID_USAGE_DIGITIZER_ALTITUDE, out uint altitude, preparsedData, reportData, reportSize) == HIDP_STATUS_SUCCESS)
                {
                    result.Altitude = (int)altitude;
                }

                if (HidP_GetUsageValue(HIDP_REPORT_TYPE.HidP_Input, HID_USAGE_PAGE_DIGITIZER, linkCollection,
                    HID_USAGE_DIGITIZER_TWIST, out uint twist, preparsedData, reportData, reportSize) == HIDP_STATUS_SUCCESS)
                {
                    result.Twist = (int)twist;
                }

                // Check for all digitizer usages (buttons/flags)
                ushort[] usages = new ushort[20];
                uint usageLength = (uint)usages.Length;

                if (HidP_GetUsages(HIDP_REPORT_TYPE.HidP_Input, HID_USAGE_PAGE_DIGITIZER, linkCollection,
                    usages, ref usageLength, preparsedData, reportData, reportSize) == HIDP_STATUS_SUCCESS)
                {
                    for (int i = 0; i < usageLength; i++)
                    {
                        switch (usages[i])
                        {
                            case HID_USAGE_DIGITIZER_TIP_SWITCH:
                                result.TipSwitch = true;
                                break;
                            case HID_USAGE_DIGITIZER_IN_RANGE:
                                result.InRange = true;
                                break;
                            case HID_USAGE_DIGITIZER_CONFIDENCE:
                                result.Confidence = true;
                                break;
                        }
                    }
                }

                // Build comprehensive summary
                var summaryParts = new List<string>();

                string deviceName = deviceNames.GetValueOrDefault(hDevice, "UNK");
                summaryParts.Add($"Dev:{deviceName}");

                if (result.ReportId > 0) summaryParts.Add($"RID:{result.ReportId}");
                if (result.ContactCount > 0) summaryParts.Add($"CNT:{result.ContactCount}");

                summaryParts.Add($"X:{result.X}");
                summaryParts.Add($"Y:{result.Y}");
                summaryParts.Add($"CID:{result.ContactId}");
                summaryParts.Add($"TIP:{(result.TipSwitch ? 1 : 0)}");

                if (result.InRange) summaryParts.Add("RNG:1");
                if (result.Confidence) summaryParts.Add("CONF:1");
                if (result.Pressure > 0) summaryParts.Add($"PRESS:{result.Pressure}");
                if (result.Width > 0) summaryParts.Add($"W:{result.Width}");
                if (result.Height > 0) summaryParts.Add($"H:{result.Height}");
                if (result.Azimuth > 0) summaryParts.Add($"AZ:{result.Azimuth}");
                if (result.Altitude > 0) summaryParts.Add($"ALT:{result.Altitude}");
                if (result.Twist > 0) summaryParts.Add($"TWIST:{result.Twist}");

                result.IsValid = true;
                result.Summary = string.Join(" ", summaryParts);
            }
            catch (Exception ex)
            {
                result.Summary = $"DECODE_ERROR:{ex.Message}";
            }

            return result;
        }

        // ===================== FALLBACK DECODER =====================
        private static string? DecodeTouchReportFallback(byte[] report)
        {
            if (report.Length < 2)
                return null;

            try
            {
                // Try multiple common formats
                var interpretations = new[]
                {
                    TryDecodeWindowsPrecisionTouch(report),
                    TryDecodeGenericHidTouch(report),
                    TryDecodeBasicFormat(report)
                };

                foreach (var result in interpretations)
                {
                    if (result != null)
                        return result;
                }

                return AnalyzeRawReport(report);
            }
            catch (Exception ex)
            {
                return $"Fallback Error: {ex.Message}";
            }
        }

        private static string? TryDecodeWindowsPrecisionTouch(byte[] report)
        {
            if (report.Length < 8) return null;

            try
            {
                byte reportId = report[0];
                byte contactCount = report[1];

                if (contactCount == 0 || contactCount > 10) return null;

                var contacts = new List<string>();
                int offset = 2;

                for (int i = 0; i < contactCount && offset + 6 <= report.Length; i++)
                {
                    byte flags = report[offset];
                    bool tipSwitch = (flags & 0x01) != 0;
                    bool inRange = (flags & 0x02) != 0;
                    byte contactId = (byte)(flags >> 2);

                    int x = BitConverter.ToUInt16(report, offset + 1);
                    int y = BitConverter.ToUInt16(report, offset + 3);

                    string extra = "";
                    if (offset + 7 < report.Length)
                    {
                        byte pressure = report[offset + 5];
                        byte width = report[offset + 6];
                        extra = $" P:{pressure} W:{width}";
                    }

                    contacts.Add($"[{contactId}] T:{(tipSwitch ? 1 : 0)} R:{(inRange ? 1 : 0)} X:{x} Y:{y}{extra}");
                    offset += 6;
                }

                return $"WinPrec RID:{reportId} CNT:{contactCount} " + string.Join(" ", contacts);
            }
            catch
            {
                return null;
            }
        }

        private static string? TryDecodeGenericHidTouch(byte[] report)
        {
            if (report.Length < 6) return null;

            try
            {
                var interpretations = new[]
                {
                    new {
                        Status = report[0],
                        ContactId = report[1],
                        X = (int)(report[2] | (report[3] << 8)),
                        Y = (int)(report[4] | (report[5] << 8)),
                        Format = "Generic1"
                    },
                    new {
                        Status = report[1],
                        ContactId = report[0],
                        X = (int)(report[2] | (report[3] << 8)),
                        Y = (int)(report[4] | (report[5] << 8)),
                        Format = "Generic2"
                    }
                };

                foreach (var interp in interpretations)
                {
                    if (interp.X >= 0 && interp.X <= 65535 &&
                        interp.Y >= 0 && interp.Y <= 65535 &&
                        interp.ContactId <= 10)
                    {
                        bool tipSwitch = (interp.Status & 0x01) != 0;
                        bool inRange = (interp.Status & 0x02) != 0;

                        return $"{interp.Format} T:{(tipSwitch ? 1 : 0)} R:{(inRange ? 1 : 0)} CID:{interp.ContactId} X:{interp.X} Y:{interp.Y}";
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string? TryDecodeBasicFormat(byte[] report)
        {
            if (report.Length < 6) return null;

            try
            {
                bool tipSwitch = (report[0] & 0x01) != 0;
                int contactId = report[1];
                int x = report[2] | (report[3] << 8);
                int y = report[4] | (report[5] << 8);

                if (contactId > 20 || x > 100000 || y > 100000)
                    return null;

                return $"Basic T:{(tipSwitch ? 1 : 0)} CID:{contactId} X:{x} Y:{y}";
            }
            catch
            {
                return null;
            }
        }

        private static string AnalyzeRawReport(byte[] report)
        {
            var analysis = new List<string>();

            analysis.Add($"Raw[{report.Length}]:{BitConverter.ToString(report)}");

            if (report.Length > 0)
            {
                byte first = report[0];
                analysis.Add($"B0:{Convert.ToString(first, 2).PadLeft(8, '0')}");

                for (int i = 0; i < report.Length - 1; i++)
                {
                    int val16LE = report[i] | (report[i + 1] << 8);
                    if (val16LE > 0 && val16LE < 10000)
                        analysis.Add($"P{i}:{val16LE}");
                }
            }

            return string.Join(" ", analysis);
        }

        // ===================== LOGGING =====================
        private static void LogRaw(string text)
        {
            string line = $"{DateTime.Now:HH:mm:ss.fff} {text}";
            File.AppendAllText(RawLogFile, line + Environment.NewLine);
            Debug.WriteLine(line);
        }

        private static void LogDecoded(string text)
        {
            string line = $"{DateTime.Now:HH:mm:ss.fff} {text}";
            File.AppendAllText(DecodedLogFile, line + Environment.NewLine);
            Debug.WriteLine(line);
        }

        private static void LogDetailed(DecodedTouchData data)
        {
            var details = new List<string>
            {
                $"Timestamp: {data.Timestamp:HH:mm:ss.fff}",
                $"Device: {data.DeviceHandle:X8}",
                $"ReportID: {data.ReportId}",
                $"ContactCount: {data.ContactCount}",
                $"Position: ({data.X}, {data.Y})",
                $"ContactID: {data.ContactId}",
                $"TipSwitch: {data.TipSwitch}",
                $"InRange: {data.InRange}",
                $"Confidence: {data.Confidence}"
            };

            if (data.Pressure > 0) details.Add($"Pressure: {data.Pressure}");
            if (data.Width > 0) details.Add($"Width: {data.Width}");
            if (data.Height > 0) details.Add($"Height: {data.Height}");
            if (data.Azimuth > 0) details.Add($"Azimuth: {data.Azimuth}");
            if (data.Altitude > 0) details.Add($"Altitude: {data.Altitude}");
            if (data.Twist > 0) details.Add($"Twist: {data.Twist}");

            string detailedLine = $"{DateTime.Now:HH:mm:ss.fff} [DETAILED] {string.Join(" | ", details)}";
            File.AppendAllText(DetailedLogFile, detailedLine + Environment.NewLine);
        }

        private static readonly object _serialLogLock = new object();

        private static void LogSerialData(string text)
        {
            string line = $"{DateTime.Now:HH:mm:ss.fff} {text}";

            lock (_serialLogLock)
            {
                try
                {
                    File.AppendAllText(SerialLogFile, line + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Serial log error: {ex.Message}");
                }
            }

            Debug.WriteLine($"Serial: {line}");
        }

        // ===================== CLEANUP =====================
        static Program()
        {
            // Cleanup preparsed data on exit
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                foreach (var preparsedData in devicePreparsedData.Values)
                {
                    HidD_FreePreparsedData(preparsedData);
                }
                CleanupSerial();
            };
        }
    }
}