# IFPD Touch Back

[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![ESP32-S3](https://img.shields.io/badge/ESP32-S3-E7352C)](https://www.espressif.com/en/products/socs/esp32-s3)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](Apache-2.0.txt)

A complete solution to enable reverse touch / touch-back functionality for Interactive Flat Panel Displays (IFPD) with Intel Architecture (IA) platforms.

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Components](#components)
- [System Requirements](#system-requirements)
- [Quick Start](#quick-start)
- [Documentation](#documentation)
- [Use Cases](#use-cases)
- [License](#license)

## 🔍 Overview

This project enables touch input from an IFPD to be transferred to a connected computer, creating a seamless reverse touch experience. It consists of three main components:

1. **Touch Data Capture Service** (Windows) - Captures touch input from the IFPD
2. **MCU Firmware** (ESP32-S3) - Acts as a bridge, receiving touch data via UART and presenting it as USB HID
3. **Floating Menu Application** (Windows) - Provides a user-friendly interface for camera management and screen annotation

**Note:** All three components are designed to work independently. You can use the Touch Data Capture Service standalone or integrate it with the Floating Menu Application based on your requirements.

## 🏗️ Architecture

<div align="center">
	<img src="images/Architecture.png" alt="Architecture Diagram">
</div>

## 📦 Components

### 1. Touch Data Capture Service (`Windows/GetTouchInfo`)

A Windows service that captures raw HID touch input and forwards it to the ESP32 via serial connection.

**Features:**
- Raw HID touch event capture
- High-speed serial communication (3 Mbaud)
- Dynamic coordinate scaling
- Process filtering
- Comprehensive logging

📖 [Full Documentation](Windows/GetTouchInfo/README.md)

### 2. MCU Firmware (`MCU`)

ESP32-S3 firmware that bridges UART touch data to USB HID multi-touch digitizer.

**Features:**
- 10-finger multi-touch support
- USB HID Digitizer protocol
- Low-latency dual-task architecture
- Windows Precision Touchpad compatible

**Supported Targets:** ESP32-S3

📖 [Full Documentation](MCU/README.md)

### 3. Floating Menu Application (`Windows/FloatingMenu`)

A WPF application providing an edge-docked floating menu for camera management and screen annotation.

**Features:**
- Edge-docked collapsible UI
- Camera detection and preview (Aforge.NET)
- Signal source management
- Screen annotation integration
- Always-on-top interface

📖 [Full Documentation](Windows/FloatingMenu/README.md)

## 💻 System Requirements

### Hardware
- **IFPD Device**: Interactive Flat Panel Display with touch support
- **ESP32-S3**: Development board with USB-OTG support
- **Target Computer**: Any Windows PC
- **Cables**: USB data cables, UART connection (TX/RX)

### Software
- **Windows 11** (for IFPD and Target Computer)
- **.NET 10 SDK** (for Windows applications)
- **ESP-IDF v5.x** (for MCU firmware)
- **Visual Studio 2022 or higher** or **Visual Studio Code** (optional)

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/<your-organization>/ifpd-touchback-floatingmenu.git
cd ifpd-touchback-floatingmenu
```

### 2. Setup MCU Firmware

```bash
cd MCU
# Follow SETUP.md for ESP-IDF configuration
# Follow DEPLOYMENT.md for flashing
```

### 3. Build Windows Applications

```bash
cd Windows/GetTouchInfo
dotnet build
dotnet publish -c Release

cd ../FloatingMenu
dotnet build
dotnet publish -c Release
```

### 4. Deploy and Run

#### Hardware Setup
1. Flash the ESP32-S3 with the MCU firmware
2. Connect ESP32-S3 UART to IFPD's serial port
3. Connect ESP32-S3 USB to Target Computer

#### Running the Application

**Option 1: Touch Back Only (Standalone)**

If you only need touch-back functionality without the floating menu:
- Run the Touch Data Capture Service (TouchDataCaptureService.exe) directly on the IFPD
- Touch events will be forwarded to the Target Computer via ESP32-S3

**Option 2: With Floating Menu Integration**

For full functionality including camera management and screen annotation:
1. Configure the COM port in the Floating Menu configuration file
2. Launch the Floating Menu Application on the IFPD
3. The application will automatically manage the Touch Data Capture Service
4. Click on the signal source button to start touch forwarding
5. Click the source button again to stop the touch service

## 📚 Documentation

Each component has detailed documentation in its respective directory:

- **[MCU README](MCU/README.md)** - ESP32-S3 firmware details
- **[MCU SETUP](MCU/SETUP.md)** - Development environment setup
- **[MCU DEPLOYMENT](MCU/DEPLOYMENT.md)** - Flashing and deployment guide
- **[Touch Service README](Windows/GetTouchInfo/README.md)** - Touch capture service details
- **[Floating Menu README](Windows/FloatingMenu/README.md)** - Floating menu application details

## 🎯 Use Cases

- **Presentation Mode**: Present from IFPD while controlling the connected laptop with touch
- **Remote Control**: Use IFPD as a large touch interface for a connected computer
- **Collaborative Work**: Share touch input across multiple devices
- **Interactive Displays**: Enable touch-back for IA-only IFPD systems

## 📄 License

This project is licensed under the Apache License 2.0. See [Apache-2.0.txt](Apache-2.0.txt) file for details


