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
using System.Windows.Input;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace FloatingMenu.Controls
{
    /// <summary>
    /// Interaction logic for EdgeHandleControl.xaml
    /// </summary>
    public partial class EdgeHandleControl : UserControl
    {
        private bool _isDragging;
        private Point _startMousePosition;
        private double _startTop;
        // Declare event
        public event Action EdgeButtonClicked;

        public EdgeHandleControl()
        {
            InitializeComponent();
         
        }

        private void EdgeButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.DragMove();
            }

            if (!_isDragging)
            {
                // THIS replaces Click event
                EdgeButtonClicked?.Invoke();
            }
        }

        private void EdgeButton_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            var parentWindow = Window.GetWindow(this);
            if (parentWindow == null) return;

            Point currentPosition = e.GetPosition(null);
            double deltaY = currentPosition.Y - _startMousePosition.Y;

            double newTop = _startTop + deltaY;

            double screenHeight = SystemParameters.PrimaryScreenHeight;
            newTop = Math.Max(0, Math.Min(screenHeight - parentWindow.Height, newTop));

            //parentWindow.Left = 0;      // Always stay at left edge
            //parentWindow.Top = newTop;  // Move only vertically

            double rightDockLeft = SystemParameters.WorkArea.Right - parentWindow.Width;
            parentWindow.Left = rightDockLeft; // Keep window docked to the right edge
            parentWindow.Top = newTop;         // Move only vertically
        }

        private void EdgeButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            Mouse.Capture(null);
        }
    }
}
