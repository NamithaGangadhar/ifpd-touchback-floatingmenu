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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using UserControl = System.Windows.Controls.UserControl;

namespace FloatingMenu.Controls
{
    /// <summary>
    /// Interaction logic for EdgeMenuControl.xaml
    /// </summary>
    public partial class EdgeMenuControl : UserControl
    {
        private bool _suppressSelectionEvent;
        public event Action<int> MenuItemSelected;

        public EdgeMenuControl()
        {
            InitializeComponent();
            Loaded += EdgeMenuControl_Loaded;
        }

        private void EdgeMenuControl_Loaded(object sender, RoutedEventArgs e)
        {
            AdjustMenuSize();
        }

        private void AdjustMenuSize()
        {
            var window = Window.GetWindow(this);

            var screen = System.Windows.Forms.Screen.FromHandle(
                         new WindowInteropHelper(window).Handle);

            double screenHeight = screen.WorkingArea.Height;
            double screenWidth = screen.WorkingArea.Width;

            // Menu height = 45% of screen
            this.Height = screenHeight * 0.4;

            // Menu width = 4% of screen width
            this.Width = screenWidth * 0.033;
        }

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionEvent)
                return;

            if (NavList.SelectedIndex < 0)
                return;

            // Fire event to MainWindow
            MenuItemSelected?.Invoke(NavList.SelectedIndex);
        }

        public void ClearSelection()
        {
            NavList.SelectedItem = null;
        }

        public void SelectMenuItem(int index)
        {
            _suppressSelectionEvent = true;

            NavList.SelectedIndex = index;

            _suppressSelectionEvent = false;
        }
    }
}
