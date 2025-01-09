using System;
using System.Collections.Generic;
using System.Management;
using System.Linq;
using System.Diagnostics;
using System.IO;
using UsbManagementSystem.Models;
using System.Security.Principal;

namespace UsbManagementSystem.Managers
{
    public class UsbManager
    {
        private readonly SettingsManager _settingsManager;
        private readonly string _devconPath;

        public UsbManager(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
            _devconPath = FindDevconExe();

            if (string.IsNullOrEmpty(_devconPath))
            {
                throw new FileNotFoundException("Devcon.exe not found. Please ensure it is properly installed in one of these locations:\n" +
                    "1. Application directory\n" +
                    "2. C:\\Program Files (x86)\\Windows Kits\\10\\Tools\\10.0.26100.0\\x64\\\n" +
                    "3. C:\\Program Files (x86)\\Windows Kits\\10\\Tools\\10.0.26100.0\\arm64\\");
            }

            if (!IsAdministrator())
            {
                throw new UnauthorizedAccessException("This application requires administrator privileges to manage USB devices.");
            }
        }

        private string FindDevconExe()
        {
            // Look in application directory first
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "devcon.exe");
            if (File.Exists(localPath))
                return localPath;

            // Look in WDK installation paths
            string[] possiblePaths = new[]
            {
                @"C:\Program Files (x86)\Windows Kits\10\Tools\10.0.26100.0\x64\devcon.exe",
                @"C:\Program Files (x86)\Windows Kits\10\Tools\10.0.26100.0\arm64\devcon.exe"
            };

            string foundPath = possiblePaths.FirstOrDefault(File.Exists);

            if (!string.IsNullOrEmpty(foundPath))
            {
                try
                {
                    // Copy to application directory for future use
                    File.Copy(foundPath, localPath, true);
                    return localPath;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to copy devcon.exe: {ex.Message}");
                    return foundPath;
                }
            }

            return null;
        }

        private bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public List<UsbDevice> GetAllUsbDevices()
        {
            List<UsbDevice> devices = new List<UsbDevice>();

            try
            {
                // Query USB devices
                using (var searcher = new ManagementObjectSearcher(@"SELECT * FROM Win32_PnPEntity WHERE ClassGuid='{36FC9E60-C465-11CF-8056-444553540000}'"))
                using (var collection = searcher.Get())
                {
                    foreach (ManagementObject device in collection)
                    {
                        try
                        {
                            AddDeviceToList(devices, device);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error processing device: {ex.Message}");
                        }
                    }
                }

                Debug.WriteLine($"Found {devices.Count} USB devices");
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to enumerate USB devices", ex);
            }

            return devices;
        }

        private void AddDeviceToList(List<UsbDevice> devices, ManagementObject device)
        {
            string deviceID = device["DeviceID"]?.ToString() ?? "";
            string description = device["Description"]?.ToString() ??
                               device["Name"]?.ToString() ??
                               "Unknown Device";
            string manufacturer = device["Manufacturer"]?.ToString() ?? "Unknown Manufacturer";

            string vid = "0000";
            string pid = "0000";

            if (!string.IsNullOrEmpty(deviceID))
            {
                var vidMatch = System.Text.RegularExpressions.Regex.Match(deviceID, @"VID_([0-9A-F]{4})",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var pidMatch = System.Text.RegularExpressions.Regex.Match(deviceID, @"PID_([0-9A-F]{4})",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (vidMatch.Success) vid = vidMatch.Groups[1].Value;
                if (pidMatch.Success) pid = pidMatch.Groups[1].Value;
            }

            if (vid != "0000" || pid != "0000")
            {
                bool isBlocked = IsDeviceDisabled(deviceID);
                bool isWhitelisted = _settingsManager.IsDeviceWhitelisted(vid, pid);

                var usbDevice = new UsbDevice
                {
                    DeviceID = deviceID,
                    VID = vid,
                    PID = pid,
                    Description = description,
                    Manufacturer = manufacturer,
                    IsWhitelisted = isWhitelisted,
                    IsBlocked = isBlocked,
                    LastSeen = DateTime.Now
                };

                if (!devices.Any(d => d.DeviceID == deviceID))
                {
                    devices.Add(usbDevice);
                }
            }
        }

        private bool IsDeviceDisabled(string deviceID)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_PnPEntity WHERE DeviceID='{deviceID}'"))
                using (var collection = searcher.Get())
                {
                    foreach (ManagementObject device in collection)
                    {
                        string status = device["Status"]?.ToString();
                        return status == "Error" || status == "Disabled";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking device status: {ex.Message}");
            }
            return false;
        }

        public void BlockDevice(UsbDevice device)
        {
            if (device.IsWhitelisted)
            {
                throw new InvalidOperationException("Cannot block a whitelisted device");
            }

            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = _devconPath;
                    process.StartInfo.Arguments = $"disable \"*VID_{device.VID}&PID_{device.PID}*\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.Verb = "runas";

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Devcon error (Exit code: {process.ExitCode})\nOutput: {output}\nError: {error}");
                    }
                }
                device.IsBlocked = true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to block device {device.DisplayName}", ex);
            }
        }

        public void UnblockDevice(UsbDevice device)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = _devconPath;
                    process.StartInfo.Arguments = $"enable \"*VID_{device.VID}&PID_{device.PID}*\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.Verb = "runas";

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Devcon error (Exit code: {process.ExitCode})\nOutput: {output}\nError: {error}");
                    }
                }
                device.IsBlocked = false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to unblock device {device.DisplayName}", ex);
            }
        }

        public void BlockAllDevices(List<UsbDevice> devices)
        {
            var errors = new List<Exception>();

            foreach (var device in devices.Where(d => !d.IsWhitelisted && !d.IsBlocked))
            {
                try
                {
                    BlockDevice(device);
                }
                catch (Exception ex)
                {
                    errors.Add(new Exception($"Failed to block {device.DisplayName}: {ex.Message}", ex));
                }
            }

            if (errors.Any())
            {
                throw new AggregateException("Failed to block one or more devices", errors);
            }
        }

        public void UnblockAllDevices(List<UsbDevice> devices)
        {
            var errors = new List<Exception>();

            foreach (var device in devices.Where(d => d.IsBlocked))
            {
                try
                {
                    UnblockDevice(device);
                }
                catch (Exception ex)
                {
                    errors.Add(new Exception($"Failed to unblock {device.DisplayName}: {ex.Message}", ex));
                }
            }

            if (errors.Any())
            {
                throw new AggregateException("Failed to unblock one or more devices", errors);
            }
        }
    }
}