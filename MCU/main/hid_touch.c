/****************************************************************************** 
* Copyright (C) 2026 Intel Corporation
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

#define REPORT_ID_TOUCH         1
#define REPORT_ID_MAX_COUNT     2
#define ITF_NUM_HID             0
#define EPNUM_HID               0x81
#define HID_POLL_INTERVAL_MS    1
#define MAX_CONTACTS            1
#define TOUCH_MAX_X             32767
#define TOUCH_MAX_Y             32767
#define CONFIG_TOTAL_LEN        (TUD_CONFIG_DESC_LEN + TUD_HID_DESC_LEN)

// =================== REPORT DESCRIPTOR ===================

const uint8_t hid_report_descriptor[] = {
    // --- Touch Input Report (ID 1) ---
    0x05, 0x0D,                     // Usage Page (Digitizers)
    0x09, 0x04,                     // Usage (Touch Screen)
    0xA1, 0x01,                     // Collection (Application)

      0x85, REPORT_ID_TOUCH,        //   Report ID (1)

      0x09, 0x22,                   //   Usage (Finger)
      0xA1, 0x02,                   //   Collection (Logical)

        // --- Flags byte: TipSwitch(1) + InRange(1) + Confidence(1) + Padding(5) ---
        // Tip Switch (1 bit)
        0x09, 0x42,                 //     Usage (Tip Switch)
        0x15, 0x00,                 //     Logical Min (0)
        0x25, 0x01,                 //     Logical Max (1)
        0x75, 0x01,                 //     Report Size (1)
        0x95, 0x01,                 //     Report Count (1)
        0x81, 0x02,                 //     Input (Data, Var, Abs)

        // In Range (1 bit)
        0x09, 0x32,                 //     Usage (In Range)
        0x15, 0x00,                 //     Logical Min (0)
        0x25, 0x01,                 //     Logical Max (1)
        0x75, 0x01,                 //     Report Size (1)
        0x95, 0x01,                 //     Report Count (1)
        0x81, 0x02,                 //     Input (Data, Var, Abs)

        // Confidence (1 bit)
        0x09, 0x47,                 //     Usage (Confidence)
        0x15, 0x00,                 //     Logical Min (0)
        0x25, 0x01,                 //     Logical Max (1)
        0x75, 0x01,                 //     Report Size (1)
        0x95, 0x01,                 //     Report Count (1)
        0x81, 0x02,                 //     Input (Data, Var, Abs)

        // Padding (5 bits to complete the byte)
        0x75, 0x05,                 //     Report Size (5)
        0x95, 0x01,                 //     Report Count (1)
        0x81, 0x03,                 //     Input (Const, Var, Abs)

        // Contact ID (8 bits)
        0x09, 0x51,                 //     Usage (Contact Identifier)
        0x15, 0x00,                 //     Logical Min (0)
        0x25, 0x7F,                 //     Logical Max (127)
        0x75, 0x08,                 //     Report Size (8)
        0x95, 0x01,                 //     Report Count (1)
        0x81, 0x02,                 //     Input (Data, Var, Abs)

        // X (16 bits, 0-32767)
        0x05, 0x01,                 //     Usage Page (Generic Desktop)
        0x09, 0x30,                 //     Usage (X)
        0x16, 0x00, 0x00,           //     Logical Min (0)
        0x26, 0xFF, 0x7F,           //     Logical Max (32767)
        0x75, 0x10,                 //     Report Size (16)
        0x95, 0x01,                 //     Report Count (1)
        0x81, 0x02,                 //     Input (Data, Var, Abs)

        // Y (16 bits, 0-32767)
        0x09, 0x31,                 //     Usage (Y)
        0x16, 0x00, 0x00,           //     Logical Min (0)
        0x26, 0xFF, 0x7F,           //     Logical Max (32767)
        0x75, 0x10,                 //     Report Size (16)
        0x95, 0x01,                 //     Report Count (1)
        0x81, 0x02,                 //     Input (Data, Var, Abs)

        // Pressure (8 bits, 0-255)
        0x05, 0x0D,                 //     Usage Page (Digitizers)
        0x09, 0x30,                 //     Usage (Tip Pressure)
        0x15, 0x00,                 //     Logical Min (0)
        0x25, 0xFF,                 //     Logical Max (255)
        0x75, 0x08,                 //     Report Size (8)
        0x95, 0x01,                 //     Report Count (1)
        0x81, 0x02,                 //     Input (Data, Var, Abs)

        // Width (8 bits, 0-255)
        0x09, 0x48,                 //     Usage (Width)
        0x15, 0x00,                 //     Logical Min (0)
        0x25, 0xFF,                 //     Logical Max (255)
        0x75, 0x08,                 //     Report Size (8)
        0x95, 0x01,                 //     Report Count (1)
        0x81, 0x02,                 //     Input (Data, Var, Abs)

        // Height (8 bits, 0-255)
        0x09, 0x49,                 //     Usage (Height)
        0x15, 0x00,                 //     Logical Min (0)
        0x25, 0xFF,                 //     Logical Max (255)
        0x75, 0x08,                 //     Report Size (8)
        0x95, 0x01,                 //     Report Count (1)
        0x81, 0x02,                 //     Input (Data, Var, Abs)

        // Azimuth (16 bits, 0-360)
        0x09, 0x3F,                 //     Usage (Azimuth)
        0x15, 0x00,                 //     Logical Min (0)
        0x26, 0x68, 0x01,           //     Logical Max (360)
        0x75, 0x10,                 //     Report Size (16)
        0x95, 0x01,                 //     Report Count (1)
        0x81, 0x02,                 //     Input (Data, Var, Abs)

        // Altitude (16 bits, 0-360)
        0x09, 0x40,                 //     Usage (Altitude)
        0x15, 0x00,                 //     Logical Min (0)
        0x26, 0x68, 0x01,           //     Logical Max (360)
        0x75, 0x10,                 //     Report Size (16)
        0x95, 0x01,                 //     Report Count (1)
        0x81, 0x02,                 //     Input (Data, Var, Abs)

        // Twist (16 bits, 0-360)
        0x09, 0x41,                 //     Usage (Twist)
        0x15, 0x00,                 //     Logical Min (0)
        0x26, 0x68, 0x01,           //     Logical Max (360)
        0x75, 0x10,                 //     Report Size (16)
        0x95, 0x01,                 //     Report Count (1)
        0x81, 0x02,                 //     Input (Data, Var, Abs)

      0xC0,                         //   End Collection (Logical/Finger)

      // Contact Count (8 bits) - application level, valid only in first contact
      0x05, 0x0D,                   //   Usage Page (Digitizers)
      0x09, 0x54,                   //   Usage (Contact Count)
      0x15, 0x00,                   //   Logical Min (0)
      0x25, MAX_CONTACTS,           //   Logical Max (1)
      0x75, 0x08,                   //   Report Size (8)
      0x95, 0x01,                   //   Report Count (1)
      0x81, 0x02,                   //   Input (Data, Var, Abs)

      // --- Feature Report: Contact Count Maximum (ID 2) ---
      0x85, REPORT_ID_MAX_COUNT,    //   Report ID (2)
      0x09, 0x55,                   //   Usage (Contact Count Maximum)
      0x15, 0x00,                   //   Logical Min (0)
      0x25, MAX_CONTACTS,           //   Logical Max (1)
      0x75, 0x08,                   //   Report Size (8)
      0x95, 0x01,                   //   Report Count (1)
      0xB1, 0x02,                   //   Feature (Data, Var, Abs)

    0xC0                            // End Collection (Application)
    // Report layout (16 bytes after Report ID):
    // byte 0 : bit0=TipSwitch, bit1=InRange, bit2=Confidence, bits3-7=padding
    // byte 1 : contact_id
    // bytes 2-3 : x (LE)
    // bytes 4-5 : y (LE)
    // byte 6 : pressure
    // byte 7 : width
    // byte 8 : height
    // bytes 9-10 : azimuth (LE)
    // bytes 11-12 : altitude (LE)
    // bytes 13-14 : twist (LE)
    // byte 15 : contact_count
};

// =================== CONFIGURATION DESCRIPTOR ===================

static uint8_t const hid_fs_configuration_descriptor[] = {
    TUD_CONFIG_DESCRIPTOR(1, 1, 0, CONFIG_TOTAL_LEN, 0x00, 100),
    TUD_HID_DESCRIPTOR(ITF_NUM_HID, 0, HID_ITF_PROTOCOL_NONE,
                       sizeof(hid_report_descriptor), EPNUM_HID,
                       CFG_TUD_HID_EP_BUFSIZE, HID_POLL_INTERVAL_MS)
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
// Must match descriptor exactly (16 bytes after Report ID):
// byte 0 : bit0=TipSwitch, bit1=InRange, bit2=Confidence, bits3-7=padding
// byte 1 : contact_id
// bytes 2-3  : x (LE)
// bytes 4-5  : y (LE)
// byte 6     : pressure
// byte 7     : width
// byte 8     : height
// bytes 9-10 : azimuth (LE)
// bytes 11-12: altitude (LE)
// bytes 13-14: twist (LE)
// byte 15    : contact_count
typedef struct {
    uint8_t  flags;          // bit0=TipSwitch, bit1=InRange, bit2=Confidence, bits3-7=padding
    uint8_t  contact_id;
    uint16_t x;
    uint16_t y;
    uint8_t  pressure;
    uint8_t  width;
    uint8_t  height;
    uint16_t azimuth;
    uint16_t altitude;
    uint16_t twist;
    uint8_t  contact_count;
} __attribute__((packed)) touch_report_t;

// =================== PUBLIC SEND FUNCTION ===================

void hid_touch_send(uint16_t x, uint16_t y,
                    uint8_t contact_id, uint8_t tip_switch,
                    uint8_t pressure, uint8_t in_range, uint8_t confidence,
                    uint8_t width, uint8_t height,
                    uint16_t azimuth, uint16_t altitude, uint16_t twist,
                    uint8_t contact_count)
{
    // Wait up to 100 ms for HID endpoint to be ready
    for (int i = 0; i < 10; i++) {
        if (tud_hid_ready()) break;
        vTaskDelay(pdMS_TO_TICKS(10));
    }
    if (!tud_hid_ready()) return;

    touch_report_t report;
    // The source IFPD device never reports InRange or Confidence — C# always
    // sends them as 0.  Windows requires InRange=1 AND Confidence=1 to accept
    // a contact; without them the event is treated as a palm/hover and dropped.
    // Derive both from tip_switch: touching → in range & confident.
    uint8_t eff_inrange     = tip_switch ? 1 : in_range;
    uint8_t eff_confidence  = tip_switch ? 1 : confidence;

    // Pack flags: bit0=TipSwitch, bit1=InRange, bit2=Confidence, bits3-7=padding
    report.flags         = (tip_switch      & 0x01)       |
                           ((eff_inrange    & 0x01) << 1) |
                           ((eff_confidence & 0x01) << 2);
    report.contact_id    = contact_id;
    report.x             = x;
    report.y             = y;
    report.pressure      = pressure;
    report.width         = width;
    report.height        = height;
    report.azimuth       = azimuth;
    report.altitude      = altitude;
    report.twist         = twist;
    // C# only reports ContactCount in linkCollection=0 (application level).
    // Per-finger decode always returns 0, so derive it from tip_switch.
    report.contact_count = (contact_count > 0) ? contact_count
                         : (tip_switch ? 1 : 0);

    tud_hid_report(REPORT_ID_TOUCH, &report, sizeof(report));

    // For tip=0 (lift), send a second report with contact_count=0 to inform host
    if (!tip_switch) {
        vTaskDelay(pdMS_TO_TICKS(8));
        report.flags         = 0x00;
        report.contact_count = 0;
        for (int i = 0; i < 5; i++) {
            if (tud_hid_ready()) break;
            vTaskDelay(pdMS_TO_TICKS(10));
        }
        tud_hid_report(REPORT_ID_TOUCH, &report, sizeof(report));
    }
}