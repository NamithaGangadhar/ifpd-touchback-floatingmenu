/******************************************************************************
* Copyright (C) 2026 Intel Corporation
* SPDX-License-Identifier: Apache-2.0
*******************************************************************************/

using FloatingMenu.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;

namespace FloatingMenu.Controls
{
    /// <summary>
    /// Interaction logic for SignalSource.xaml
    /// </summary>
    public partial class SignalSource : Page, INotifyPropertyChanged
    {
        public ObservableCollection<SignalSourceModel> Devices { get; set; }

        public event Action<int> DeviceSelected;

        private bool _isOperationInProgress = false;
      
        public SignalSource()
        {
            InitializeComponent();

            Devices = new ObservableCollection<SignalSourceModel>
            {
                new SignalSourceModel { Name = "PC1", Status = DeviceStatusEnum.Disconnected },
                new SignalSourceModel { Name = "PC2", Status = DeviceStatusEnum.Available }
            };
           
            DataContext = this;
            LoadCameras();
           
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
                    Status = DeviceStatusEnum.Available,
                    CameraIndex = Devices.Count
                });
            }
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

    
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            if (_isOperationInProgress)
                return;

            if (device.Status == DeviceStatusEnum.Available)
            {
                foreach (var d in Devices)
                {
                    if (d.Status == DeviceStatusEnum.Connected)
                        d.Status = DeviceStatusEnum.Available;
                }

                device.Status = DeviceStatusEnum.Connected;
                _isOperationInProgress = true;

                DeviceSelected?.Invoke(device.CameraIndex);
            }
            else if (device.Status == DeviceStatusEnum.Connected)
            {
                device.Status = DeviceStatusEnum.Available;
                _isOperationInProgress = true;

                DeviceSelected?.Invoke(-1);
            }
        }

        public void ResetOperationState()
        {
            _isOperationInProgress = false;
        }

    }
}
