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
        private static ConfigModel LoadConfig()
        {
            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "config.json"
                );

            if (!File.Exists(path))
                throw new FileNotFoundException($"Config file not found at: {path}");

            string json = File.ReadAllText(path);

            ConfigModel config;
            try
            {
                config = JsonSerializer.Deserialize<ConfigModel>(json);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Invalid JSON format in config file '{path}': {ex.Message}");
            }

            if (config == null)
                throw new InvalidDataException($"Failed to deserialize config from '{path}'");

            return config;
        }

        public static (string port, string exePath) GetPortFromExternalConfig()
        {
            ConfigModel config = LoadConfig();

            if (string.IsNullOrWhiteSpace(config.Port))
                throw new InvalidDataException("'Port' field is missing or empty in config.json");

            string port = config.Port.Trim().ToUpper();

            if (!port.StartsWith("COM") || !int.TryParse(port.Substring(3), out _))
                throw new InvalidDataException($"Invalid 'Port' format in config.json: '{config.Port}'. Expected format: 'COMx' (e.g., COM3)");

            var availablePorts = SerialPort.GetPortNames();

            if (!availablePorts.Contains(port))
            {
                string availablePortsList = availablePorts.Length > 0
                    ? string.Join(", ", availablePorts)
                    : "None";
                throw new InvalidOperationException($"Port '{port}' specified in config.json is not available on this machine. Available ports: {availablePortsList}");
            }

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
                throw new FileNotFoundException($"Service EXE not found. Checked paths:\n{releaseExePath}\nor\n{debugExePath}");
            }

            return (port, exePath);
        }

        public static string GetAnnotationAppPath()
        {
            ConfigModel config = LoadConfig();

            if (string.IsNullOrWhiteSpace(config.AnnotationAppPath))
                return null;

            string appPath = config.AnnotationAppPath.Trim();

            if (!File.Exists(appPath))
                throw new FileNotFoundException($"Annotation app executable not found at path specified in config.json: '{appPath}'");

            return appPath;
        }

    }

    public class ConfigModel
    {
        public string Port { get; set; }
        public string AnnotationAppPath { get; set; }
    }
}
