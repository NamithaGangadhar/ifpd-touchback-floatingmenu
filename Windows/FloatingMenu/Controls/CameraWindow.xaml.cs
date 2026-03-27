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


                VideoCapabilities best = _videoSource.VideoCapabilities
                    .OrderByDescending(c => c.FrameSize.Width * c.FrameSize.Height)
                    .First();

                _videoSource.VideoResolution = best;

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
