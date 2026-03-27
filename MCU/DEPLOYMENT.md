# MCU Software - Deployment Guide

This guide covers flashing the firmware to the ESP32-S3 and monitoring its operation.

## Prerequisites

- ✅ Development environment set up ([see SETUP.md](SETUP.md))
- ✅ Project built successfully
- ✅ ESP32-S3 board available
- ✅ USB cable (data-capable, not charge-only)

## Hardware Setup

### ESP32-S3 USB Connection

The ESP32-S3 has two USB ports:
- **USB-UART port**: Used for flashing and serial monitoring
- **USB-OTG port**: Will act as the HID touch device (after firmware is running)

Connect the **USB-UART port** to your development machine for flashing.

## Flashing Firmware

### 1. Select the Correct COM Port

1. Press `F1` or `Ctrl+Shift+P`
2. Run: `ESP-IDF: Select Port to Use`
3. Choose the COM port corresponding to your ESP32-S3

**Tip**: On Windows, check Device Manager → Ports (COM & LPT) to identify the correct port.

### 2. Flash to Device

Choose one of the following methods:

- **Command Palette**: `F1` → `ESP-IDF: Flash your Project`
- **Keyboard Shortcut**: `Ctrl+E` then `F`
- **Status Bar**: Click the flame icon

Wait for the flashing process to complete (~30 seconds).

### 3. Flash and Monitor (Combined)

For a streamlined workflow:

- **Command Palette**: `F1` → `ESP-IDF: Build, Flash and Monitor`
- **Status Bar**: Click the flame button with monitor icon

This will build, flash, and start monitoring in one command.

## Monitoring Output

### Start Monitor

1. Press `F1` or `Ctrl+Shift+P`
2. Run: `ESP-IDF: Monitor Device`
3. Or press `Ctrl+E` then `M`

### Expected Output

After flashing, you should see output similar to:

```
I (xxx) MAIN: UART initialized on port 0 at 3000000
I (xxx) MAIN: Ready. Waiting for TOUCH commands on UART...
```

### Stop Monitor

Press `Ctrl+]` to exit the monitor.

## Verifying USB HID Functionality

### 1. Connect USB-OTG Port

Once the firmware is running:
1. Keep the USB-UART cable (needed for serial communication between IFPD and ESP32)
2. Connect the **USB-OTG port** to the target computer (Laptop 2)
3. The device should enumerate as a multi-touch HID digitizer

### 2. Check Device Manager (Windows)

1. Open Device Manager
2. Look under **Human Interface Devices**
3. You should see a new HID-compliant touch screen device

### 3. Test Touch Input

- Perform touch events on IFPD or test jig
- Verify touch events are received by the target computer
- Format: `TOUCH,x,y,cid,tip,pressure,inrange,confidence,width,height,azimuth,altitude,twist`

## Troubleshooting

### Flash Failed - No Serial Port

**Symptoms**: Cannot find COM port or device not detected

**Solutions**:
- Check USB cable (use a data cable, not charge-only)
- Install USB-to-UART drivers (CP210x or CH340, depending on your board)
- Try a different USB port
- Put ESP32-S3 in download mode manually:
  1. Hold BOOT button
  2. Press and release RESET button
  3. Release BOOT button
  4. Try flashing again

### Flash Failed - Permission Denied (Linux)

**Solution**: Add user to dialout group:

```bash
sudo usermod -a -G dialout $USER
```
Log out and log back in for changes to take effect.

### Monitor Shows Garbage Characters

**Solutions**:
- Verify baud rate is set to 2000000 in sdkconfig
- Try resetting the board
- Disconnect and reconnect the USB cable

### Device Not Enumerating as HID

**Symptoms**: USB-OTG connection doesn't show HID device

**Solutions**:

- Verify firmware flashed successfully (check serial monitor output)
- Try a different USB cable on the OTG port
- Check target computer USB port is functional
- Review `hid_touch.c` descriptor configuration
- Use `lsusb` (Linux) or Device Manager (Windows) to verify enumeration

### Task Watchdog Triggered

**Symptoms**: `Task watchdog got triggered` errors in monitor

**Solutions**:
- This indicates a task is blocking too long
- Check UART RX buffer isn't overflowing (current: 4096 bytes)
- Verify UART data rate doesn't exceed 3 Mbaud sustained
- Review task priorities and CPU affinity in `app_main.c`

### USB Not Working After Flash

**Solution**: Power cycle the board completely:
- Disconnect both USB cables
- Wait 5 seconds
- Reconnect and try again

## Production Deployment

For production deployment without development tools:

### 1. Extract Binary Files

- Located in build after successful build
- Required files: bootloader, partition table, application binary

### 2. Use esptool.py Directly
```
esptool.py --chip esp32s3 --port COM_PORT write_flash @build/flash_args

```

### 3. Or Use Flash Download Tool

- Download ESP Flash Download Tool from Espressif
- Load binaries at addresses specified in `flasher_args.json`

## Updating Firmware

To update firmware on a deployed device:

1. Connect USB-UART port
2. Follow the standard flashing procedure above
3. No need to erase flash unless doing major version changes

For setup and build instructions, [see SETUP.md](SETUP.md)