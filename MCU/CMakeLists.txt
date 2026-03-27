# ******************************************************************************
# Copyright (C) 2026 Intel Corporation
# SPDX-License-Identifier: Apache-2.0
# ******************************************************************************

# For more information about build system see
# https://docs.espressif.com/projects/esp-idf/en/latest/api-guides/build-system.html
# The following five lines of boilerplate have to be in your project's
# CMakeLists in this exact order for cmake to work correctly
cmake_minimum_required(VERSION 3.16)

include($ENV{IDF_PATH}/tools/cmake/project.cmake)
# "Trim" the build. Include the minimal set of components, main, and anything it depends on.
idf_build_set_property(MINIMAL_BUILD ON)

# Our HID touch report is 151 bytes payload + 1 report-ID byte = 152 bytes total.
# TinyUSB's default CFG_TUD_HID_EP_BUFSIZE is 64 — tud_hid_report() silently returns
# false for any report larger than 63 bytes. Set globally so hid_device.c (in the
# espressif__tinyusb component) allocates a large enough epin_buf at compile time.
idf_build_set_property(COMPILE_DEFINITIONS "CFG_TUD_HID_EP_BUFSIZE=256" APPEND)

project(esp32_s3_usb_touch)
