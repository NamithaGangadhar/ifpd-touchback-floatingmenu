/******************************************************************************
* Copyright (C) 2026 Intel Corporation
* SPDX-License-Identifier: Apache-2.0
*******************************************************************************/


using System.IO;
using System.IO.Ports;
using System.Text.Json;

namespace FloatingMenu.Helpers
{
    internal static class ReadJSON
    {
        public static (string port, string exePath) GetPortFromExternalConfig()
        {
            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "config.json"
                );

            if (!File.Exists(path))
                throw new Exception("Config file not found");

            string json = File.ReadAllText(path);

            ConfigModel config;
            try
            {
                config = JsonSerializer.Deserialize<ConfigModel>(json);
            }
            catch (JsonException ex)
            {
                throw new Exception("Invalid JSON format: " + ex.Message);
            }

            if (config == null)
                throw new Exception("Invalid config structure");

            if (string.IsNullOrWhiteSpace(config.Port))
                throw new Exception("Port is missing in config");

            string port = config.Port.Trim().ToUpper();

            if (!port.StartsWith("COM") || !int.TryParse(port.Substring(3), out _))
                throw new Exception($"Invalid port format: {port}");

            var availablePorts = SerialPort.GetPortNames();

            if (!availablePorts.Contains(port))
                throw new Exception($"Port '{port}' not found on this machine");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string externalToolsDir = Path.Combine(baseDir, "ExternalTools", "TouchDataCaptureService");

            string releaseExePath = Path.Combine(externalToolsDir, "TouchDataCaptureService.exe");
            string debugExePath = Path.Combine(externalToolsDir, "TouchDataCaptureService.exe");

            string exePath;
            if (File.Exists(releaseExePath))
            {
                exePath = releaseExePath;
            }
            else if (File.Exists(debugExePath))
            {
                exePath = debugExePath;
            }
            else
            {
                throw new Exception($"Service EXE not found in:\n{releaseExePath}\nor\n{debugExePath}");
            }

            return (port, exePath);
        }

    }

    public class ConfigModel
    {
        public string Port { get; set; }
    }
}
