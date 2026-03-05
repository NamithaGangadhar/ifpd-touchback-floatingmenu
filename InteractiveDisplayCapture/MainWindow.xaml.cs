using InteractiveDisplayCapture.Controls;
using InteractiveDisplayCapture.Helpers;
using InteractiveDisplayCapture.Services;
using InteractiveDisplayCapture.ViewModels;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MessageBox = System.Windows.MessageBox;


namespace InteractiveDisplayCapture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int WM_MOVING = 0x0216;

        private const double ClosedWidth = 40;
        private const double ClosedHeight = 120;

        private const double OpenWidth = 500;
        private const double OpenHeight = 400;

        private bool _menuOpen = false;
        private bool _flyoutOpen = false;
        private CameraWindow _cameraWindow;
        private Process _annotationProcess;

        private SignalSource _signalSourcePage;

        private bool _isAnnotationRunning = false;
        private bool _isCameraRunning = false;

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

            if (!_menuOpen)
            {
                // OPEN STATE
                this.Width = OpenWidth;
                this.Height = OpenHeight;

                // keep right edge locked
                this.Left = screen.WorkingArea.Right - this.Width;

                // keep vertical center
                this.Top = screen.WorkingArea.Top +
                           (screen.WorkingArea.Height - this.Height) / 2;

                EdgeMenu.Visibility = Visibility.Visible;
                
                _menuOpen = true;
                EdgeHandle.Visibility = Visibility.Collapsed;
            }
            else
            {
                // CLOSED STATE
                this.Width = ClosedWidth;
                this.Height = ClosedHeight;

                this.Left = screen.WorkingArea.Right - this.Width;

                this.Top = screen.WorkingArea.Top +
                           (screen.WorkingArea.Height - this.Height) / 2;

                EdgeMenu.Visibility = Visibility.Collapsed;
               
                _menuOpen = false;

                EdgeHandle.Visibility = Visibility.Visible;
            }
            if(_cameraWindow != null)
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
                    //if (_cameraWindow != null)
                    //{
                    //    // Camera already running — just expand menu
                    //    ToggleMenu();
                    //}
                    //else
                    //{
                    //    ShowFlyout(new SignalSource());
                    //}
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

            this.Width = ClosedWidth;
            this.Height = ClosedHeight;

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
            // If already created, reuse it
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
           // FlyoutFrame.Navigate(page);
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
            _isCameraRunning = true;
           
            CollapseMenu();
          
            //if (_cameraWindow == null)
            //{
            //    _cameraWindow = new CameraWindow(index);
            //   // _cameraWindow.CameraClosed += OnCameraClosed;
            //    _cameraWindow.Show();

            //   // HideFlyoutAndMenu();
            //}
            //else
            //{
            //    _cameraWindow.Activate();
            //}
        }
        private bool _cameraClosedManually = false;
        private void OnCameraClosed()
        {
            //Dispatcher.Invoke(() =>
            //{
            //    _cameraWindow = null;

            //    // Open menu
            //    ToggleMenu();

            //    // Only create a new SignalSource if you want fresh page
            //    // Comment out these lines to persist previous selection
            //    // var newSignalSource = new SignalSource();
            //    // ShowFlyout(newSignalSource);

            //    // Instead, reuse existing SignalSource
            //    if (_signalSourcePage != null)
            //    {
            //        ShowFlyout(_signalSourcePage);
            //    }
            //});

            //Dispatcher.Invoke(() =>
            //{
            //    _cameraWindow = null;

            //    // Open menu
            //    ToggleMenu();

            //    // Reopen fresh SignalSource
            //    var newSignalSource = new SignalSource();
            //    ShowFlyout(newSignalSource);
            //});

            Dispatcher.Invoke(() =>
            {
                _cameraWindow = null;
                _cameraClosedManually = true;
                _isCameraRunning = false;
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

        private void CloseCameraIfOpen()
        {
            _signalSourcePage?.StopCamera();
        }

        private async Task LaunchAnnotationAppAsync()
        {
            try
            {
                //if (_annotationProcess != null && !_annotationProcess.HasExited)
                //    return; // Already running
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

                _isAnnotationRunning = true;
                CollapseMenu(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to launch application:\n" + ex.Message);
            }
        }

        private void AnnotationApp_Exited(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _isAnnotationRunning = false;
                EdgeMenu.ClearSelection();
                _annotationProcess.Dispose();
                _annotationProcess = null;
            });
        }

        private async Task LaunchAnnotationAppAsync1()
        {
            try
            {
               // EdgeMenu.SelectMenuItem(3);

                string exePath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "ppInk",
                    "ppInk.exe");

                Process.Start(exePath);

                CollapseMenu(false); 
                await MonitorAnnotationApp();

                ////string exePath = @"C:\Program Files\WindowsApps\19566Hanakiansoftware.ScreenPaint_1.3.3.0_x64__y1w6xw98tx1ba\DesktopDrawing\ScreenPaint.exe";
                //Process.Start(exePath);

                //CollapseMenu(false); // keep selection

                //// Wait until real ppInk process appears
                //await WaitForAnnotationToClose();


                //Process process = new Process();
                //process.StartInfo.FileName = exePath;
                //process.StartInfo.UseShellExecute = true;
                //process.EnableRaisingEvents = true;

                //process.Exited += AnnotationApp_Exited;

                //process.Start();

                //// Hide your dock
                //CollapseMenu(false);   // or this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to launch application:\n" + ex.Message);
            }
        }

        private async Task MonitorAnnotationApp()
        {
            await Task.Run(() =>
            {
                while (Process.GetProcessesByName("ppInk").Length == 0)
                    Thread.Sleep(300);
               // _isAnnotationRunning = true;

                while (Process.GetProcessesByName("ppInk").Length > 0)
                    Thread.Sleep(500);
            });

            // Back to UI thread
            Dispatcher.Invoke(() =>
            {
              //  _isAnnotationRunning = false;
                EdgeMenu.ClearSelection();
            });
        }

        private async Task WaitForAnnotationToClose()
        {
            await Task.Run(() =>
            {
                Process annotationProcess = null;

                // Wait until process starts
                while (annotationProcess == null)
                {
                    var processes = Process.GetProcessesByName("ppInk");
                    if (processes.Length > 0)
                        annotationProcess = processes[0];

                    Thread.Sleep(300);
                }

                // Wait until it exits
                annotationProcess.WaitForExit();
            });

            // Back to UI thread
            Dispatcher.Invoke(() =>
            {
                EdgeMenu.ClearSelection();

                if (!_menuOpen)
                    ToggleMenu();
            });
        }

        private void AnnotationProcess_Exited(object? sender, EventArgs e)
        {
            // Must switch back to UI thread
            Dispatcher.Invoke(() =>
            {
                _annotationProcess = null;
                EdgeMenu.ClearSelection();

                // EdgeMenu.ClearSelection();
                // // Open the menu
                // if (!_menuOpen)
                //     ToggleMenu();

                // // Select Annotation item (example index 3)
                //// EdgeMenu.SelectMenuItem(3);
            });
        }

        private void ShowDevicePanel()
        {
            FlyoutFrame.Navigate(new SignalSource());

            FlyoutContainer.Width = 0;
            FlyoutContainer.Visibility = Visibility.Visible;

            var sb = (Storyboard)FindResource("SlideInStoryboard");
            sb.Begin();
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

        public void OpenMenu()
        {
            //EdgeMenu.Visibility = Visibility.Visible;
        }

        public void CloseMenu()
        {
           // EdgeMenu.Visibility = Visibility.Collapsed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var screen = SystemParameters.WorkArea;
            Left = screen.Right - Width;
            Top = screen.Height / 2 - Height / 2;
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            var lockButtonEdges = new LockButtonEdges();
            lockButtonEdges.LockToRightEdge(this);
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