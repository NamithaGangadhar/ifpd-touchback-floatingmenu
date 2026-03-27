# ******************************************************************************
# Copyright (C) 2026 Intel Corporation
# SPDX-License-Identifier: Apache-2.0
# ******************************************************************************

idf_component_register(
    SRCS "app_main.c"
         "hid_touch.c"
    INCLUDE_DIRS "."
    PRIV_REQUIRES esp_driver_gpio esp_driver_uart
)