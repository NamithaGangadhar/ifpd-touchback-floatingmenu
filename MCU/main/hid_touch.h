/****************************************************************************** 
* Copyright (C) 2026 Intel Corporation
* SPDX-License-Identifier: Apache-2.0
*******************************************************************************/

#pragma once
#include <stdint.h>

#define TOUCH_MAX_CONTACTS 10

const uint8_t *hid_touch_configuration_descriptor_fs(void);

// Stage one contact for the current frame. Call once per active finger.
void hid_touch_update_contact(uint16_t x, uint16_t y,
                               uint8_t contact_id, uint8_t tip_switch,
                               uint8_t in_range, uint8_t confidence,
                               uint8_t pressure,
                               uint8_t width, uint8_t height,
                               uint16_t azimuth, uint16_t altitude, uint16_t twist);

// Send one HID report with all staged contacts. Call after all fingers updated.
void hid_touch_flush(void);