using InteractiveDisplayCapture.Controls;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using MessageBox = System.Windows.MessageBox;


namespace InteractiveDisplayCapture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int WM_MOVING = 0x0216;

        //private const double ClosedWidth = 40;
        //private const double ClosedHeight = 120;

        private const double OpenWidth = 500;
        private const double OpenHeight = 400;

        private bool _menuOpen = false;
        private bool _flyoutOpen = false;
        private CameraWindow _cameraWindow;
        private Process _annotationProcess;

        private SignalSource _signalSourcePage;

        private bool _cameraClosedManually = false;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            EdgeHandle.EdgeButtonClicked += ToggleMenu;
            EdgeMenu.MenuItemSelected += EdgeMenu_MenuItemSelected;
            _signalSourcePage = new SignalSource();
           
            _signalSourcePage.DeviceSelected += OpenCameraWindow;
            // this.LocationChanged += MainWindow_LocationChanged;
        }

        private void ToggleMenu()
        {
            var screen = System.Windows.Forms.Screen.FromHandle(
                new WindowInteropHelper(this).Handle);

            double screenWidth = screen.WorkingArea.Width;
            double screenHeight = screen.WorkingArea.Height;

            System.Diagnostics.Debug.WriteLine($"Screen Width: {screenWidth}");
            System.Diagnostics.Debug.WriteLine($"Screen Height: {screenHeight}");

            if (!_menuOpen)
            {
                // OPEN STATE (percentage of screen)
                this.Width = screenWidth * 0.18;   // 18% of screen width
                this.Height = screenHeight * 0.45; // 45% of screen height

                this.Left = screen.WorkingArea.Right - this.Width;

                this.Top = screen.WorkingArea.Top +
                           (screenHeight - this.Height) / 2;

                EdgeMenu.Visibility = Visibility.Visible;

                _menuOpen = true;
                EdgeHandle.Visibility = Visibility.Collapsed;
            }
            else
            {
                // CLOSED STATE
                this.Width = screenWidth * 0.03;  // thin edge handle
                this.Height = screenHeight * 0.25;

                this.Left = screen.WorkingArea.Right - this.Width;

                this.Top = screen.WorkingArea.Top +
                           (screenHeight - this.Height) / 2;

                EdgeMenu.Visibility = Visibility.Collapsed;

                _menuOpen = false;
                EdgeHandle.Visibility = Visibility.Visible;
            }

            if (_cameraWindow != null)
            {
                FlyoutContainer.Visibility = Visibility.Visible;
                EdgeMenu.SelectMenuItem(2);
            }
        }

        private void EdgeMenu_MenuItemSelected(int index)
        {
            FlyoutContainer.Visibility= Visibility.Collapsed;
            switch (index)
            {
                case 0:
                    CollapseMenu(true);
                    break;

                case 1:
                    CloseCameraWindow();
                    CollapseMenu();
                    break;

                case 2:
                    ShowSignalSourceFlyout();
                    break;

                case 3:
                    LaunchAnnotationAppAsync();
                    break;

                case 4:
                    break;

                default:
                    CollapseMenu();
                    break;
            }
            
        }

        private void CloseCameraWindow()
        {
            if (_cameraWindow != null)
            {
                _cameraWindow.Close();
                _cameraWindow = null;
                EdgeMenu.PCStatus.Text = _signalSourcePage.UpdateDevice();
            }
        }

        private void CollapseMenu(bool clearSelection = true)
        {
            var screen = System.Windows.Forms.Screen.FromHandle(
        new WindowInteropHelper(this).Handle);

            double screenWidth = screen.WorkingArea.Width;
            double screenHeight = screen.WorkingArea.Height;
            this.Width = screenWidth * 0.035;  // thin edge handle
            this.Height = screenHeight * 0.25;

            this.Left = screen.WorkingArea.Right - this.Width;

            this.Top = screen.WorkingArea.Top +
                       (screen.WorkingArea.Height - this.Height) / 2;

            EdgeMenu.Visibility = Visibility.Collapsed;
            EdgeHandle.Visibility = Visibility.Visible;
            _menuOpen = false;
            FlyoutContainer.Visibility = Visibility.Collapsed;
            if (_cameraWindow == null)
            {
                if (clearSelection)
                    EdgeMenu.ClearSelection();
            }
        }

        private void ShowSignalSourceFlyout()
        {
            if (_signalSourcePage == null)
            {
                _signalSourcePage = new SignalSource();
                _signalSourcePage.DeviceSelected += OpenCameraWindow;
            }
        
            ShowFlyout(_signalSourcePage);
            EdgeMenu.PCStatus.Text = _signalSourcePage.UpdateDevice();
        }

        private void ShowFlyout(Page page)
        {
            FlyoutFrame.Content = page;
            FlyoutContainer.Visibility = Visibility.Visible;
            FlyoutContainer.Margin = new Thickness(0, 100, 0, 0);

            if (page is SignalSource signalPage)
            {
                signalPage.DeviceSelected += OpenCameraWindow;
            }

            page.Loaded += (s, e) =>
            {
                double targetWidth = page.ActualWidth;
                double targetHeight = page.ActualHeight;

                var animation = new DoubleAnimation
                {
                    From = 0,
                    To = targetWidth,
                    Duration = TimeSpan.FromMilliseconds(250)
                };

                FlyoutContainer.BeginAnimation(WidthProperty, animation);

                FlyoutContainer.Height = targetHeight;

            };

            _flyoutOpen = true;
        }

        private void OpenCameraWindow(int index)
        {
            if (index == -1)
            {
                CloseCameraWindow();
                return;
            }
            if (_cameraWindow != null)
                return;

            _cameraWindow = new CameraWindow(index);
            _cameraWindow.CameraClosed += OnCameraClosed;

            _cameraWindow.Show();
           
            CollapseMenu();
            EdgeMenu.PCStatus.Text = _signalSourcePage.UpdateDevice();
        }


        private void OnCameraClosed()
        {
            Dispatcher.Invoke(() =>
            {
                _cameraWindow = null;
                _cameraClosedManually = true;
                _menuOpen = false;
                ToggleMenu();
                EdgeMenu.ClearSelection();
                EdgeMenu.SelectMenuItem(2);
                if (_cameraClosedManually)
                {
                    _signalSourcePage = new SignalSource();
                    _signalSourcePage.DeviceSelected += OpenCameraWindow;
                    _cameraClosedManually = false;
                }

                ShowFlyout(_signalSourcePage);
                EdgeMenu.PCStatus.Text = _signalSourcePage.UpdateDevice();
            });
        }

       
        private async Task LaunchAnnotationAppAsync()
        {
            try
            {
                string exePath = @"C:\Program Files\WindowsApps\19566Hanakiansoftware.ScreenPaint_1.3.3.0_x64__y1w6xw98tx1ba\DesktopDrawing\ScreenPaint.exe";
                //string exePath = System.IO.Path.Combine(
                //    AppDomain.CurrentDomain.BaseDirectory,
                //    "ppInk",
                //    "ppInk.exe");

                _annotationProcess = new Process();
                _annotationProcess.StartInfo.FileName = exePath;
                _annotationProcess.EnableRaisingEvents = true;
                _annotationProcess.Exited += AnnotationProcess_Exited;

                _annotationProcess.Start();
                CollapseMenu(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to launch application:\n" + ex.Message);
            }
        }

        private void AnnotationProcess_Exited(object? sender, EventArgs e)
        {
            // Must switch back to UI thread
            Dispatcher.Invoke(() =>
            {
                _annotationProcess = null;
                EdgeMenu.ClearSelection();

            });
        }

        private void HideFlyout()
        {
            double currentWidth = FlyoutContainer.ActualWidth;

            var animation = new DoubleAnimation
            {
                From = currentWidth,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250)
            };

            animation.Completed += (s, e) =>
            {
                FlyoutContainer.Visibility = Visibility.Collapsed;
            };

            FlyoutContainer.BeginAnimation(WidthProperty, animation);

            _flyoutOpen = false;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var screen = SystemParameters.WorkArea;
            Left = screen.Right - Width;
            Top = screen.Height / 2 - Height / 2;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var handle = new WindowInteropHelper(this).Handle;
            var source = HwndSource.FromHwnd(handle);
            source.AddHook(WndProc);

            DockToRight();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOVING)
            {
                RECT rect = Marshal.PtrToStructure<RECT>(lParam);

                var screen = System.Windows.Forms.Screen.FromHandle(hwnd);
                int width = rect.Right - rect.Left;

                rect.Left = screen.WorkingArea.Right - width;
                rect.Right = screen.WorkingArea.Right;

                Marshal.StructureToPtr(rect, lParam, true);
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void DockToRight()
        {
            var screen = System.Windows.Forms.Screen.FromHandle(
                new WindowInteropHelper(this).Handle);

            this.Left = screen.WorkingArea.Right - this.Width;
        }

    }


    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}