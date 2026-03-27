# Touch Data Capture Service

[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14.0-239120)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

A Windows service that captures raw HID (Human Interface Device) touch input from touch screens and forwards the data to a serial device (e.g., ESP32) for further processing.

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [System Requirements](#system-requirements)
- [Quick Start](#quick-start)
- [Command-Line Arguments](#command-line-arguments)
- [Configuration](#configuration)
- [Touch Data Format](#touch-data-format)
- [Dynamic Coordinate Scaling](#dynamic-coordinate-scaling)
- [Logging](#logging)
- [Process Bypass Feature](#process-bypass-feature)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

## 🔍 Overview

This service intercepts touch events at the HID level, decodes comprehensive touch data including coordinates, pressure, contact information, and forwards it via serial communication. It features dynamic coordinate scaling, process filtering, and multiple logging formats for debugging and analysis.

### Architecture

```
Touch Screen (HID) → Raw Input API → Touch Decoder → Serial Queue → Serial Port (ESP32) ↓ Log Files (Raw/Decoded/Detailed)
```

### Project Structure
```
./ 
├── Program.cs              # Main application and HID processing 
├── Helpers/ 
│   └── WindowProcess.cs    # Window and process detection utilities 
└── bin/ 
	└── Release/ 
		└── net10.0/        # Build output
			└── win-x64/
				└── publish/
					└── TouchDataCaptureService.exe  # Published executable
```

## ✨ Features

### Core Functionality
- **Raw HID Touch Capture** - Intercepts touch events directly from HID devices using Windows Raw Input API
- **Comprehensive Touch Decoding** - Extracts all available touch parameters:
  - Position (X/Y coordinates)
  - Contact ID and count
  - Tip switch state
  - Pressure sensitivity
  - Contact dimensions (width/height)
  - Orientation (azimuth, altitude, twist)
  - Confidence and in-range detection

### Serial Communication
- **High-Speed Serial Output** - Default 3Mbps baud rate (configurable)
- **Asynchronous Queue System** - Non-blocking send/receive with dedicated threads
- **Configurable Data Format** - Send raw HID bytes or decoded touch data
- **Optional Serial Logging** - Debug serial communication when needed

### Advanced Features
- **Dynamic Coordinate Scaling** - Automatically normalizes touch coordinates to HID standard range (0-32767)
- **Process Detection & Filtering** - Optional bypass for specific applications
- **Multi-touch Support** - Handles up to 10 simultaneous contacts
- **Real-time Calibration** - Press 'R' key to reset coordinate scaling
- **Multiple Log Formats** - Raw bytes, decoded summary, and detailed human-readable logs

## 💻 System Requirements

### Hardware
- **Operating System**: Windows 11
- **Touch Screen**: HID-compliant touch device
- **Serial Port**: Physical COM port, USB-to-Serial adapter, or virtual COM port
- **Disk Space**: Minimum 100 MB (for application and logs)

### Software
- **.NET Runtime**: .NET 10 Runtime or SDK
- **Serial Drivers**: Appropriate drivers for your serial device
  - CH340/CH341 for common USB-Serial adapters
  - FTDI drivers for FTDI chips
  - CP210x drivers for Silicon Labs chips

### Permissions
- **Administrator Rights** - Required for HID device access during runtime

## 🚀 Quick Start

### Installation

1. **Install .NET 10 Runtime**

	- Download from: https://dotnet.microsoft.com/download/dotnet/10.0
	- Or use winget
		```
		winget install Microsoft.DotNet.Runtime.10	
		```
2. **Download or Build the Application**

	- Clone repository
	```
	git clone https://github.com/<your-organization>/ifpd-touchback-floatingmenu.git
	cd ifpd-touchback-floatingmenu
	```
	- Build Release version
		```
		dotnet build -c Release
		```

3. **Install Serial Drivers** (if using USB-to-Serial adapter)

	- Download the appropriate driver for your serial device:
		- **CH340/CH341**: [Download from manufacturer](http://www.wch-ic.com/downloads/CH341SER_EXE.html)
		- **FTDI**: [Download from FTDI](https://ftdichip.com/drivers/vcp-drivers/)
		- **CP210x**: [Download from Silicon Labs](https://www.silabs.com/developers/usb-to-uart-bridge-vcp-drivers)
	
	- Install the downloaded driver:
		1. Run the driver installer as Administrator
		2. Follow the installation wizard
		3. Restart your computer if prompted
	
	- Verify driver installation in Device Manager:
		```powershell
		devmgmt.msc
		```
		1. Expand **Ports (COM & LPT)**
		2. Look for your device (e.g., "USB-SERIAL CH340 (COM3)")
		3. Note the COM port number (e.g., COM3, COM10)
		4. If you see a yellow warning icon, right-click and select **Update driver**
	
	- Alternative: List COM ports using PowerShell
		```powershell
		Get-WmiObject Win32_SerialPort | Select-Object Name,DeviceID
		```

4. **Run the Application**

	* Navigate to build output
		```
		cd bin\Release\net10.0
		```
	* Run with default settings (COM10, 3Mbps)
		```
		.\TouchDataCaptureService.exe
		```
	* Or with custom settings (replace COM3 with your actual COM port)
		```
		.\TouchDataCaptureService.exe --port COM3 --baudrate 115200
		```

### First Run

1. **Connect your serial device** (ESP32, Arduino, etc.)
2. **Identify the COM port** (Device Manager → Ports)
3. **Run the application** as Administrator
4. **Calibrate coordinates** by touching all four corners of the screen
5. **Verify data** in log files or serial monitor

## ⚙️ Command-Line Arguments

### Basic Usage

```
TouchDataCaptureService.exe [options]
```

### Available Options

| Argument | Description | Default | Example |
|----------|-------------|---------|---------|
| `-port <COMx>` | Set serial port | COM10 | `-port COM3` |
| `--port <COMx>` | Set serial port (long form) | COM10 | `--port COM3` |
| `-baudrate <value>` | Set baud rate | 3000000 | `-baudrate 115200` |
| `--baudrate <value>` | Set baud rate (long form) | 3000000 | `--baudrate 115200` |
| `-useraw` | Send raw HID bytes instead of decoded data | Disabled | `-useraw` |
| `--useraw` | Send raw HID bytes (long form) | Disabled | `--useraw` |
| `-seriallog` | Enable serial communication logging | Disabled | `-seriallog` |
| `--seriallog` | Enable serial logging (long form) | Disabled | `--seriallog` |
| `-h` or `--help` | Show help message | N/A | `--help` |

### Examples

* Basic usage with default settings
	```
	TouchDataCaptureService.exe
	```
* Custom COM port
	```
	TouchDataCaptureService.exe --port COM5
	```
* Multiple arguments
	```
	TouchDataCaptureService.exe --port COM3 --baudrate 115200 --seriallog
	```
* Raw data mode
	```
	TouchDataCaptureService.exe --port COM4 --useraw
	```
* View help
	```
	TouchDataCaptureService.exe --help
	```

## 🔧 Configuration

### Default Settings

- **Serial Port**:     `COM10`   
- **Baud Rate**:       `3000000 (3Mbps)`   
- **Data Bits**:       `8`  
- **Parity**:          `None`   
- **Stop Bits**:       `One`  
- **Handshake**:       `None`   
- **Log Mode**:        `RawAndDecoded`   
- **Process Bypass**:  `Always Enabled`  
- **Serial Logging**:  `Disabled`  

### Log Modes

The application supports three logging modes (configured in code):

1. **RawOnly** - Only raw HID bytes
2. **DecodedOnly** - Only decoded touch data
3. **RawAndDecoded** - Both formats (default)

To change the log mode, modify `CurrentLogMode` in `Program.cs`:
```
private static readonly LogMode CurrentLogMode = LogMode.RawAndDecoded;
```

### Serial Configuration Notes

> **⚠️ Important: Baud Rate Limitations**
> 
> - **Maximum baud rate**: 3000000 (3 Mbps)
> - **Synchronization requirement**: The baud rate configured in this service **must match exactly** with the baud rate configured in the MCU project (e.g., ESP32).
> - Exceeding 3 Mbps may cause data corruption or communication failures.
> - Common stable rates: 9600, 115200, 921600, 3000000

### Data Format Configuration

> **📌 Note: Raw vs Decoded Data Mode**
> 
> - **Default mode**: Decoded data (CSV format)
> - **Current MCU project configuration**: Configured to receive and parse **decoded data**
> - **To use raw data mode** (`--useraw` flag): The MCU project firmware must be updated to parse raw HID byte format instead of CSV format
> - Ensure both the service and MCU firmware are synchronized on the data format being used

## 📊 Touch Data Format

### Serial Output Format (Decoded Mode)

The service sends touch data in CSV format over serial:
```
TOUCH,<X>,<Y>,<ContactID>,<TipSwitch>,<Pressure>,<InRange>,<Confidence>,<Width>,<Height>,<Azimuth>,<Altitude>,<Twist>,<ContactCount>
```

### Field Descriptions

| Field | Type | Range | Description |
|-------|------|-------|-------------|
| **X** | int | 0-32767 | Horizontal coordinate (scaled) |
| **Y** | int | 0-32767 | Vertical coordinate (scaled) |
| **ContactID** | int | 0-9 | Unique identifier for this touch point |
| **TipSwitch** | bool | 0/1 | Touch contact state (0=no touch, 1=touching) |
| **Pressure** | int | Device-dependent | Touch pressure (if supported) |
| **InRange** | bool | 0/1 | Proximity detection (hovering) |
| **Confidence** | bool | 0/1 | Touch confidence level |
| **Width** | int | Device-dependent | Contact width (if supported) |
| **Height** | int | Device-dependent | Contact height (if supported) |
| **Azimuth** | int | 0-360 | Rotation around Z-axis (degrees) |
| **Altitude** | int | 0-90 | Angle from screen surface (degrees) |
| **Twist** | int | Device-dependent | Stylus twist (if supported) |
| **ContactCount** | int | 1-10 | Total number of simultaneous contacts |

### Example Output
```
TOUCH,16384,8192,0,1,0,1,1,0,0,0,0,0,1 
TOUCH,24576,16384,1,1,0,1,1,0,0,0,0,0,2
```
**Interpretation:**
- First touch at center-right (16384, 8192), single contact
- Second touch at lower-right (24576, 16384), two total contacts

## 📏 Dynamic Coordinate Scaling

### How It Works

The service automatically normalizes touch coordinates from device-specific ranges to the standard HID range (0-32767).

1. **Initial Sampling** - Collects first 10 touch samples
2. **Range Detection** - Determines min/max X/Y values from HID logical ranges
3. **Normalization** - Scales all coordinates to 0-32767 range
4. **Continuous Updates** - Adjusts scaling as needed

### Calibration Process

For best results, calibrate by touching all four corners:

1. **Top-Left Corner** - Establishes minimum X and Y
2. **Top-Right Corner** - Establishes maximum X
3. **Bottom-Left Corner** - Establishes maximum Y
4. **Bottom-Right Corner** - Confirms full range

### Manual Reset

Press the **'R' key** while the application is running to reset scaling and recalibrate.

### Scaling Formula
```
scaledX = ((rawX - minX) * 32767) / (maxX - minX) scaledY = ((rawY - minY) * 32767) / (maxY - minY)
```

### Scaling Information

The service logs scaling information every 50 samples when run in `Debug` mode: 
```
[SCALING] Range: X(0-65535=65535) Y(0-65535=65535) | Raw(32768,16384) -> Scaled(16384,8192)
```

## 📝 Logging

### Log Files

All log files are created in the application directory:

| File | Content | Format |
|------|---------|--------|
| `hid_raw.log` | Raw HID report bytes | Hex dump |
| `hid_decoded.log` | Decoded touch data | Compact summary |
| `hid_detailed.log` | Detailed touch parameters | Human-readable |
| `serial_data.log` | Serial TX/RX data | Timestamped (optional) |

### Log File Examples

#### Raw Log Format
```
09:15:23.456 [RAW] Device:HID_12345678 Size:32 Data:05-01-40-7F-20-3F-00-01...
```

#### Decoded Log Format
```
09:15:23.456 [HID] Dev:HID_12345678 RID:5 CNT:1 X:16384 Y:8192 CID:0 TIP:1 RNG:1 CONF:1 PRESS:0 W:0 H:0 AZ:0 ALT:0 TWIST:0
```

#### Detailed Log Format
```
09:15:23.456 [DETAILED] Timestamp: 09:15:23.456 | Device: 12345678 | ReportID: 5 | ContactCount: 1 | Position: (16384, 8192) | ContactID: 0 | TipSwitch: True | InRange: True | Confidence: True | ProcessName: chrome | ProcessID: 5432
```

#### Serial Log Format (when enabled)
```
09:15:23.456 TX: TOUCH,16384,8192,0,1,0,1,1,0,0,0,0,0,1 09:15:23.457 
RX: OK
```

### Log Headers

Log files include descriptive headers explaining the format:
```
HID Touch Data Decoded Log
Generated: 2026-03-20 09:15:00
Log Mode: RawAndDecoded
Serial Port: COM10 @ 3000000 baud
```

Column Format:
```
Timestamp [Type] Device ReportID ContactCount X Y ContactID TipSwitch [...]
```

## 🚫 Process Bypass Feature

### Purpose

The service automatically skips touch events originating from specific applications, preventing interference with UI overlays. This feature is **always enabled** by default.

### Bypassed Processes (Default)

- **FloatingMenu** - Floating menu application
- **ScreenPaint** - Annotation tool

### How It Works

1. **Touch Detected** - Service captures HID coordinates
2. **Coordinate Conversion** - Converts HID coordinates to screen coordinates
3. **Process Detection** - Identifies which process owns the window at that location
4. **Filtering** - Skips logging and serial transmission if process is in bypass list

## 🔧 Troubleshooting

### 1. Serial Port Issues

**Problem:** `Failed to open serial port COM10`

**Solutions:**

* Check available COM ports in Device Manager under **Ports (COM & LPT)**.
* List COM ports with PowerShell
```
Get-WmiObject Win32_SerialPort | Select-Object Name,DeviceID
```
* Verify port is not in use
```
Get-Process | Where-Object {$_.MainWindowTitle -like "COM10"}
```
* Run as Administrator
```
Start-Process TouchDataCaptureService.exe -Verb RunAs
```

### 2. No Touch Data Received

**Problem:** Application runs but no touch events are captured

**Solutions:**

1. **Verify touch screen is working**
2. **Check HID devices**
	```
	Get-PnpDevice -Class HIDClass | Where-Object {$_.FriendlyName -like "touch"}
	```
3. **Run as Administrator** - Required for HID access
4. **Check log files** for error messages

### 3. Coordinate Scaling Issues

**Problem:** Coordinates are out of range or inaccurate

**Solutions:**

1. **Reset scaling** - Press 'R' key
2. **Calibrate** - Touch all four corners systematically
3. **Wait for samples** - Allow 10+ touches before expecting accurate scaling
4. **Check console output** - Review scaling information in the console when running in Debug mode

### 4. Serial Communication Problems

**Problem:** Data not being transmitted or received

**Solutions:**

1. **Enable serial logging** for debugging
```
TouchDataCaptureService.exe --port COM3 --seriallog
```
2. **Check serial log**
3. **Verify baud rate** matches receiver device
> **⚠️ Important:** The baud rate must be identical in both this service and the MCU project. Maximum supported rate is 3000000 bps.
	
Try common baud rates
	```
	TouchDataCaptureService.exe --port COM3 --baudrate 115200 TouchDataCaptureService.exe --port COM3 --baudrate 9600
	```
4. **Check cable** and port configuration

5. **Test with serial monitor** (Arduino IDE, PuTTY, etc.)

### 5. High CPU Usage

**Problem:** Application consuming excessive CPU

**Solutions:**

1. **Disable serial logging** (if enabled)
Remove --seriallog flag
```
TouchDataCaptureService.exe --port COM10
```
2. **Check queue backlog** in logs
```
Select-String -Path hid_decoded.log -Pattern "queue full"
```

### 6. Permission Errors

**Problem:** Access denied or insufficient privileges

**Solutions:**

* Run as Administrator
	```
	Start-Process TouchDataCaptureService.exe -Verb RunAs
	```
* Or create shortcut with "Run as administrator" enabled

## 🤝 Contributing

This is an internal Intel project. For contributions or issues:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/improvement`)
3. Commit your changes (`git commit -am 'Add new feature'`)
4. Push to the branch (`git push origin feature/improvement`)
5. Create a Pull Request


## 📄 License

**Copyright (C) 2026 Intel Corporation**  
**SPDX-License-Identifier: Apache-2.0**

Licensed under the Apache License, Version 2.0. See [Apache-2.0.txt](../../Apache-2.0.txt) file for details.

## 📚 Additional Resources

- **Deployment Guide**: See [DEPLOYMENT.md](DEPLOYMENT.md) for publishing and deployment instructions
- **Repository**: [https://github.com/<your-organization>/ifpd-touchback-floatingmenu](https://github.com/<your-organization>/ifpd-touchback-floatingmenu)
- **Issue Tracker**: Report bugs and request features via GitHub Issues
- **.NET Documentation**: [https://docs.microsoft.com/dotnet](https://docs.microsoft.com/dotnet)
- **HID Specification**: [USB.org HID Documentation](https://www.usb.org/hid)

## 🏷️ Version Information

- **Version**: 1.0.0
- **Last Updated**: March 2026
- **Target Framework**: .NET 10
- **C# Version**: 14.0
- **Platform**: Windows 11 (x64)

