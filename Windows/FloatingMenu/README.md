# Floating Menu Application

[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14.0-239120)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![WPF](https://img.shields.io/badge/WPF-Windows-blue)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

A Windows WPF application that provides an edge-docked floating menu interface for camera switching, signal source management, and screen annotation integration on interactive flat panel displays (IFPD).

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [System Requirements](#system-requirements)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Configuration](#configuration)
- [User Interface](#user-interface)
- [Camera Management](#camera-management)
- [Signal Source Detection](#signal-source-detection)
- [Screen Annotation Integration](#screen-annotation-integration)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

## 🔍 Overview

The Floating Menu application provides a convenient edge-docked interface for managing camera feeds and signal sources on Windows-based interactive displays. It features a collapsible menu system, real-time camera preview, automatic camera detection, and integration with screen annotation tools.

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Floating Menu Application                    │
├─────────────────────────────────────────────────────────────────┤
│  Edge Handle  →  Menu Control  →  Signal Source Manager         │
│      ↓               ↓                    ↓                     │
│  Draggable    Menu Items      Camera Detection (pnputil)        │
│  Collapse     - Home          Camera List (ObservableCollection)│
│  Expand       - Exit                      ↓                     │
│  Always-On    - Signal Source  →  Camera Window                 │
│               - Annotation              (OpenCvSharp)           │
│               - Settings                  ↓                     │
│                                    Full Screen Display          │
├─────────────────────────────────────────────────────────────────┤
│  External Integration: Screen Paint / Annotation Tool           │
└─────────────────────────────────────────────────────────────────┘
```

### Project Structure
```
FloatingMenu/ 
├── MainWindow.xaml(.cs)           # Main application window and logic 
├── App.xaml(.cs)                  # Application startup and configuration 
├── Controls/ 
│   ├── EdgeHandleControl.xaml(.cs)   # Collapsible edge handle 
│   ├── EdgeMenuControl.xaml(.cs)     # Main menu interface 
│   ├── SignalSource.xaml(.cs)        # Camera/signal source management 
│   └── CameraWindow.xaml(.cs)        # Full-screen camera display 
├── Helpers/ 
│   ├── SignalSourceModel.cs          # Camera/signal source data model 
│   └── DeviceStatusEnum.cs           # Device connection status enum 
├── Styles/ 
│   ├── ButtonDictionary.xaml         # Custom button styles 
│   └── ListDictionary.xaml           # Custom list styles 
└── bin/ 
	└── Release/ 
		└── net10.0/ 
			└── FloatingMenu.exe      # Published executable
```
## ✨ Features

### Core Functionality
- **Edge-Docked Interface** - Always-accessible menu docked to the right edge of the screen
- **Collapsible Design** - Expands/collapses with smooth animations
- **Draggable Handle** - Vertically repositionable while staying docked to the edge
- **Auto-Docking** - Automatically returns to right edge when moved

### Camera Management
- **Automatic Camera Detection** - Enumerates connected cameras using Windows PnP utilities
- **Real-Time Preview** - Full-screen camera feed using OpenCvSharp
- **Multi-Camera Support** - Switch between multiple connected cameras
- **Dynamic Resolution** - Automatically adapts to primary screen resolution
- **High Frame Rate** - 60 FPS camera capture (configurable)

### Signal Source Management
- **Device Status Tracking** - Shows Available, Connected, or Disconnected states
- **Observable Collection** - Real-time UI updates when devices change
- **Single Connection Mode** - Only one camera can be active at a time
- **Visual Status Indicators** - Color-coded status for each signal source

### Screen Annotation Integration
- **External App Launch** - Integrates with ScreenPaint annotation tool
- **Process Monitoring** - Detects when annotation tool closes
- **Seamless Workflow** - Menu auto-collapses when launching annotation

### User Interface
- **Responsive Sizing** - Adapts to screen dimensions dynamically
- **Smooth Animations** - Polished expand/collapse transitions
- **Flyout Panels** - Slide-out configuration panels
- **Custom Styling** - Branded button and list styles
- **Menu Items**:
  - Home (Collapse menu)
  - Exit (Close application)
  - Signal Source (Camera selection)
  - Annotation (Launch screen annotation tool)
  - Settings (Future expansion)

## 💻 System Requirements

### Hardware
- **Operating System**: Windows 11
- **Display**: Interactive flat panel display or standard monitor
- **Camera**: USB camera(s) compatible with DirectShow (DSHOW)
- **RAM**: Minimum 4 GB (8 GB recommended for HD camera feeds)
- **Disk Space**: 200 MB (for application and dependencies)

### Software
- **.NET Runtime**: .NET 10 Runtime or SDK
- **Camera Drivers**: DirectShow-compatible camera drivers
- **Optional**: ScreenPaint or compatible annotation software

### Dependencies
- **OpenCvSharp4**: Computer vision library for camera capture
- **OpenCvSharp4.WpfExtensions**: WPF integration for OpenCvSharp
- **System.Drawing.Common**: Image processing support

### Permissions
- **Standard User** - No administrator rights required for normal operation
- **Camera Access** - Windows Camera privacy settings must allow desktop apps

## 🚀 Quick Start

### Installation

1. **Install .NET 10 Runtime**

   - Download from: https://dotnet.microsoft.com/download/dotnet/10.0
   - Or use winget:
	   ```
		winget install Microsoft.DotNet.Runtime.10
	   ```
2. **Download or Build the Application**

   - Clone repository:
	```
	git clone https://github.com/intel-sandbox/ifpd-touchback-floatingmenu.git 
	cd ifpd-touchback-floatingmenu\Windows\FloatingMenu
	```
   - Build Release version:

3. **Run the Application**

	- Navigate to build output:
		```
		cd bin\Release\net10.0-windows
		```
	- Launch application:
		```
		 .\FloatingMenu.exe
		```

### First Run

1. **Application starts** with a thin edge handle on the right side of the screen
2. **Click the edge handle** to expand the menu
3. **Select "Signal Source"** to view available cameras
4. **Click a camera** from the list to open full-screen preview
5. **Drag the edge handle** vertically to reposition as needed

## ⚙️ Configuration

### Default Settings

- **Window Position**: Right edge of screen, vertically centered
- **Collapsed Size**: 3.5% screen width × 25% screen height
- **Expanded Size**: 18% screen width × 45% screen height
- **Camera Frame Rate**: 60 FPS
- **Camera Resolution**: Matches primary screen resolution
- **Video Capture API**: DirectShow (DSHOW)

### Customization

* To modify window dimensions, edit `MainWindow.xaml.cs`:
	```
	// Collapsed state this.Width = screenWidth * 0.035;  // 3.5% of screen width this.Height = screenHeight * 0.25;  // 25% of screen height
	// Expanded state this.Width = screenWidth * 0.18;   // 18% of screen width this.Height = screenHeight * 0.45; // 45% of screen height
	```

* To adjust camera settings, edit `CameraWindow.xaml.cs`:
	```
	 _capture.Set(VideoCaptureProperties.FrameWidth, screenWidth); 
	 _capture.Set(VideoCaptureProperties.FrameHeight, screenHeight);
	 _capture.Set(VideoCaptureProperties.Fps, 60);
	```

### Screen Annotation Tool Path

To configure the annotation tool path, edit `MainWindow.xaml.cs`:
```
string exePath = @"C:\Program Files\WindowsApps\19566Hanakiansoftware.ScreenPaint_1.3.3.0_x64__y1w6xw98tx1ba\DesktopDrawing\ScreenPaint.exe";
```

## 🖥️ User Interface

### Edge Handle (Collapsed State)

<table>
<tr>
<td width="70%">

- **Size**: Thin vertical bar (3.5% × 25% of screen)
- **Position**: Right edge, vertically draggable
- **Interaction**: Click to expand, drag to reposition vertically
- **Behavior**: Auto-docks to right edge when moved

</td>
<td width="30%" align="center">

<img src="../../images/FloatingMenu_EdgeHandle.png" alt="Edge Handle" height="200px" width="120px"/>

</td>
</tr>
</table>

### Menu (Expanded State)

<table>
<tr>
<td width="70%">

- **Size**: Larger panel (18% × 45% of screen)
- **Menu Items**:
  1. **Home** - Collapse menu
  2. **Exit** - Close application
  3. **Signal Source** - Manage cameras
  4. **Annotation** - Launch ScreenPaint
  5. **Settings** - Reserved for future use

</td>
<td width="30%" align="center">

<img src="../../images/FloatingMenu_EdgeMenu.png" alt="Menu Expanded" height="400px" width="150px"/>

</td>
</tr>
</table>

### Signal Source Flyout

<table>
<tr>
<td width="70%">

- **Camera List**: Shows all detected cameras (PC1, PC2, etc.)
- **Status Indicators**:
  - **Green**: Available
  - **Blue**: Connected
  - **Gray**: Disconnected
- **Selection**: Click to connect/disconnect camera
- **Behavior**: Only one camera can be active at a time

</td>
<td width="30%" align="center">

<img src="../../images/FloatingMenu_SignalSourceMenu.png" alt="Signal Source" width="200px"/>

</td>
</tr>
</table>

### Camera Window

- **Display**: Full-screen borderless window
- **Resolution**: Matches primary screen
- **Frame Rate**: 60 FPS
- **Controls**: Close window to disconnect camera
- **Auto-Return**: Menu reopens with Signal Source flyout when camera closes

## 📹 Camera Management

### Automatic Detection

The application uses Windows `pnputil` command to enumerate connected cameras:
```
pnputil /enum-devices /class Camera /connected
```
### Connection Workflow

1. **User clicks** Signal Source menu item
2. **Application queries** connected cameras via PnP
3. **Devices populate** in observable collection
4. **User selects** a camera from the list
5. **Camera window opens** in full-screen mode
6. **OpenCvSharp** captures frames via DirectShow API
7. **Frames display** at 60 FPS with automatic resolution

### Camera Disconnection

- Click the camera window close button
- Select "Exit" from menu while camera is active
- Select the same camera again in Signal Source list

### Supported Camera Types

- USB webcams (UVC-compatible)
- Integrated laptop cameras
- Document cameras
- Any DirectShow-compatible video capture device

## 🔍 Signal Source Detection

### Detection Process
```
var process = new Process(); 
process.StartInfo.FileName = "cmd.exe"; 
process.StartInfo.Arguments = "/c pnputil /enum-devices /class Camera /connected"; 
process.StartInfo.RedirectStandardOutput = true; process.StartInfo.UseShellExecute = false; 
process.StartInfo.CreateNoWindow = true;
process.Start(); 
string output = await process.StandardOutput.ReadToEndAsync();
```
### Parsing Camera Information

The application extracts camera names from:
- **Device Description** fields
- **Friendly Name** fields

### Status Management

- **Available** (default): Camera detected and ready
- **Connected**: Camera actively streaming
- **Disconnected**: Camera removed or unavailable

## 🎨 Screen Annotation Integration

### Supported Annotation Tool

<div>
	<img src = "../../images/FloatingMenu_AnnotationTool.png" alt = "Annotation Tool">
</div>

- **ScreenPaint** by Hanakian software
- Version: 1.3.3.0 or later
- Platform: Windows x64

### Integration Features

- **Process Launching**: Starts external annotation application
- **Process Monitoring**: Detects when annotation tool exits
- **UI Coordination**: Menu auto-collapses when launching
- **Menu State**: Clears selection when annotation closes

### Workflow

1. User clicks **Annotation** menu item
2. Application launches **ScreenPaint.exe**
3. Menu **auto-collapses** to minimize interference
4. User annotates screen using ScreenPaint
5. User closes ScreenPaint
6. Application **detects exit** and clears menu selection

### Custom Annotation Tool

To integrate a different annotation tool modify the `exePath` in `LaunchAnnotationAppAsync` method in `MainWindow.xaml.cs`:
```
string exePath = @"C:\Path\To\Your\AnnotationTool.exe";
```

## 🔧 Troubleshooting

### 1. No Cameras Detected

**Problem:** Signal Source list is empty

**Solutions:**

1. **Verify camera connection**
   - Check USB cable
   - Try different USB port
   - Restart camera if external

2. **Check Windows Camera permissions**
   - Settings → Privacy → Camera
   - Enable "Allow desktop apps to access your camera"

3. **Verify camera in Device Manager**
   - Device Manager → Cameras or Imaging devices
   - Ensure no yellow warning icons

4. **Test with PowerShell**
	```
	pnputil /enum-devices /class Camera /connected
	```
5. **Update camera drivers**
	```
	Device Manager → Right-click camera → Update driver
	```
6. **Test camera with Camera app**
	```
	start microsoft.windows.camera:
	```

### 2. Camera Window Shows Black Screen

**Problem:** Camera window opens but displays black/frozen image

**Solutions:**

1. **Check camera is not in use by another application**
	- Close Zoom,Teams, Skype, etc.
	- Check for other camera apps in Task Manager

2. **Verify DirectShow support**
	- Some cameras may not support DirectShow API
	- Try with different camera

3. **Adjust resolution settings**
	- Lower the resolution in `CameraWindow.xaml.cs`:
		```
		_capture.Set(VideoCaptureProperties.FrameWidth, 1920); 
		_capture.Set(VideoCaptureProperties.FrameHeight, 1080);
		```
4. **Check OpenCvSharp installation**
	```
	dotnet list package | findstr OpenCvSharp
	```
5. **Restart camera device:**
   - Device Manager → Right-click camera → Disable device
   - Wait 5 seconds
   - Right-click camera → Enable device

### 3. Application Won't Start

**Problem:** Double-clicking executable does nothing or shows error

**Solutions:**

1. **Verify .NET 10 Runtime installed**
	```
	dotnet --list-runtimes
	```
	Should show: `Microsoft.WindowsDesktop.App 10.x.x`

2. **Check for missing dependencies**
	- Ensure OpenCvSharp native libraries are present
	- Run from command prompt to see error messages:
		```
		.\FloatingMenu.exe
		```
3. **Run as different user**
	- Right-click → Run as administrator

### 4. Menu Position Issues

**Problem:** Menu appears in wrong position or doesn't dock correctly

**Solutions:**

1. **Check multi-monitor setup**
   - Application targets primary monitor
   - Move to primary monitor if on secondary

2. **Reset window position**
   - Close application
   - Delete user settings (if any)
   - Restart application

3. **Verify screen resolution**
   - Application calculates position based on screen dimensions
   - Try at native resolution

### 5. Annotation Tool Won't Launch

**Problem:** Clicking Annotation menu item does nothing

**Solutions:**

1. **Verify ScreenPaint installed**
   ```
	Get-ChildItem "C:\Program Files\WindowsApps" -Filter "ScreenPaint" -Recurse -Directory
   ```
2. **Find actual installation path:**
   ```
    Get-ChildItem "C:\Program Files\WindowsApps" -Filter "ScreenPaint.exe" -Recurse
   ```
3. **Check file path**
	```
	string exePath = @"C:\Program Files\WindowsApps...";
	```
	- Update path if ScreenPaint version changed or if installed in different location

4. **Install ScreenPaint from Microsoft Store**
	- Search for "ScreenPaint" by Hanakian

5. **Check file permissions**
	- Ensure user has execute permission for annotation tool
	
6. **Check Event Viewer for errors**:
	```
	Event Viewer → Windows Logs → Application
	```

### 6. Camera Not Releasing

**Problem:** Camera stays in use after closing Camera Window

**Solutions:**

1. **Force application restart**
	- Close FloatingMenu completely
	- Relaunch application

2. **Check Task Manager**
	- Look for lingering FloatingMenu processes
	- End any orphaned processes

3. **Review cleanup code**
	- Ensure `StopCamera()` is called:
		```
		_capture?.Release();
	      _capture?.Dispose();
		```

### 7. High CPU/Memory Usage

**Problem:** Application consuming excessive resources

**Solutions:**

1. **Lower camera frame rate**
	```
	_capture.Set(VideoCaptureProperties.Fps, 30); // Reduce from 60
	```
2. **Reduce camera resolution**
	```
	_capture.Set(VideoCaptureProperties.FrameWidth, 1280); 
	_capture.Set(VideoCaptureProperties.FrameHeight, 720);
	```
3. **Close unused camera windows**

4. **Check for memory leaks**
	```
	Get-Process FloatingMenu | Select-Object Name, CPU, WorkingSet
	```
5. **Monitor over time:**
   - Open Task Manager
   - Performance tab → Monitor FloatingMenu process

6. **If issue persists:**
   - Close and restart application

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

- **Repository**: [https://github.com/intel-sandbox/ifpd-touchback-floatingmenu](https://github.com/intel-sandbox/ifpd-touchback-floatingmenu)
- **Issue Tracker**: Report bugs and request features via GitHub Issues
- **.NET Documentation**: [https://docs.microsoft.com/dotnet](https://docs.microsoft.com/dotnet)
- **WPF Documentation**: [https://docs.microsoft.com/dotnet/desktop/wpf/](https://docs.microsoft.com/dotnet/desktop/wpf/)
- **OpenCvSharp**: [https://github.com/shimat/opencvsharp](https://github.com/shimat/opencvsharp)
- **ScreenPaint**: [Microsoft Store](https://www.microsoft.com/store/apps)

## 🏷️ Version Information

- **Version**: 1.0.0
- **Last Updated**: March 2026
- **Target Framework**: .NET 10
- **C# Version**: 14.0
- **Platform**: Windows 11 (x64)
- **UI Framework**: WPF (Windows Presentation Foundation)