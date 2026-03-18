/****************************************************************************** 
* Copyright (C) 2026 Intel Corporation
* SPDX-License-Identifier: Apache-2.0
* 
* This software and the related documents are Intel copyrighted materials,
* and your use of them is governed by the express license under which they
* were provided to you ("License"). Unless the License provides otherwise,
* you may not use, modify, copy, publish, distribute, disclose or transmit
* this software or the related documents without Intel's prior written
* permission.
* 
* This software and the related documents are provided as is, with no
* express or implied warranties, other than those that are expressly stated
* in the License.
*******************************************************************************/

#include "hid_touch.h"
#include "tusb.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include <string.h>
#include <stdbool.h>

#define REPORT_ID_TOUCH         1
#define REPORT_ID_MAX_COUNT     2
#define ITF_NUM_HID             0
#define EPNUM_HID               0x81
#define HID_POLL_INTERVAL_MS    1
#define MAX_CONTACTS            TOUCH_MAX_CONTACTS   // 10
#define CONFIG_TOTAL_LEN        (TUD_CONFIG_DESC_LEN + TUD_HID_DESC_LEN)

// =================== REPORT DESCRIPTOR ===================
// Parallel 10-finger format: all contacts in one report.
// Each finger = 15 bytes: flags(1) + cid(1) + x(2) + y(2) + pressure(1) +
//                          width(1) + height(1) + azimuth(2) + altitude(2) + twist(2)
// 10 × 15 + 1 (contact count) = 151 bytes payload → 152 B with report-ID.
// Sent as 3 × 64-byte USB FS packets per 1ms poll interval.
//
// HID global state persists across collection boundaries. After each finger's
// Azimuth/Altitude/Twist block the active Usage Page is Digitizers so no page
// restoration prefix is needed between fingers.

// Inner finger body (89 descriptor bytes, 15 report bytes per finger).
// After this block, active page = Digitizers — identical state for every slot.
#define FINGER_INNER_BODY \
    /* Flags: TipSwitch(b0)+InRange(b1)+Confidence(b2)+Pad(5b) = 1 byte */ \
    0x09, 0x42,                                                  \
    0x09, 0x32,                                                  \
    0x09, 0x47,                                                  \
    0x15, 0x00, 0x25, 0x01,                                      \
    0x75, 0x01, 0x95, 0x03, 0x81, 0x02,                          \
    0x75, 0x05, 0x95, 0x01, 0x81, 0x03,                          \
    /* Contact ID (1 byte) */                                     \
    0x09, 0x51,                                                  \
    0x15, 0x00, 0x25, 0x7F,                                      \
    0x75, 0x08, 0x95, 0x01, 0x81, 0x02,                          \
    /* X (2 bytes) */                                             \
    0x05, 0x01,                                                  \
    0x09, 0x30,                                                  \
    0x16, 0x00, 0x00, 0x26, 0xFF, 0x7F,                          \
    0x75, 0x10, 0x95, 0x01, 0x81, 0x02,                          \
    /* Y (2 bytes) — inherits all globals from X */               \
    0x09, 0x31, 0x81, 0x02,                                      \
    /* Tip Pressure + Width + Height (Digitizers, 8-bit) */       \
    0x05, 0x0D,                                                  \
    0x09, 0x30, 0x09, 0x48, 0x09, 0x49,                          \
    0x15, 0x00, 0x25, 0xFF,                                      \
    0x75, 0x08, 0x95, 0x03, 0x81, 0x02,                          \
    /* Azimuth + Altitude + Twist (Digitizers, 16-bit, 0-360) */  \
    0x09, 0x3F, 0x09, 0x40, 0x09, 0x41,                          \
    0x15, 0x00, 0x26, 0x68, 0x01,                                \
    0x75, 0x10, 0x95, 0x03, 0x81, 0x02

// All 10 fingers share the same collection — no per-finger page-restore prefix.
#define FINGER_COLLECTION \
    0x09, 0x22, 0xA1, 0x02, \
    FINGER_INNER_BODY,       \
    0xC0

const uint8_t hid_report_descriptor[] = {
    0x05, 0x0D,                     // Usage Page (Digitizers)
    0x09, 0x04,                     // Usage (Touch Screen)
    0xA1, 0x01,                     // Collection (Application)

      0x85, REPORT_ID_TOUCH,        //   Report ID (1)

      FINGER_COLLECTION,            //   Finger 1
      FINGER_COLLECTION,            //   Finger 2
      FINGER_COLLECTION,            //   Finger 3
      FINGER_COLLECTION,            //   Finger 4
      FINGER_COLLECTION,            //   Finger 5
      FINGER_COLLECTION,            //   Finger 6
      FINGER_COLLECTION,            //   Finger 7
      FINGER_COLLECTION,            //   Finger 8
      FINGER_COLLECTION,            //   Finger 9
      FINGER_COLLECTION,            //   Finger 10

      // Contact Count — Digitizers page already active after last finger
      0x09, 0x54,                   //   Usage (Contact Count)
      0x15, 0x00,                   //   Logical Min (0)
      0x25, MAX_CONTACTS,           //   Logical Max (10)
      0x75, 0x08,                   //   Report Size (8)
      0x95, 0x01,                   //   Report Count (1)
      0x81, 0x02,                   //   Input (Data, Var, Abs)

      // Feature Report: Contact Count Maximum (ID 2)
      0x85, REPORT_ID_MAX_COUNT,    //   Report ID (2)
      0x09, 0x55,                   //   Usage (Contact Count Maximum)
      0x15, 0x00,                   //   Logical Min (0)
      0x25, MAX_CONTACTS,           //   Logical Max (10)
      0x75, 0x08,                   //   Report Size (8)
      0x95, 0x01,                   //   Report Count (1)
      0xB1, 0x02,                   //   Feature (Data, Var, Abs)

    0xC0                            // End Collection (Application)
};

// =================== CONFIGURATION DESCRIPTOR ===================

static uint8_t const hid_fs_configuration_descriptor[] = {
    TUD_CONFIG_DESCRIPTOR(1, 1, 0, CONFIG_TOTAL_LEN, 0x00, 100),
    TUD_HID_DESCRIPTOR(ITF_NUM_HID, 0, HID_ITF_PROTOCOL_NONE,
                       sizeof(hid_report_descriptor), EPNUM_HID,
                       64, HID_POLL_INTERVAL_MS)  // wMaxPacketSize=64 (FS max); report >64B sent multi-packet
};

const uint8_t *hid_touch_configuration_descriptor_fs(void)
{
    return hid_fs_configuration_descriptor;
}

// =================== TINYUSB CALLBACKS ===================

uint8_t const *tud_hid_descriptor_report_cb(uint8_t instance)
{
    (void)instance;
    return hid_report_descriptor;
}

uint16_t tud_hid_get_report_cb(uint8_t instance, uint8_t report_id,
                                hid_report_type_t report_type,
                                uint8_t *buffer, uint16_t reqlen)
{
    // Respond to host request for Contact Count Maximum feature report
    if (report_type == HID_REPORT_TYPE_FEATURE &&
        report_id == REPORT_ID_MAX_COUNT)
    {
        buffer[0] = MAX_CONTACTS;
        return 1;
    }
    return 0;
}

void tud_hid_set_report_cb(uint8_t instance, uint8_t report_id,
                            hid_report_type_t report_type,
                            uint8_t const *buffer, uint16_t bufsize)
{
    (void)instance;
    (void)report_id;
    (void)report_type;
    (void)buffer;
    (void)bufsize;
}

// =================== REPORT STRUCT ===================
// 10 × 15 bytes/finger + 1 (contact_count) = 151 bytes payload.
// With report-ID prefix = 152 bytes; sent as 3 USB FS packets per poll.
typedef struct {
    uint8_t  flags;       // bit0=TipSwitch, bit1=InRange, bit2=Confidence
    uint8_t  contact_id;
    uint16_t x;
    uint16_t y;
    uint8_t  pressure;
    uint8_t  width;
    uint8_t  height;
    uint16_t azimuth;
    uint16_t altitude;
    uint16_t twist;
} __attribute__((packed)) finger_report_t;

typedef struct {
    finger_report_t contacts[MAX_CONTACTS];   // 150 bytes (10 × 15)
    uint8_t         contact_count;            //   1 byte
} __attribute__((packed)) touch_report_t;

// =================== CONTACT STATE ===================

typedef struct {
    uint16_t x;
    uint16_t y;
    uint8_t  contact_id;
    uint8_t  tip_switch;
    uint8_t  in_range;
    uint8_t  confidence;
    uint8_t  pressure;
    uint8_t  width;
    uint8_t  height;
    uint16_t azimuth;
    uint16_t altitude;
    uint16_t twist;
    bool     in_use;
} touch_slot_t;

static touch_slot_t g_slots[MAX_CONTACTS];

// =================== PUBLIC API ===================

void hid_touch_update_contact(uint16_t x, uint16_t y,
                               uint8_t cid, uint8_t tip_switch,
                               uint8_t in_range, uint8_t confidence,
                               uint8_t pressure,
                               uint8_t width, uint8_t height,
                               uint16_t azimuth, uint16_t altitude, uint16_t twist)
{
    for (int i = 0; i < MAX_CONTACTS; i++) {
        if (g_slots[i].in_use && g_slots[i].contact_id == cid) {
            g_slots[i].x          = x;
            g_slots[i].y          = y;
            g_slots[i].tip_switch = tip_switch;
            g_slots[i].in_range   = in_range;
            g_slots[i].confidence = confidence;
            g_slots[i].pressure   = pressure;
            g_slots[i].width      = width;
            g_slots[i].height     = height;
            g_slots[i].azimuth    = azimuth;
            g_slots[i].altitude   = altitude;
            g_slots[i].twist      = twist;
            return;
        }
    }
    if (tip_switch) {
        for (int i = 0; i < MAX_CONTACTS; i++) {
            if (!g_slots[i].in_use) {
                g_slots[i].x          = x;
                g_slots[i].y          = y;
                g_slots[i].contact_id = cid;
                g_slots[i].tip_switch = 1;
                g_slots[i].in_range   = in_range;
                g_slots[i].confidence = confidence;
                g_slots[i].pressure   = pressure;
                g_slots[i].width      = width;
                g_slots[i].height     = height;
                g_slots[i].azimuth    = azimuth;
                g_slots[i].altitude   = altitude;
                g_slots[i].twist      = twist;
                g_slots[i].in_use     = true;
                return;
            }
        }
    }
}

void hid_touch_flush(void)
{
    if (!tud_hid_ready()) return;  // USB busy — slots stay updated, next event will send

    touch_report_t report;
    memset(&report, 0, sizeof(report));

    uint8_t active = 0;
    for (int i = 0; i < MAX_CONTACTS; i++) {
        bool touching = g_slots[i].in_use && g_slots[i].tip_switch;
        uint8_t tip = g_slots[i].tip_switch & 0x01;
        uint8_t inr = g_slots[i].in_range   & 0x01;
        // Per HID Digitizer spec: Confidence MUST be 1 when TipSwitch=1.
        // If Confidence=0 with TipSwitch=1, Windows drops the touch silently
        // (POINTER_FLAG_CONFIDENCE cleared). Force it to 1 for active contacts.
        uint8_t conf = tip ? 1 : (g_slots[i].confidence & 0x01);
        report.contacts[i].flags = g_slots[i].in_use ? (tip | (inr << 1) | (conf << 2)) : 0x00;
        report.contacts[i].contact_id = g_slots[i].in_use ? g_slots[i].contact_id : (uint8_t)i;
        report.contacts[i].x          = g_slots[i].in_use ? g_slots[i].x        : 0;
        report.contacts[i].y          = g_slots[i].in_use ? g_slots[i].y        : 0;
        report.contacts[i].pressure   = g_slots[i].in_use ? g_slots[i].pressure : 0;
        report.contacts[i].width      = g_slots[i].in_use ? g_slots[i].width    : 0;
        report.contacts[i].height     = g_slots[i].in_use ? g_slots[i].height   : 0;
        report.contacts[i].azimuth    = g_slots[i].in_use ? g_slots[i].azimuth  : 0;
        report.contacts[i].altitude   = g_slots[i].in_use ? g_slots[i].altitude : 0;
        report.contacts[i].twist      = g_slots[i].in_use ? g_slots[i].twist    : 0;
        if (touching) active++;
    }
    report.contact_count = active;

    tud_hid_report(REPORT_ID_TOUCH, &report, sizeof(report));

    // Free lifted contacts after the report is queued
    for (int i = 0; i < MAX_CONTACTS; i++) {
        if (g_slots[i].in_use && !g_slots[i].tip_switch)
            memset(&g_slots[i], 0, sizeof(g_slots[i]));
    }
}