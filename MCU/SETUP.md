# MCU Software - Setup & Build Guide

This guide covers setting up the development environment and building the MCU firmware.

## Overview

This software runs on an ESP32-S3 MCU which receives touch data from IFPD over UART and projects it as a USB HID touch screen (Digitizer) to a connected computer.

## Requirements

- ESP-IDF v5.x or later
- VSCode with ESP-IDF extension
- ESP32-S3 development board with USB support
- Windows/Linux/macOS development machine

## Installation

### 1. Install ESP-IDF Extension

1. Open VSCode
2. Go to Extensions (`Ctrl+Shift+X`)
3. Search for "ESP-IDF" 
4. Install the official extension by Espressif
5. Follow the extension's setup wizard to install ESP-IDF toolkit

### 2. Open the Project

1. **File → Open Folder**
2. Navigate to and select the `MCU` folder
3. VSCode will recognize it as an ESP-IDF project

### 3. Configure ESP-IDF Extension

1. Press `F1` or `Ctrl+Shift+P` to open the command palette
2. Run: `ESP-IDF: Configure ESP-IDF Extension`
3. Select your ESP-IDF installation path
4. Select ESP32-S3 as the target device

## Project Configuration

### Option 1: Using Menuconfig UI (Recommended)

1. **Open Configuration UI**
   - Press `F1` or `Ctrl+Shift+P`
   - Run: `ESP-IDF: SDK Configuration Editor (Menuconfig)`

2. **Configure TinyUSB HID**
   - Navigate to: `Component config → TinyUSB Stack → Human Interface Device Class (HID)`
   - Set `CONFIG_TINYUSB_HID_COUNT` = `1`

3. **Configure Serial Monitor**
   - Navigate to: `Serial flasher config`
   - Set `CONFIG_ESPTOOLPY_MONITOR_BAUD` = `2000000`

4. **Save and Exit**
   - Press `S` to save
   - Press `Q` to quit

### Option 2: Manual Configuration

Edit `sdkconfig` directly and add/modify these lines:

```ini
CONFIG_TINYUSB_HID_COUNT=1
CONFIG_ESPTOOLPY_MONITOR_BAUD=2000000
```

## Building

### Full Build
Choose one of the following methods:

- **Command Palette**: `F1` → `ESP-IDF: Build your Project`
- **Status Bar**: Click the build button (spanner icon)
- **Keyboard Shortcut**: `Ctrl+E` then `B`

### Clean Build
If you encounter build issues or after major configuration changes:

1. Press `F1` or `Ctrl+Shift+P`
2. Run: `ESP-IDF: Full Clean`
3. Rebuild the project using the methods above

### Build Output

The build process generates files in the build directory:

```
build/
├── esp32_s3_usb_touch.bin       # Main application binary
├── bootloader/bootloader.bin     # Bootloader binary
├── partition_table/              # Partition table
├── config/
│   ├── sdkconfig.h              # Auto-generated configuration header
│   └── sdkconfig.cmake          # Auto-generated CMake config
├── compile_commands.json        # For IntelliSense
└── project_description.json     # Project metadata
```

⚠️ Do **NOT** manually edit files in build folder - they are regenerated on each build.

## Troubleshooting

### IntelliSense Not Working

- Solution: Rebuild the project to regenerate `compile_commands.json`
- Check that the C/C++ extension is installed
- Verify that `compile_commands.json` exists in the :file_folder: build folder

### Configuration Changes Not Applied
- Solution: Perform a full clean and rebuild
   1. `ESP-IDF: Full Clean`
   2. `ESP-IDF: Build your Project`

### Build Errors After Pulling Updates
 - Try cleaning and rebuilding
 - Verify ESP-IDF version matches project requirements
 - Check if any new dependencies were added in idf_component.yml

### Python/IDF Tool Errors
 - Reinstall ESP-IDF extension
 - Verify ESP-IDF installation path in extension settings
 - Check that Python virtual environment is activated

## Next Steps
Once the build completes successfully, proceed to [DEPLOYMENT.md](DEPLOYMENT.md) for instructions on flashing and running the firmware on your ESP32-S3 device.



