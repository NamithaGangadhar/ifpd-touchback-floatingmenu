#include <stdio.h>
#include <string.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/uart.h"
#include "esp_log.h"
#include "tusb.h"
#include "tinyusb.h"
#include "hid_touch.h"

#define UART_PORT   UART_NUM_0
#define BUF_SIZE    256

// HID logical range: Windows auto-maps 0-32767 to Laptop 2 screen resolution.
// C# already normalizes Laptop 1 pixel coords into this range via GetSystemMetrics.
// No re-scaling needed here.
#define HID_MAX  32767

static const char *TAG = "MAIN";

void uart_init(void)
{
    uart_config_t config = {
        .baud_rate  = 2000000,
        .data_bits  = UART_DATA_8_BITS,
        .parity     = UART_PARITY_DISABLE,
        .stop_bits  = UART_STOP_BITS_1,
        .flow_ctrl  = UART_HW_FLOWCTRL_DISABLE,
    };

    ESP_ERROR_CHECK(uart_param_config(UART_PORT, &config));
    ESP_ERROR_CHECK(uart_driver_install(UART_PORT, BUF_SIZE * 2, 0, 0, NULL, 0));

    ESP_LOGI(TAG, "UART initialized on port %d at 2000000", UART_PORT);
}

void parse_touch(char *line)
{
    // Strip CR and LF
    line[strcspn(line, "\r\n")] = '\0';

    if (strncmp(line, "TOUCH", 5) != 0)
        return;

    uint16_t x, y;
    uint8_t  cid, tip;
    uint16_t pressure;

    if (sscanf(line, "TOUCH,%hu,%hu,%hhu,%hhu,%hu",
               &x, &y, &cid, &tip, &pressure) == 5)
    {
        // C# already sends normalized 0-32767 coords (via GetSystemMetrics).
        // Just clamp to be safe — no re-scaling needed.
        if (x > HID_MAX) x = HID_MAX;
        if (y > HID_MAX) y = HID_MAX;

        ESP_LOGI(TAG, "Touch: hid=(%d,%d) cid=%d tip=%d press=%d",
                 x, y, cid, tip, pressure);

        hid_touch_send(x, y, cid, tip, pressure);
    }
    else
    {
        ESP_LOGW(TAG, "Parse failed: [%s]", line);
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

    ESP_LOGI(TAG, "Ready. Waiting for TOUCH commands on UART...");

    uint8_t data[BUF_SIZE];
    char    line[BUF_SIZE];
    int     line_len = 0;

    while (1)
    {
        int len = uart_read_bytes(UART_PORT, data, BUF_SIZE - 1,
                                  pdMS_TO_TICKS(20));
        if (len <= 0) continue;

        for (int i = 0; i < len; i++)
        {
            char c = (char)data[i];
            if (c == '\n')
            {
                line[line_len] = '\0';
                if (line_len > 0) parse_touch(line);
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