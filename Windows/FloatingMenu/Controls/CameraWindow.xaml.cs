/******************************************************************************
* Copyright (C) 2026 Intel Corporation
* SPDX-License-Identifier: Apache-2.0
*******************************************************************************/
using AForge.Video;
using AForge.Video.DirectShow;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace FloatingMenu.Controls
{
    /// <summary>
    /// Interaction logic for CameraWindow.xaml
    /// </summary>
    public partial class CameraWindow : System.Windows.Window
    {
        // Aspect ratio tolerance for resolution matching (e.g., 16:9 vs 16:10)
        private const double AspectRatioTolerance = 0.15;
        const double Epsilon = 1e-6;
        public event Action CameraClosed;

        private VideoCaptureDevice _videoSource;
        private FilterInfoCollection _videoDevices;
        private int _frameCounter = 0;

        public CameraWindow(int cameraIndex)
        {
            InitializeComponent();
            Loaded += (s, e) => StartCamera(cameraIndex);

            Closed += (s, e) =>
            {
                StopCamera();
                CameraClosed?.Invoke();
            };
           
        }

        private void StartCamera(int cameraIndex = 0)
        {
            try
            {
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (_videoDevices.Count == 0)
                {
                    MessageBox.Show("No camera found");
                    return;
                }

                if (cameraIndex >= _videoDevices.Count)
                {
                    MessageBox.Show($"Invalid camera index. Found {_videoDevices.Count} devices.");
                    return;
                }

                _videoSource = new VideoCaptureDevice(_videoDevices[cameraIndex].MonikerString);

                // Log all available resolutions for diagnostics
                System.Diagnostics.Debug.WriteLine("=== Available Video Resolutions ===");
                foreach (var cap in _videoSource.VideoCapabilities)
                {
                    double aspectRatio = (double)cap.FrameSize.Width / cap.FrameSize.Height;
                    System.Diagnostics.Debug.WriteLine($"  {cap.FrameSize.Width}x{cap.FrameSize.Height} @ {cap.AverageFrameRate}fps (aspect: {aspectRatio:F2})");
                }

                // Get the current window's screen dimensions for resolution matching
                // Use WorkingArea to better match maximized borderless WPF sizing (taskbar excluded)
                var windowHandle = new WindowInteropHelper(this).Handle;
                var screen = System.Windows.Forms.Screen.FromHandle(windowHandle);
                int screenWidth = screen.WorkingArea.Width;
                int screenHeight = screen.WorkingArea.Height;
                double screenAspectRatio = (double)screenWidth / screenHeight;
                System.Diagnostics.Debug.WriteLine($"Screen working area: {screenWidth}x{screenHeight} (aspect: {screenAspectRatio:F2})");

                // Select best resolution that matches screen characteristics:
                // 1. Match screen aspect ratio (±AspectRatioTolerance for slight variations)
                // 2. Prefer resolution closest to screen resolution (balance quality vs bandwidth)
                // 3. Prefer higher frame rates (30fps+)
                // 100% adaptive - no hardcoded resolutions
                VideoCapabilities best = _videoSource.VideoCapabilities
                    .OrderByDescending(c => 
                    {
                        double capAspectRatio = (double)c.FrameSize.Width / c.FrameSize.Height;
                        double aspectDiff = Math.Abs(capAspectRatio - screenAspectRatio);

                        // Filter out resolutions with very different aspect ratios
                        if (aspectDiff - AspectRatioTolerance > Epsilon) return -1000;

                        // Calculate how close this resolution is to screen resolution
                        int widthDiff = Math.Abs(c.FrameSize.Width - screenWidth);
                        int heightDiff = Math.Abs(c.FrameSize.Height - screenHeight);
                        int totalDiff = widthDiff + heightDiff;

                        // Score based on proximity to screen resolution
                        // Closer = better (prefer not too much higher or lower)
                        int score = 10000 - totalDiff;

                        // Large bonus for exact screen resolution match
                        if (c.FrameSize.Width == screenWidth && c.FrameSize.Height == screenHeight)
                            score += 5000;

                        // Slight preference for slightly higher resolution over lower
                        // (better to downscale than upscale)
                        if (c.FrameSize.Width >= screenWidth && c.FrameSize.Height >= screenHeight)
                            score += 100;

                        return score;
                    })
                    .ThenByDescending(c => c.AverageFrameRate >= 30 ? c.AverageFrameRate : 0)
                    .ThenByDescending(c => c.FrameSize.Width * c.FrameSize.Height)
                    .First();

                _videoSource.VideoResolution = best;

                System.Diagnostics.Debug.WriteLine($">>> SELECTED: {best.FrameSize.Width}x{best.FrameSize.Height} @ {best.AverageFrameRate}fps");
                System.Diagnostics.Debug.WriteLine("===================================");

                _videoSource.NewFrame += VideoSource_NewFrame;
                _videoSource.Start();
            }
            catch (Exception e)
            {
                StopCamera();

                MessageBox.Show(
                    this,
                    "PC Cast is not Enabled for the selected device.",
                    "Camera Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                this.Close();
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                
                if (++_frameCounter % 2 != 0)
                    return;

                using (Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    IntPtr hBitmap = bitmap.GetHbitmap();

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            var source = Imaging.CreateBitmapSourceFromHBitmap(
                                hBitmap,
                                IntPtr.Zero,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());

                            CameraImage.Source = source;
                        }
                        finally
                        {
                            DeleteObject(hBitmap); 
                        }
                    }));
                }
            }
            catch
            {
                
            }
        }
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private void StopCamera()
        {
            try
            {
                if (_videoSource != null)
                {
                    _videoSource.SignalToStop();
                    _videoSource.WaitForStop();
                    _videoSource = null;
                }
            }
            catch { }
        }
    }
}
