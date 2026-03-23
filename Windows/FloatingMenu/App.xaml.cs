/******************************************************************************
* Copyright (C) 2026 Intel Corporation
* SPDX-License-Identifier: Apache-2.0
*******************************************************************************/

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
