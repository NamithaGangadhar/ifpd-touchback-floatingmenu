/******************************************************************************
* Copyright (C) 2026 Intel Corporation
* SPDX-License-Identifier: Apache-2.0
*******************************************************************************/

using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace FloatingMenu.Controls
{
    /// <summary>
    /// Interaction logic for CameraWindow.xaml
    /// </summary>
    public partial class CameraWindow : System.Windows.Window
    {
        private VideoCapture _capture;
        private CancellationTokenSource _cameraTokenSource;
        private Task _cameraTask;
        public event Action CameraClosed;
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

        private void StartCamera(int cameraIndex)
        {
            _cameraTokenSource = new CancellationTokenSource();
            var token = _cameraTokenSource.Token;

            _capture = new VideoCapture(cameraIndex, VideoCaptureAPIs.DSHOW);

            if (!_capture.IsOpened())
            {
                MessageBox.Show("Camera failed to open!");
                StopCamera();
                return;
            }

            // Get primary screen resolution dynamically
            var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            _capture.Set(VideoCaptureProperties.FrameWidth, screenWidth);
            _capture.Set(VideoCaptureProperties.FrameHeight, screenHeight);

            _capture.Set(VideoCaptureProperties.Fps, 60);
            
            _cameraTask = Task.Run(async () =>
            {
                using var frame = new Mat();

                while (!token.IsCancellationRequested)
                {
                    _capture.Read(frame);

                    if (!frame.Empty())
                    {
                        var image = frame.ToBitmapSource();
                        image.Freeze();

                        Dispatcher.BeginInvoke(() =>
                        {
                            CameraImage.Source = image;
                        });
                    }

                    await Task.Delay(16);
                }
            }, token);
        }

        private void StopCamera()
        {
            try
            {
                _cameraTokenSource?.Cancel();
                _cameraTask?.Wait(300);

                _capture?.Release();
                _capture?.Dispose();
                _capture = null;
            }
            catch { }
        }
    }
}
