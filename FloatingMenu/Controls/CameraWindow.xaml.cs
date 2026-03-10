/******************************************************************************
*
* Copyright (C) 2026 Intel Corporation
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

using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace InteractiveDisplayCapture.Controls
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
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                    Close();
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
                return;
            }

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

                    await Task.Delay(30);
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

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }
}
