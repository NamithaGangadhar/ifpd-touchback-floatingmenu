# Touch Data Capture Service - Deployment Guide

This guide covers building, publishing, and deploying the Touch Data Capture Service for production use.

## 📋 Table of Contents

- [Prerequisites](#prerequisites)
- [Building the Application](#building-the-application)
- [Publishing Options](#publishing-options)
- [Manual Deployment](#manual-deployment)
- [Testing the Deployment](#testing-the-deployment)
- [Startup Configuration](#startup-configuration)
- [Updates and Maintenance](#updates-and-maintenance)
- [Uninstalling](#uninstalling)

## ✅ Prerequisites

### Development Machine

- **Windows 10/11** with latest updates
- **.NET 10 SDK** installed
- **Git** for source control
- **Visual Studio 2022** or **VS Code** (optional)

### Target Machine

- **Windows 10** (build 19041+) or **Windows 11**
- **.NET 10 Runtime** (for framework-dependent deployment)
- **Administrator privileges** for installation
- **HID-compliant touch screen** available 
- **Serial port** available (physical or virtual)

---

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

### 3. Build Release Version

 - Build in Release configuration
	```
	dotnet build -c Release
	```
 - Build output location: `bin\Release\net10.0\`

### 4. Verify Build

 - Navigate to output directory
	```
	cd bin\Release\net10.0
	```

 - List files
	```
	dir
	```

 - Expected files:
	- TouchDataCaptureService.exe
	- TouchDataCaptureService.dll
	- TouchDataCaptureService.runtimeconfig.json
	- TouchDataCaptureService.deps.json
	
## 📦 Publishing Options


### Option 1: Framework-Dependent Deployment (FDD)

**Smaller size, requires .NET runtime on target machine**

Publish for any Windows x64 machine
```
dotnet publish -c Release -r win-x64 --no-self-contained
```
Output location: `bin\Release\net10.0\win-x64\publish\`

### Option 2: Self-Contained Deployment (SCD)

**Larger size, no runtime required on target machine**

Publish with bundled runtime
```
dotnet publish -c Release -r win-x64 --self-contained true
```

Output location: `bin\Release\net10.0\win-x64\publish\`

### Option 3: Single File Executable

**All-in-one executable file**

Publish as single executable
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Optional: Enable compression
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

Output: Single TouchDataCaptureService.exe file

### Option 4: Trimmed Deployment

**Reduced size by removing unused code**

Publish with trimming
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishTrimmed=true
```

Single file + trimming
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

## 🚀 Manual Deployment

### Step 1: Prepare Deployment Package

- Navigate to publish directory
	```
	cd bin\Release\net10.0\win-x64\publish
	```

- Create deployment folder
	```
	$deployPath = "C:\Temp\TouchDataCaptureService_Deployment"
	New-Item -Path $deployPath -ItemType Directory -Force
	```

- Copy all files
	```
	Copy-Item -Path * -Destination $deployPath -Recurse
	```

- Optional: Create ZIP for distribution
	```
	Compress-Archive -Path * -DestinationPath "$deployPath..\TouchDataCaptureService.zip"
	```

### Step 2: Transfer to Target Machine

Choose your transfer method:

- **Option A: USB Drive**

	Copy to USB drive
	```
	Copy-Item -Path $deployPath -Destination "E:\TouchDataCaptureService" -Recurse
	```
- **Option B: Email/Cloud**

	Use the ZIP file created earlier
	Upload to OneDrive, Google Drive, or email

### Step 3: Install on Target Machine

* Create installation directory
	```
	$installPath = "C:\Program Files\TouchDataCaptureService" New-Item -Path $installPath -ItemType Directory -Force
	```

* Copy files from deployment package
	```
	Copy-Item -Path "E:\TouchDataCaptureService*" -Destination $installPath -Recurse
	```

* Or extract from ZIP
	```
	Expand-Archive -Path "C:\Downloads\TouchDataCaptureService.zip" -DestinationPath $installPath
	```

### Step 4: Install .NET Runtime (if using FDD)

If you published as framework-dependent, install .NET 10 runtime:

* Option 1: Direct download
	```
	Start-Process "https://dotnet.microsoft.com/download/dotnet/10.0"
	```

* Option 2: Using winget
	```
	winget install Microsoft.DotNet.Runtime.10
	```

* Verify installation
	```
	dotnet --list-runtimes
	```

### Step 5: Verify Installation

Navigate to install directory
```
cd "C:\Program Files\TouchDataCaptureService"
```

Test run (show help)
```
.\TouchDataCaptureService.exe --help
```

If successful, you should see the help message

## ✅ Testing the Deployment

### Test 1: Basic Functionality

Run with default settings
```
.\TouchDataCaptureService.exe
```

Expected output:
- No errors
- Application is visible as a background process in Task Manager

### Test 2: Serial Communication

Test with specific COM port
```
.\TouchDataCaptureService.exe --port COM3 --baudrate 115200 --seriallog
```

Verify:
- Serial port opens successfully
- Touch data appears in serial_data.log

### Test 3: Touch Input

1. **Touch the screen** while application is running
2. **Check log files** are being created:

	* Check log files
		```
		Get-ChildItem "C:\Program Files\TouchDataCaptureService" *.log
		```
	* View recent entries
		```
		Get-Content "C:\Program Files\TouchDataCaptureService\hid_decoded.log" -Tail 10
		```
3. **Verify data** is being captured and logged

### Test 4: Process Bypass (Optional)

Test bypass feature
```
.\TouchDataCaptureService.exe --enablebypass
```

Verify:
- Touch events from bypassed apps are skipped
- Other touch events still captured

## 🎯 Startup Configuration

### Option 1: Desktop Shortcut

Create a shortcut for manual startup:

1. **Right-click Desktop** → New → Shortcut
2. **Location:** 
   `C:\Program Files\TouchDataCaptureService\TouchDataCaptureService.exe`
3. **Add arguments** (optional):
   `C:\Program Files\TouchDataCaptureService\TouchDataCaptureService.exe --port COM3`
4. **Name:** `Touch Data Capture Service`
5. **Right-click shortcut** → Properties → Advanced
6. **Check** "Run as administrator"
7. **Change icon** (optional)

### Option 2: Batch Script

Create a batch file for easy launching:

```batch
@echo off
REM ============================================================================
REM Touch Data Capture Service Launcher
REM Copyright (C) 2026 Intel Corporation
REM ============================================================================

echo.
echo Starting Touch Data Capture Service...
echo.

REM Navigate to service installation directory
cd /d "C:\Program Files\TouchDataCaptureService"
if errorlevel 1 (
    echo ERROR: Could not find service directory
    pause
    exit /b 1
)

REM ============================================================================
REM Configuration Settings (with defaults)
REM ============================================================================
set COM_PORT=%~1
set BAUD_RATE=%~2
set ENABLE_BYPASS=%~3
set ENABLE_SERIAL_LOG=%~4

REM Apply defaults if not provided
if "%COM_PORT%"=="" set COM_PORT=COM10
if "%BAUD_RATE%"=="" set BAUD_RATE=3000000

REM ============================================================================
REM Build Command Line
REM ============================================================================
set CMD=TouchDataCaptureService.exe --port %COM_PORT% --baudrate %BAUD_RATE%

if /i "%ENABLE_BYPASS%"=="true" (
    set CMD=%CMD% --enablebypass
    echo Bypass mode: ENABLED
) else (
    echo Bypass mode: DISABLED
)

if /i "%ENABLE_SERIAL_LOG%"=="true" (
    set CMD=%CMD% --seriallog
    echo Serial logging: ENABLED
) else (
    echo Serial logging: DISABLED
)

REM ============================================================================
REM Execute Service
REM ============================================================================
echo.
echo Configuration:
echo   COM Port: %COM_PORT%
echo   Baud Rate: %BAUD_RATE%
echo.
echo Running: %CMD%
echo.

%CMD%

REM ============================================================================
REM Error Handling
REM ============================================================================
if errorlevel 1 (
    echo.
    echo ERROR: Service exited with error code %errorlevel%
    echo Press any key to exit...
    pause >nul
    exit /b %errorlevel%
)

echo.
echo Service stopped successfully.
exit /b 0
```
Save as `StartTouchService.bat` in the installation directory.

**Usage Example**

1. Using default values:
   ```
   StartTouchService.bat
   ```
2. Custom COM port:
   ```
   StartTouchService.bat COM3
   ```
3. Custom COM port and baud rate:
   ```
   StartTouchService.bat COM12 115200
   ```
4. Enable bypass:
   ```
   StartTouchService.bat COM3 115200 true
   ```
5. Enable bypass and serial logging:
   ```
   StartTouchService.bat COM3 115200 true true
   ```

## 🔄 Updates and Maintenance

### Updating to a New Version

- Stop any running instances
	```
	Stop-Process -Name TouchDataCaptureService -Force -ErrorAction SilentlyContinue
	```

- Backup current version
	```
	$installPath = "C:\Program Files\TouchDataCaptureService" 
	$backupPath = "C:\Program Files\TouchDataCaptureService.backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')" 
	Copy-Item -Path $installPath -Destination $backupPath -Recurse
	```

- Copy new version
	```
	Copy-Item -Path "C:\Temp\TouchDataCaptureService_NewVersion*" -Destination $installPath -Recurse -Force
	```

- Verify new version
	```
	cd $installPath .\TouchDataCaptureService.exe --help
	```

## 🗑️ Uninstalling

- Stop running instances
	```
	Stop-Process -Name TouchDataCaptureService -Force -ErrorAction SilentlyContinue
	```

- Remove desktop shortcut (if exists)
	```
	Remove-Item "$env:USERPROFILE\Desktop\Touch Data Capture Service.lnk" -Force -ErrorAction SilentlyContinue
	```

- Remove application directory
	```
	Remove-Item -Path "C:\Program Files\TouchDataCaptureService" -Recurse -Force -ErrorAction SilentlyContinue
	```

- Remove application
	```
	Remove-Item -Path "C:\Program Files\TouchDataCaptureService" -Recurse -Force
	```

## 📋 Deployment Checklist

Use this checklist for each deployment:

### Pre-Deployment
- [ ] .NET 10 SDK installed on build machine
- [ ] Source code up to date from repository
- [ ] All changes committed and pushed
- [ ] Version number updated (if applicable)
- [ ] Release notes prepared

### Build Process
- [ ] Dependencies restored successfully
- [ ] Release build completed without errors
- [ ] Publish command executed for target platform
- [ ] Output files verified in publish directory
- [ ] Deployment package created (folder or ZIP)

### Target Machine Setup
- [ ] Windows 10/11 verified
- [ ] .NET 10 runtime installed (if using FDD)
- [ ] Touch screen device working
- [ ] Serial port identified and available
- [ ] Serial drivers installed
- [ ] Administrator access available

### Installation
- [ ] Application files copied to installation directory
- [ ] Log directory created
- [ ] Permissions configured
- [ ] Desktop shortcut created (if needed)
- [ ] Startup configuration set (if needed)

### Testing
- [ ] Application launches successfully
- [ ] Help command displays correctly
- [ ] Serial port opens successfully
- [ ] Touch events captured in logs
- [ ] Data transmitted via serial
- [ ] Process bypass working (if enabled)
- [ ] Coordinate scaling functioning
- [ ] No errors in log files

## 🆘 Troubleshooting Deployment

### Build Fails
1. Clean and rebuild
```
dotnet clean 
dotnet restore --force 
dotnet build -c Release
```
2. Check for errors
3. Review build output for specific errors

### Publish Fails
1. Clear publish directory
	```
	Remove-Item -Path "bin\Release\net10.0\win-x64\publish" -Recurse -Force
	```
2. Retry publish
	```
	dotnet publish -c Release -r win-x64 --self-contained true
	```
3. If still fails, check disk space and permissions

### Application Won't Start on Target
1. Check .NET runtime (for FDD)
	```
	dotnet --list-runtimes
	```

2. If missing, install .NET 10 runtime
	```
	winget install Microsoft.DotNet.Runtime.10
	```

3. For self-contained, check Visual C++ Redistributable

   Download from: https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist

### Access Denied Errors

1. Run as Administrator
	```
	Start-Process TouchDataCaptureService.exe -Verb RunAs
	```
2. Or set UAC permissions on shortcut  `Right-click → Properties → Advanced → Run as administrator`

## 📚 Additional Resources

- **.NET Publishing Guide**: [Microsoft Docs](https://learn.microsoft.com/dotnet/core/deploying/)
- **Single File Deployment**: [Microsoft Docs](https://learn.microsoft.com/dotnet/core/deploying/single-file)
- **Task Scheduler**: [Windows Documentation](https://learn.microsoft.com/windows-server/administration/windows-commands/schtasks)
- **PowerShell Scripting**: [Microsoft Docs](https://learn.microsoft.com/powershell/)

---

**Document Version**: 1.0  
**Last Updated**: March 2026  
**Maintained By**: Intel Corporation

**Copyright (C) 2026 Intel Corporation**  
**SPDX-License-Identifier: Apache-2.0**