/******************************************************************************
* Copyright (C) 2026 Intel Corporation
* SPDX-License-Identifier: Apache-2.0
*******************************************************************************/

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
            NavList.PreviewMouseLeftButtonDown += NavList_PreviewMouseLeftButtonDown;
        }

        private void NavList_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = FindVisualParent<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (item != null)
            {
                int index = NavList.ItemContainerGenerator.IndexFromContainer(item);
                if (index >= 0)
                {
                    MenuItemSelected?.Invoke(index);
                }
            }
        }

        private T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
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
