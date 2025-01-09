using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UsbManagementSystem.Models;

namespace UsbManagementSystem.Managers
{
    public class SettingsManager
    {
        private readonly string _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "UsbManagementSystem",
            "settings.json"
        );

        public List<UsbDevice> WhitelistedDevices { get; private set; } = new List<UsbDevice>();

        public SettingsManager()
        {
            try
            {
                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(_settingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                LoadSettings();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to initialize settings manager", ex);
            }
        }

        public void SaveSettings()
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(WhitelistedDevices, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_settingsPath, jsonString);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to save settings", ex);
            }
        }

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    string jsonString = File.ReadAllText(_settingsPath);
                    WhitelistedDevices = JsonSerializer.Deserialize<List<UsbDevice>>(jsonString) ?? new List<UsbDevice>();
                }
                else
                {
                    WhitelistedDevices = new List<UsbDevice>();
                    SaveSettings(); // Create initial settings file
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load settings", ex);
            }
        }

        public void AddToWhitelist(UsbDevice device)
        {
            try
            {
                if (!WhitelistedDevices.Exists(d => d.VID == device.VID && d.PID == device.PID))
                {
                    device.IsWhitelisted = true;
                    device.IsBlocked = false;
                    WhitelistedDevices.Add(device);
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to add device {device.DisplayName} to whitelist", ex);
            }
        }

        public void RemoveFromWhitelist(UsbDevice device)
        {
            try
            {
                WhitelistedDevices.RemoveAll(d => d.VID == device.VID && d.PID == device.PID);
                device.IsWhitelisted = false;
                SaveSettings();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to remove device {device.DisplayName} from whitelist", ex);
            }
        }

        public bool IsDeviceWhitelisted(string vid, string pid)
        {
            return WhitelistedDevices.Exists(d =>
                d.VID.Equals(vid, StringComparison.OrdinalIgnoreCase) &&
                d.PID.Equals(pid, StringComparison.OrdinalIgnoreCase));
        }

        public void ClearWhitelist()
        {
            try
            {
                WhitelistedDevices.Clear();
                SaveSettings();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to clear whitelist", ex);
            }
        }

        public void BackupSettings(string backupPath)
        {
            try
            {
                File.Copy(_settingsPath, backupPath, true);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to backup settings", ex);
            }
        }

        public void RestoreSettings(string backupPath)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, _settingsPath, true);
                    LoadSettings();
                }
                else
                {
                    throw new FileNotFoundException("Backup file not found", backupPath);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to restore settings", ex);
            }
        }
    }
}