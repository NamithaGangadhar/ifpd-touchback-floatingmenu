/******************************************************************************
*
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
*
******************************************************************************* */

using System.Configuration;
using System.Data;
using System.Windows;
using Application = System.Windows.Application;

namespace FloatingMenu
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Start the floating edge handle by showing the main window on application startup.
            var mainWindow = new MainWindow();
            mainWindow.Show();

        }
    }
}
