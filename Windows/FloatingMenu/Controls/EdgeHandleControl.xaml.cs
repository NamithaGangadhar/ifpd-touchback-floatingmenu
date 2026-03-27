/******************************************************************************
* Copyright (C) 2026 Intel Corporation
* SPDX-License-Identifier: Apache-2.0
*******************************************************************************/

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
