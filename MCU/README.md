# MCU SW

This software runs on an MCU which receives touch data from IFPD and projects as USB HID touch screen (Digitizer).

## Requirements

- ESP-IDF v5.x or later
- VSCode with ESP-IDF extension
- ESP32-S3 development board

## Opening the Project in VSCode

1. **Install ESP-IDF Extension**
   - Open VSCode
   - Go to Extensions (Ctrl+Shift+X)
   - Search for "ESP-IDF" and install the official extension by Espressif

2. **Open the Project**
   - File → Open Folder
   - Navigate to and select the `MCU` folder
   - VSCode will recognize it as an ESP-IDF project

3. **Configure ESP-IDF Extension**
   - Press `F1` or `Ctrl+Shift+P` to open the command palette
   - Run: `ESP-IDF: Configure ESP-IDF Extension`
   - Select your ESP-IDF installation path

## Configuration

### Initial Configuration

1. **Open ESP-IDF Configuration UI**
   - Press `F1` or `Ctrl+Shift+P`
   - Run: `ESP-IDF: SDK Configuration Editor (Menuconfig)`
   
2. **Configure Required Settings**

   Navigate through the menuconfig UI to set these options:
   
   **TinyUSB HID Configuration:**
   - Component config → TinyUSB Stack → Human Interface Device Class (HID)
   - Set `CONFIG_TINYUSB_HID_COUNT` = `1`
   
   **Serial Monitor Baud Rate:**
   - Serial flasher config
   - Set `CONFIG_ESPTOOLPY_MONITOR_BAUD` = `2000000`
   
3. **Save Configuration**
   - Press `S` to save
   - Press `Q` to quit menuconfig

### Alternative: Manual Configuration

You can also edit `sdkconfig` directly and add:
- CONFIG_TINYUSB_HID_COUNT=1
- CONFIG_ESPTOOLPY_MONITOR_BAUD=2000000

## Building the Project

1. **Full Build**
   - Press `F1` or `Ctrl+Shift+P`
   - Run: `ESP-IDF: Build your Project`
   - Or use the build button in the bottom status bar
   - Or press `Ctrl+E` then `B`

2. **Clean Build** (if needed)
   - Press `F1` or `Ctrl+Shift+P`
   - Run: `ESP-IDF: Full Clean`
   - Then rebuild the project

### Generated Files

During build time, the following files are automatically generated in the `build/` directory:
- `build/config/sdkconfig.h` - C header with configuration defines
- `build/config/sdkconfig.cmake` - CMake configuration
- `build/compile_commands.json` - Compilation database for IntelliSense
- `build/project_description.json` - Project metadata
- Bootloader binaries in `build/bootloader/`

These files should NOT be manually edited as they are regenerated on each build.

## Flashing and Monitoring

1. **Flash to Device**
   - Connect your ESP32-S3 board via USB
   - Press `F1` or `Ctrl+Shift+P`
   - Run: `ESP-IDF: Flash your Project`
   - Or press `Ctrl+E` then `F`

2. **Monitor Output**
   - Press `F1` or `Ctrl+Shift+P`
   - Run: `ESP-IDF: Monitor Device`
   - Or press `Ctrl+E` then `M`

3. **Flash and Monitor** (combined)
   - Press `F1` or `Ctrl+Shift+P`
   - Run: `ESP-IDF: Build, Flash and Monitor`
   - Or use the flame button in the status bar

## Troubleshooting

- If IntelliSense doesn't work, rebuild the project to regenerate `compile_commands.json`
- If configuration changes don't take effect, do a full clean and rebuild
- Make sure the correct USB port is selected in ESP-IDF extension settings


