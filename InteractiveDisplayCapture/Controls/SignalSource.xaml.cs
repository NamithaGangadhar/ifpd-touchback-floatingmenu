using InteractiveDisplayCapture.Helpers;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;

namespace InteractiveDisplayCapture.Controls
{
    /// <summary>
    /// Interaction logic for SignalSource.xaml
    /// </summary>
    public partial class SignalSource : Page, INotifyPropertyChanged
    {

        public ObservableCollection<SignalSourceModel> Devices { get; set; }
        private VideoCapture _capture;
        private bool _isCameraRunning;
        private SignalSourceModel _selectedDevice;

        private CancellationTokenSource _cameraTokenSource;
        private Task _cameraTask;
        private CameraWindow _cameraWindow;
        public event Action<int> DeviceSelected;
        private SignalSourceModel device;
        public SignalSourceModel SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                //if (_selectedDevice == value)
                //    return;
                if (value == null)
                    return;

                // Clicking SAME device again
                if (_selectedDevice == value)
                {
                    if (_selectedDevice.Status == DeviceStatusEnum.Connected)
                    {
                        // Turn back to Available (Blue)
                        _selectedDevice.Status = DeviceStatusEnum.Available;

                        // Close camera
                        DeviceSelected?.Invoke(-1);
                    }

                    return;
                }

                // Reset old device (if it was connected)
                if (_selectedDevice != null &&
                    _selectedDevice.Status == DeviceStatusEnum.Connected)
                {
                    _selectedDevice.Status = DeviceStatusEnum.Available;
                }

                _selectedDevice = value;

                // Make new one connected
                if (_selectedDevice != null &&
                    _selectedDevice.Status == DeviceStatusEnum.Available)
                {
                    _selectedDevice.Status = DeviceStatusEnum.Connected;
                    
                    //  StartCamera(0); // For now using index 0

                    // OpenCameraWindow();
                    DeviceSelected?.Invoke(0);

                }

                OnPropertyChanged();
              
            }
        }
        public SignalSource()
        {
            InitializeComponent();
            LoadCameras();
            DataContext = this;

            Devices = new ObservableCollection<SignalSourceModel>
            {
                new SignalSourceModel { Name = "PC1", Status = DeviceStatusEnum.Disconnected },
                new SignalSourceModel { Name = "PC2", Status = DeviceStatusEnum.Available }
            };
            device = new SignalSourceModel();
            this.Unloaded += (s, e) => StopCamera();

           // this.Unloaded += SignalSource_Unloaded;
        }

        private async void LoadCameras()
        {
            var cameras = await GetConnectedCameras();

            Devices.Clear();

            foreach (var cam in cameras)
            {
                Devices.Add(new SignalSourceModel
                {
                    Name = $"PC {Devices.Count + 1}",
                    Status = DeviceStatusEnum.Available
                });
            }
        }

        public void StopCamera()
        {
            try
            {
                _cameraTokenSource?.Cancel();
                _cameraTask?.Wait(300);

                _capture?.Release();
                _capture?.Dispose();

                _capture = null;
                _cameraTokenSource = null;
                _cameraTask = null;

                CameraImage.Source = null;
            }
            catch
            {
                // ignore cleanup errors
            }
        }

        private void OpenCameraWindow()
        {
            if (_cameraWindow == null || !_cameraWindow.IsVisible)
            {
                _cameraWindow = new CameraWindow(0);
                _cameraWindow.Show();
            }
            else
            {
                _cameraWindow.Activate();
            }
        }
        public void StartCamera(int cameraIndex = 0)
        {
            StopCamera();

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

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            CameraImage.Source = image;
                        }));
                    }

                    await Task.Delay(30); // Important! Prevent CPU 100%
                }
            }, token);
        }

       


        private async void OpenCamera()
        {
            _capture?.Release();
            _capture = new VideoCapture(0); // index 0 for POC

            if (!_capture.IsOpened())
                return;

            _isCameraRunning = true;

            await Task.Run(() =>
            {
                var frame = new Mat();

                while (_isCameraRunning)
                {
                    _capture.Read(frame);

                    if (!frame.Empty())
                    {
                        Dispatcher.Invoke(() =>
                        {
                            CameraImage.Source = frame.ToBitmapSource();
                        });
                    }
                }

                frame.Dispose();
            });
        }

        public void StopCamera1()
        {
            _isCameraRunning = false;

            if (_capture != null)
            {
                _capture.Release();
                _capture.Dispose();
                _capture = null;
            }

            CameraImage.Source = null;
        }

        private void SignalSource_Unloaded(object sender, RoutedEventArgs e)
        {
            StopCamera();
        }

        public async Task<List<string>> GetConnectedCameras()
        {
            var result = new List<string>();

            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c pnputil /enum-devices /class Camera /connected";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("Device Description", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("Friendly Name", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = trimmed.Split(':');

                    if (parts.Length > 1)
                    {
                        string name = parts[1].Trim();
                        result.Add(name);
                    }
                }
            }

            return result;
        }

        public List<string> GetConnectedCameras1()
        {
            var result = new List<string>();

            var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Camera'");

            foreach (ManagementObject device in searcher.Get())
            {
                var name = device["Name"]?.ToString();
                if (!string.IsNullOrEmpty(name))
                    result.Add(name);
            }

            return result;
        }

        public List<string> GetConnectedCameras2()
        {
            var result = new List<string>();

            var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Camera'");

            foreach (var device in searcher.Get())
            {
                result.Add(device["Name"]?.ToString());
            }

            return result;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void DeviceList_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(sender is ListBox listBox)
            {
                var item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;

                if (item?.DataContext is SignalSourceModel device)
                {
                    ToggleDevice(device);
                }

            }
        }

        private void ToggleDevice(SignalSourceModel device)
        {
            if (device.Status == DeviceStatusEnum.Available)
            {
                // Disconnect any previously connected device
                foreach (var d in Devices)
                {
                    if (d.Status == DeviceStatusEnum.Connected)
                        d.Status = DeviceStatusEnum.Available;
                }

                device.Status = DeviceStatusEnum.Connected;
                
                DeviceSelected?.Invoke(0); // Open camera
            }
            else if (device.Status == DeviceStatusEnum.Connected)
            {
                device.Status = DeviceStatusEnum.Available;

                DeviceSelected?.Invoke(-1); // Close camera
            }
            //UpdateDevice(device);
        }

        public string UpdateDevice()
        {
            if (device.Status == DeviceStatusEnum.Connected)
            {
                return device.Name;
            }
            else if (device.Status != DeviceStatusEnum.Connected)
            {
                return string.Empty;
            }
            return string.Empty;
        }
    }
}
