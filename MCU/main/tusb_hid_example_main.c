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

#include <stdio.h>
#include <string.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/queue.h"
#include "driver/uart.h"
#include "esp_log.h"
#include "tusb.h"
#include "tinyusb.h"
#include "hid_touch.h"

#define UART_PORT       UART_NUM_0
#define BUF_SIZE        256
#define UART_RX_BUF     4096   // 3Mbaud * ~10ms max burst = ~3750 bytes; 4096 gives headroom

// HID logical range: Windows auto-maps 0-32767 to Laptop 2 screen resolution.
// C# already normalizes Laptop 1 pixel coords into this range via GetSystemMetrics.
// No re-scaling needed here.
#define HID_MAX  32767

static const char *TAG = "MAIN";

void uart_init(void)
{
    uart_config_t config = {
        .baud_rate  = 3000000,
        .data_bits  = UART_DATA_8_BITS,
        .parity     = UART_PARITY_DISABLE,
        .stop_bits  = UART_STOP_BITS_1,
        .flow_ctrl  = UART_HW_FLOWCTRL_DISABLE,
    };

    ESP_ERROR_CHECK(uart_param_config(UART_PORT, &config));
    ESP_ERROR_CHECK(uart_driver_install(UART_PORT, UART_RX_BUF, 0, 0, NULL, 0));

    ESP_LOGI(TAG, "UART initialized on port %d at 3000000", UART_PORT);
}

// Fast parser for all 13 UART fields. Uses strtoul (no locale, ~5x faster than sscanf).
// Format: TOUCH,x,y,cid,tip,pressure,inrange,confidence,width,height,azimuth,altitude,twist[,contactcount]
void parse_touch(const char *line)
{
    if (strncmp(line, "TOUCH,", 6) != 0)
        return;

    char *p = (char *)(line + 6);
    char *end;

    unsigned long x         = strtoul(p, &end, 10); if (*end != ',') return; p = end + 1;
    unsigned long y         = strtoul(p, &end, 10); if (*end != ',') return; p = end + 1;
    unsigned long cid       = strtoul(p, &end, 10); if (*end != ',') return; p = end + 1;
    unsigned long tip       = strtoul(p, &end, 10); if (*end != ',') return; p = end + 1;
    unsigned long pressure  = strtoul(p, &end, 10); if (*end != ',') return; p = end + 1;
    unsigned long inrange     = strtoul(p, &end, 10); if (*end != ',') return; p = end + 1;
    unsigned long confidence   = strtoul(p, &end, 10); if (*end != ',') return; p = end + 1;
    unsigned long width     = strtoul(p, &end, 10); if (*end != ',') return; p = end + 1;
    unsigned long height    = strtoul(p, &end, 10); if (*end != ',') return; p = end + 1;
    unsigned long azimuth   = strtoul(p, &end, 10); if (*end != ',') return; p = end + 1;
    unsigned long altitude  = strtoul(p, &end, 10); if (*end != ',') return; p = end + 1;
    unsigned long twist     = strtoul(p, &end, 10);
    // field 13 (contactcount) is parsed but not used — derived from active slots

    if (x > HID_MAX) x = HID_MAX;
    if (y > HID_MAX) y = HID_MAX;

    hid_touch_update_contact(
        (uint16_t)x,   (uint16_t)y,   (uint8_t)cid, (uint8_t)tip,
        (uint8_t)inrange, (uint8_t)confidence,
        (uint8_t)pressure,
        (uint8_t)width, (uint8_t)height,
        (uint16_t)azimuth, (uint16_t)altitude, (uint16_t)twist
    );
}

static QueueHandle_t line_queue;

// Task 1: Read UART bytes, assemble lines, push complete lines to the queue.
// Pinned to CPU 1 so it never starves IDLE0 on CPU 0.
static void uart_rx_task(void *arg)
{
    uint8_t data[BUF_SIZE];
    char    line[BUF_SIZE];
    int     line_len = 0;

    for (;;)
    {
        int len = uart_read_bytes(UART_PORT, data, BUF_SIZE - 1,
                                  pdMS_TO_TICKS(1));
        if (len <= 0) { vTaskDelay(1); continue; }  // yield when idle

        for (int i = 0; i < len; i++)
        {
            char c = (char)data[i];
            if (c == '\n')
            {
                line[line_len] = '\0';
                if (line_len > 0)
                    xQueueSend(line_queue, line, 0);  // non-blocking — never stall uart_rx
                line_len = 0;
            }
            else if (c != '\r')
            {
                if (line_len < BUF_SIZE - 1)
                    line[line_len++] = c;
                else
                    line_len = 0;  // overflow — discard
            }
        }
    }
}

// Drain all co-arriving lines (one sender frame = N finger lines) before flushing.
// This collapses N flush attempts into one USB report, eliminating N-1 blocked waits.
static void usb_hid_task(void *arg)
{
    char line[BUF_SIZE];

    for (;;)
    {
        xQueueReceive(line_queue, line, portMAX_DELAY);
        parse_touch(line);

        while (xQueueReceive(line_queue, line, 0) == pdTRUE)
            parse_touch(line);
        hid_touch_flush();  // one report with all current slot states
    }
}

void app_main(void)
{
    uart_init();

    tinyusb_config_t tusb_cfg;
    memset(&tusb_cfg, 0, sizeof(tusb_cfg));
    tusb_cfg.task.size     = 4096;
    tusb_cfg.task.priority = 5;
    tusb_cfg.task.xCoreID  = 0;
    tusb_cfg.descriptor.full_speed_config = hid_touch_configuration_descriptor_fs();

    ESP_ERROR_CHECK(tinyusb_driver_install(&tusb_cfg));

    // Create queue BEFORE tasks so it is ready when tasks start
    line_queue = xQueueCreate(64, BUF_SIZE);
    configASSERT(line_queue);

    ESP_LOGI(TAG, "Ready. Waiting for TOUCH commands on UART...");

    // uart_rx on CPU 1: keeps IDLE0 on CPU 0 alive (watchdog)
    // usb_hid on CPU 0: co-located with TinyUSB
    xTaskCreatePinnedToCore(uart_rx_task, "uart_rx",  4096, NULL, 5, NULL, 1);
    xTaskCreatePinnedToCore(usb_hid_task, "usb_hid", 4096, NULL, 5, NULL, 0);
}