# Floating Menu Application - Deployment Guide

This guide covers building, publishing, and deploying the Floating Menu Application for production use on interactive flat panel displays (IFPD) and Windows systems.

## 📋 Table of Contents

- [Prerequisites](#prerequisites)
- [Building the Application](#building-the-application)
- [Publishing Options](#publishing-options)
- [Manual Deployment](#manual-deployment)
- [Testing the Deployment](#testing-the-deployment)
- [Startup Configuration](#startup-configuration)
- [Screen Annotation Integration](#screen-annotation-integration)
- [Updates and Maintenance](#updates-and-maintenance)
- [Uninstalling](#uninstalling)

## ✅ Prerequisites

### Development Machine

- **Windows 11** with latest updates
- **.NET 10 SDK** installed
- **Git** for source control
- **Visual Studio 2022** or **Visual Studio 2026** (recommended)
- **OpenCvSharp4** NuGet packages available

### Target Machine

- **Windows 11**
- **.NET 10 Runtime** (for framework-dependent deployment) or **.NET 10 Desktop Runtime**
- **DirectShow-compatible camera** (USB webcam, document camera, etc.)
- **Display**: Interactive flat panel display or standard monitor
- **RAM**: Minimum 4 GB (8 GB recommended for HD camera feeds)
- **Disk Space**: 200 MB (for application and dependencies)
- **Camera Access**: Windows Camera privacy settings must allow desktop apps

## 🔨 Building the Application

### 1. Clone the Repository
   
   - Clone from GitHub
	   ```
	   git clone https://github.com/intel-sandbox/ifpd-touchback-floatingmenu.git
	   cd ifpd-touchback-floatingmenu
	   ```
   - Or navigate to existing clone
	
	  ```
	  cd C:\<clone-directory>\ifpd-touchback-floatingmenu
	  ```

### 2. Restore Dependencies

 - Restore NuGet packages
	
	```
	dotnet restore
	```
 - Verify restoration
	
	```
	dotnet list package
	```
  - Expected packages:
	- OpenCvSharp4
	- OpenCvSharp4.WpfExtensions
	- System.Drawing.Common

### 3. Build Release Version

 - Build in Release configuration
	```
	dotnet build -c Release
	```
 - Build output location: `bin\Release\net10.0-windows\`

### 4. Verify Build

 - Navigate to output directory
	```
	cd bin\Release\net10.0-windows
	```

 - List files
	```
	dir
	```
- Expected files:
	- FloatingMenu.exe
	- FloatingMenu.dll
	- OpenCvSharp*.dll (multiple OpenCV libraries)
	- FloatingMenu.runtimeconfig.json
	- FloatingMenu.deps.json
	- Controls, Helpers, Styles folders (if not embedded)

## 📦 Publishing Options

### Option 1: Framework-Dependent Deployment (FDD)

**Smaller size, requires .NET Desktop Runtime on target machine**

Publish for any Windows x64 machine:
```
dotnet publish -c Release -r win-x64 --no-self-contained
```
Output location: `bin\Release\net10.0-windows\win-x64\publish\`

### Option 2: Self-Contained Deployment (SCD)

**Larger size, no runtime required on target machine**

Publish with bundled runtime:
```
dotnet publish -c Release -r win-x64 --self-contained true
```
Output location: `bin\Release\net10.0-windows\win-x64\publish\`

### Option 3: Single File Executable

**All-in-one executable file (recommended for easy deployment)**

Publish as single executable:
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
Optional: Enable compression:
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```
**Note**: `IncludeNativeLibrariesForSelfExtract=true` is important for OpenCvSharp native libraries.

Output: Single FloatingMenu.exe file

### Option 4: Trimmed Deployment

**Reduced size by removing unused code**

Publish with trimming:
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishTrimmed=true -p:TrimMode=partial
```
## 🚀 Manual Deployment

### Step 1: Prepare Deployment Package

* Navigate to publish directory:
	```
	cd bin\Release\net10.0-windows\win-x64\publish
	```
* Create deployment folder:
	```
	$deployPath = "C:\Temp\FloatingMenu_Deployment"
	New-Item -Path $deployPath -ItemType Directory -Force
	```
* Copy all files:
	```
	Copy-Item -Path * -Destination $deployPath -Recurse
	```
* Optional: Create ZIP for distribution:
	```
	$zipPath = Join-Path (Split-Path $deployPath -Parent) 'FloatingMenu.zip'
	Compress-Archive -Path "$deployPath\*" -DestinationPath $zipPath
	```
### Step 2: Transfer to Target Machine

Choose your transfer method:

* **Option A: USB Drive**

	Copy to USB drive:
	```
	Copy-Item -Path $deployPath -Destination "E:\FloatingMenu" -Recurse
	```
* **Option B: Email/Cloud**

	Use the ZIP file created earlier. Upload to OneDrive, Google Drive, or email.

### Step 3: Install on Target Machine

* Create installation directory:
	```
	$installPath = "C:\Program Files\FloatingMenu"
	New-Item -Path $installPath -ItemType Directory -Force
	```
* Copy files from deployment package:
	```	
	Copy-Item -Path "E:\FloatingMenu\*" -Destination $installPath -Recurse
	```
* Or extract from ZIP:
	```
	Expand-Archive -Path "C:\Downloads\FloatingMenu.zip" -DestinationPath $installPath
	```
### Step 4: Install .NET Runtime (if using FDD)

If you published as framework-dependent, install .NET 10 Desktop Runtime:

* **Option 1: Direct download**
	```
	Start-Process "https://dotnet.microsoft.com/download/dotnet/10.0"
	```
* **Option 2: Using winget**
	```
	winget install Microsoft.DotNet.DesktopRuntime.10
	```
* **Verify installation:**
	```
	dotnet --list-runtimes
	```
	Should show: `Microsoft.WindowsDesktop.App 10.x.x`

### Step 5: Configure Camera Permissions

Enable camera access for desktop apps:

1. Open **Settings** → **Privacy & Security** → **Camera**
2. Enable **"Let desktop apps access your camera"**
3. Verify camera is working in Device Manager

### Step 6: Verify Installation

Navigate to install directory:
```
cd "C:\Program Files\FloatingMenu"
```
Test run:
```
.\FloatingMenu.exe
```
Expected behavior:
- Application starts with edge handle on right side of screen
- No error messages
- UI is responsive

## ✅ Testing the Deployment

### Test 1: Basic Functionality

* Run the application:  

* Verify:
	- ✅ Edge handle appears on right side of screen
	- ✅ Handle is draggable vertically
	- ✅ Clicking handle expands the menu
	- ✅ Menu items are visible and clickable

### Test 2: Camera Detection

1. **Ensure camera is connected** (USB or integrated)
2. **Click edge handle** to expand menu
3. **Click "Signal Source"** menu item
4. **Verify cameras are listed** (PC1, PC2, etc.)

* Check if cameras are detected:

* Test camera detection via PnP
	```
	pnputil /enum-devices /class Camera /connected
	```

### Test 3: Camera Preview

1. **Select a camera** from Signal Source list
2. **Verify**:
   - ✅ Full-screen camera window opens
   - ✅ Live camera feed displays (not black screen)
   - ✅ Frame rate is smooth (~60 FPS)
   - ✅ Resolution matches screen
   - ✅ No lag or freezing

3. **Close camera window**
4. **Verify**:
   - ✅ Camera releases properly
   - ✅ Menu returns to Signal Source view
   - ✅ Camera status changes appropriately

### Test 4: Menu Navigation

Test each menu item:
- ✅ **Home** - Collapses menu to edge handle
- ✅ **Exit** - Closes application completely
- ✅ **Signal Source** - Opens camera selection panel
- ✅ **Annotation** - Launches annotation tool (if installed)
- ✅ **Settings** - (Reserved for future use)

### Test 5: Multi-Monitor Setup (if applicable)

1. Connect multiple monitors
2. Launch FloatingMenu
3. Verify:
   - ✅ Menu docks to primary monitor
   - ✅ Camera window uses primary monitor resolution
   - ✅ Menu doesn't disappear on secondary monitor

### Test 6: Camera Switching

1. Connect **multiple cameras**
2. Select **first camera** → verify preview works
3. Close camera window
4. Select **second camera** → verify preview works
5. Verify:
   - ✅ Only one camera active at a time
   - ✅ Previous camera released properly
   - ✅ No resource leaks

## 🎯 Startup Configuration

### Option 1: Desktop Shortcut

Create a shortcut for manual startup:

1. **Right-click Desktop** → New → Shortcut
2. **Location:** 
	```
	C:\Program Files\FloatingMenu\FloatingMenu.exe
	```
3. **Name:** `Floating Menu`
4. **Optional: Change icon**
	- Right-click shortcut → Properties → Change Icon
	- Browse to FloatingMenu.exe (if it has embedded icon)

### Option 2: Windows Startup Folder

Auto-start FloatingMenu when Windows starts:

1. Press `Win + R`
2. Type `shell:startup` and press Enter
3. Create shortcut to FloatingMenu.exe in this folder

### Option 3: Batch Script Launcher

Create a batch file for easy launching with custom settings:

```cmd
@echo off
REM ============================================================================
REM Floating Menu Application Launcher
REM Copyright (C) 2026 Intel Corporation
REM ============================================================================

echo.
echo Starting Floating Menu Application...
echo.

REM Navigate to installation directory
cd /d "C:\Program Files\FloatingMenu"
if errorlevel 1 (
    echo ERROR: Could not find FloatingMenu directory
    pause
    exit /b 1
)

REM ============================================================================
REM Pre-Flight Checks
REM ============================================================================
echo Checking camera availability...
pnputil /enum-devices /class Camera /connected >nul 2>&1
if errorlevel 1 (
    echo WARNING: No cameras detected
    echo Please connect a camera and restart
    timeout /t 5
)

echo Checking .NET Runtime...
dotnet --list-runtimes | findstr "Microsoft.WindowsDesktop.App 10." >nul 2>&1
if errorlevel 1 (
    echo WARNING: .NET 10 Desktop Runtime not found
    echo Application may fail to start
    timeout /t 5
)

REM ============================================================================
REM Launch Application
REM ============================================================================
echo.
echo Launching FloatingMenu...
echo.

start "" "FloatingMenu.exe"

REM Wait a moment for startup
timeout /t 2 >nul

REM Check if process started
tasklist | findstr "FloatingMenu.exe" >nul 2>&1
if errorlevel 1 (
    echo ERROR: FloatingMenu failed to start
    echo Check the Event Viewer for details
    pause
    exit /b 1
)

echo.
echo FloatingMenu started successfully!
echo Check the right edge of your screen for the menu handle.
echo.
exit /b 0
```

Save as `LaunchFloatingMenu.bat` in the installation directory.

## 🎨 Screen Annotation Integration

### Installing ScreenPaint (Recommended Annotation Tool)

**Method 1: Microsoft Store**

1. Open **Microsoft Store**
2. Search for **"ScreenPaint"** by Hanakian Software
3. Click **Get** or **Install**
4. Wait for installation to complete

**Method 2: PowerShell**

```powershell
# Install via winget (if available)
winget install "ScreenPaint"
```

### Configuring Annotation Tool Path

The default path in the application is:
```
C:\Program Files\WindowsApps\19566Hanakiansoftware.ScreenPaint_1.3.3.0_x64__y1w6xw98tx1ba\DesktopDrawing\ScreenPaint.exe
```

**To find your actual ScreenPaint path:**

```powershell
# Find ScreenPaint installation
Get-ChildItem "C:\Program Files\WindowsApps" -Filter "*.ScreenPaint*" -Recurse -Directory

# Or search for the executable
Get-ChildItem "C:\Program Files\WindowsApps" -Filter "ScreenPaint.exe" -Recurse
```

**To update the path in the application:**

If you need to use a different annotation tool or ScreenPaint is at a different location, you'll need to rebuild the application with the updated path in `MainWindow.xaml.cs`.

### Using Alternative Annotation Tools

To integrate a different annotation tool:

1. Note the full path to the tool's executable
2. Update the path in `MainWindow.xaml.cs` (line with `exePath` variable)
3. Rebuild and republish the application

Example for Epic Pen:
```csharp
string exePath = @"C:\Program Files (x86)\Epic Pen\EpicPen.exe";
```

### Testing Annotation Integration

1. Launch FloatingMenu
2. Expand menu
3. Click **Annotation** menu item
4. Verify:
   - ✅ ScreenPaint (or other tool) launches
   - ✅ FloatingMenu collapses automatically
   - ✅ You can annotate the screen
5. Close annotation tool
6. Verify:
   - ✅ FloatingMenu clears selection

## 🔄 Updates and Maintenance

### Updating to a New Version

**Step 1: Stop any running instances**

```powershell
Stop-Process -Name FloatingMenu -Force -ErrorAction SilentlyContinue
```

**Step 2: Backup current version**

```powershell
$installPath = "C:\Program Files\FloatingMenu"
$backupPath = "C:\Program Files\FloatingMenu.backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $installPath -Destination $backupPath -Recurse
```

**Step 3: Copy new version**

```powershell
Copy-Item -Path "C:\Temp\FloatingMenu_NewVersion\*" -Destination $installPath -Recurse -Force
```

**Step 4: Verify new version**

```powershell
cd $installPath
.\FloatingMenu.exe
```

### Updating OpenCvSharp Dependencies

If updating OpenCvSharp versions:

1. **Update NuGet packages** in development environment:
   ```
   dotnet add package OpenCvSharp4
   dotnet add package OpenCvSharp4.WpfExtensions
   ```

2. **Rebuild and republish**:
   ```
   dotnet publish -c Release -r win-x64 --self-contained true
   ```

3. **Test camera functionality** thoroughly before deploying

### Checking for Camera Driver Updates

```powershell
# Update all drivers via Windows Update
Get-WindowsUpdate -AcceptAll -Install

# Or check Device Manager for camera driver updates
```

## 🗑️ Uninstalling

### Step 1: Stop running instances

```powershell
Stop-Process -Name FloatingMenu -Force -ErrorAction SilentlyContinue
```

### Step 2: Remove startup entries (if configured)

**Remove from Startup folder:**

```powershell
Remove-Item "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\FloatingMenu.lnk" -Force -ErrorAction SilentlyContinue
```

### Step 3: Remove desktop shortcuts

```powershell
Remove-Item "$env:USERPROFILE\Desktop\Floating Menu.lnk" -Force -ErrorAction SilentlyContinue
```

### Step 4: Remove application directory

```powershell
Remove-Item -Path "C:\Program Files\FloatingMenu" -Recurse -Force
```

### Step 5: Optional - Remove .NET Runtime (if not used by other apps)

```powershell
# Only if FloatingMenu was the only .NET 10 application
winget uninstall Microsoft.DotNet.DesktopRuntime.10
```

## 📋 Deployment Checklist

Use this checklist for each deployment:

### Pre-Deployment
- [ ] .NET 10 SDK installed on build machine
- [ ] Source code up to date from repository
- [ ] All changes committed and pushed
- [ ] OpenCvSharp NuGet packages restored
- [ ] Version number updated (if applicable)
- [ ] Release notes prepared

### Build Process
- [ ] Dependencies restored successfully
- [ ] Release build completed without errors
- [ ] Publish command executed for target platform
- [ ] Output files verified in publish directory
- [ ] OpenCvSharp native libraries included
- [ ] Deployment package created (folder or ZIP)

### Target Machine Setup
- [ ] Windows 11 verified
- [ ] .NET 10 Desktop Runtime installed (if using FDD)
- [ ] Camera device connected and working
- [ ] Camera drivers installed and up to date
- [ ] Windows Camera privacy settings configured
- [ ] Display resolution noted (for testing)
- [ ] ScreenPaint or annotation tool installed (optional)

### Installation
- [ ] Application files copied to installation directory
- [ ] Permissions configured (if needed)
- [ ] Desktop shortcut created (if needed)
- [ ] Startup configuration set (if needed)
- [ ] Annotation tool path verified (if using)

### Testing
- [ ] Application launches successfully
- [ ] Edge handle visible on right edge
- [ ] Menu expands/collapses correctly
- [ ] Handle is draggable vertically
- [ ] Signal Source detects connected cameras
- [ ] Camera preview opens and displays feed
- [ ] Camera resolution matches screen
- [ ] Frame rate is smooth (~60 FPS)
- [ ] Camera releases when window closes
- [ ] Multiple cameras can be switched
- [ ] Annotation tool launches (if installed)
- [ ] Menu collapses when annotation launches
- [ ] Exit button closes application cleanly
- [ ] No errors in Event Viewer

## 🆘 Troubleshooting Deployment

### 1. Build Fails

**Issue**: Build errors during compilation

**Solutions**:

1. Clean and rebuild:
   ```
   dotnet clean
   dotnet restore --force
   dotnet build -c Release
   ```

2. Check for missing packages:
   ```
   dotnet list package
   ```

3. Verify .NET 10 SDK installed:
   ```
   dotnet --list-sdks
   ```

### 2. Publish Fails

**Issue**: Publish command fails or times out

**Solutions**:

1. Clear publish directory:
   ```powershell
   Remove-Item -Path "bin\Release\net10.0-windows\win-x64\publish" -Recurse -Force
   ```

2. Retry publish:
   ```
   dotnet publish -c Release -r win-x64 --self-contained true
   ```

3. Check disk space and permissions

4. Try without single-file option first

### 3. Access Denied / Permission Errors

**Issue**: Application can't access camera or files

**Solutions**:

1. Check Windows Camera privacy settings:
   - Settings → Privacy & Security → Camera
   - Enable desktop app access

2. Run as Administrator (if needed):
   ```powershell
   Start-Process FloatingMenu.exe -Verb RunAs
   ```

3. Check folder permissions:
   ```powershell
   Get-Acl "C:\Program Files\FloatingMenu" | Format-List
   ```

## 📚 Additional Resources

- **.NET Publishing Guide**: [Microsoft Docs](https://learn.microsoft.com/dotnet/core/deploying/)
- **WPF Deployment**: [Microsoft Docs](https://learn.microsoft.com/dotnet/desktop/wpf/app-development/deploying-a-wpf-application-wpf)
- **Single File Deployment**: [Microsoft Docs](https://learn.microsoft.com/dotnet/core/deploying/single-file)
- **OpenCvSharp Documentation**: [GitHub](https://github.com/shimat/opencvsharp)
- **DirectShow Documentation**: [Microsoft Docs](https://learn.microsoft.com/windows/win32/directshow/directshow)
- **Windows Camera Privacy**: [Microsoft Support](https://support.microsoft.com/windows/camera-privacy-settings)
- **Task Scheduler**: [Windows Documentation](https://learn.microsoft.com/windows-server/administration/windows-commands/schtasks)

---

**Document Version**: 1.0  
**Last Updated**: March 2026  
**Maintained By**: Intel Corporation

**Copyright (C) 2026 Intel Corporation**  
**SPDX-License-Identifier: Apache-2.0**

