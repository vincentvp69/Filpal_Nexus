using System;
using System.Windows.Forms;
using UsbManagementSystem.Managers;
using UsbManagementSystem.Models;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;

namespace UsbManagementSystem
{
    public partial class Form1 : Form
    {
        private readonly UsbManager _usbManager;
        private readonly SettingsManager _settingsManager;

        private ListView lvAvailable;
        private ListView lvWhitelisted;
        private ListView lvBlocked;
        private Button btnScanDevices;
        private Button btnBlockAll;
        private Button btnUnblockAll;
        private Button btnAddToWhitelist;
        private Button btnRemoveFromWhitelist;
        private Button btnBlockSelected;
        private Button btnUnblockSelected;
        private Label lblStatus;

        public Form1()
        {
            InitializeComponent();
            InitializeControls();

            try
            {
                _settingsManager = new SettingsManager();
                _usbManager = new UsbManager(_settingsManager);
                InitializeListViews();
                ScanDevices(); // Initial scan
                UpdateStatus("Ready");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }

        private void InitializeControls()
        {
            this.Size = new Size(1000, 600);
            this.MinimumSize = new Size(1000, 600);
            this.Text = "USB Management System";

            // Initialize ListViews
            lvAvailable = new ListView
            {
                Location = new Point(12, 50),
                Size = new Size(760, 130),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };

            lvWhitelisted = new ListView
            {
                Location = new Point(12, 220),
                Size = new Size(760, 130),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };

            lvBlocked = new ListView
            {
                Location = new Point(12, 390),
                Size = new Size(760, 130),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };

            // Initialize Labels
            var lblAvailable = new Label
            {
                Location = new Point(12, 20),
                Size = new Size(200, 20),
                Text = "Available USB Devices"
            };

            var lblWhitelisted = new Label
            {
                Location = new Point(12, 190),
                Size = new Size(200, 20),
                Text = "Whitelisted Devices"
            };

            var lblBlocked = new Label
            {
                Location = new Point(12, 360),
                Size = new Size(200, 20),
                Text = "Blocked Devices"
            };

            lblStatus = new Label
            {
                Location = new Point(12, 530),
                Size = new Size(760, 20),
                Text = "Status: Initializing..."
            };

            // Initialize Buttons
            btnScanDevices = new Button
            {
                Location = new Point(800, 50),
                Size = new Size(160, 30),
                Text = "Scan Devices",
                UseVisualStyleBackColor = true
            };
            btnScanDevices.Click += btnScanDevices_Click;

            btnBlockSelected = new Button
            {
                Location = new Point(800, 90),
                Size = new Size(160, 30),
                Text = "Block Selected",
                UseVisualStyleBackColor = true
            };
            btnBlockSelected.Click += btnBlockSelected_Click;

            btnBlockAll = new Button
            {
                Location = new Point(800, 130),
                Size = new Size(160, 30),
                Text = "Block All",
                UseVisualStyleBackColor = true
            };
            btnBlockAll.Click += btnBlockAll_Click;

            btnAddToWhitelist = new Button
            {
                Location = new Point(800, 220),
                Size = new Size(160, 30),
                Text = "Add to Whitelist",
                UseVisualStyleBackColor = true
            };
            btnAddToWhitelist.Click += btnAddToWhitelist_Click;

            btnRemoveFromWhitelist = new Button
            {
                Location = new Point(800, 260),
                Size = new Size(160, 30),
                Text = "Remove from Whitelist",
                UseVisualStyleBackColor = true
            };
            btnRemoveFromWhitelist.Click += btnRemoveFromWhitelist_Click;

            btnUnblockSelected = new Button
            {
                Location = new Point(800, 390),
                Size = new Size(160, 30),
                Text = "Unblock Selected",
                UseVisualStyleBackColor = true
            };
            btnUnblockSelected.Click += btnUnblockSelected_Click;

            btnUnblockAll = new Button
            {
                Location = new Point(800, 430),
                Size = new Size(160, 30),
                Text = "Unblock All",
                UseVisualStyleBackColor = true
            };
            btnUnblockAll.Click += btnUnblockAll_Click;

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                lblAvailable, lblWhitelisted, lblBlocked, lblStatus,
                lvAvailable, lvWhitelisted, lvBlocked,
                btnScanDevices, btnBlockSelected, btnBlockAll,
                btnAddToWhitelist, btnRemoveFromWhitelist,
                btnUnblockSelected, btnUnblockAll
            });
        }

        private void InitializeListViews()
        {
            foreach (ListView lv in new[] { lvAvailable, lvWhitelisted, lvBlocked })
            {
                lv.Columns.Clear();
                lv.Columns.Add("Description", 300);
                lv.Columns.Add("VID", 80);
                lv.Columns.Add("PID", 80);
                lv.Columns.Add("Manufacturer", 280);
            }
        }

        private void ScanDevices()
        {
            try
            {
                var allDevices = _usbManager.GetAllUsbDevices();
                UpdateListView(lvAvailable, allDevices.Where(d => !d.IsWhitelisted && !d.IsBlocked));
                UpdateListView(lvWhitelisted, allDevices.Where(d => d.IsWhitelisted));
                UpdateListView(lvBlocked, allDevices.Where(d => d.IsBlocked));
                UpdateStatus($"Last scanned: {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error scanning devices: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        private void UpdateListView(ListView listView, IEnumerable<UsbDevice> devices)
        {
            listView.Items.Clear();
            foreach (var device in devices)
            {
                var item = new ListViewItem(device.Description);
                item.SubItems.Add(device.VID);
                item.SubItems.Add(device.PID);
                item.SubItems.Add(device.Manufacturer);
                item.Tag = device;
                listView.Items.Add(item);
            }
        }

        private void UpdateStatus(string message)
        {
            lblStatus.Text = $"Status: {message}";
        }

        private void btnScanDevices_Click(object sender, EventArgs e)
        {
            ScanDevices();
        }

        private void btnBlockSelected_Click(object sender, EventArgs e)
        {
            if (lvAvailable.SelectedItems.Count > 0)
            {
                try
                {
                    var device = (UsbDevice)lvAvailable.SelectedItems[0].Tag;
                    _usbManager.BlockDevice(device);
                    ScanDevices();
                    UpdateStatus($"Blocked device: {device.Description}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error blocking device: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnBlockAll_Click(object sender, EventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Are you sure you want to block all non-whitelisted devices?",
                    "Confirm Block All",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    var devices = _usbManager.GetAllUsbDevices();
                    _usbManager.BlockAllDevices(devices);
                    ScanDevices();
                    UpdateStatus("All non-whitelisted devices have been blocked");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error blocking devices: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddToWhitelist_Click(object sender, EventArgs e)
        {
            if (lvAvailable.SelectedItems.Count > 0)
            {
                try
                {
                    var device = (UsbDevice)lvAvailable.SelectedItems[0].Tag;
                    _settingsManager.AddToWhitelist(device);
                    if (device.IsBlocked)
                    {
                        _usbManager.UnblockDevice(device);
                    }
                    ScanDevices();
                    UpdateStatus($"Added {device.Description} to whitelist");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding device to whitelist: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnRemoveFromWhitelist_Click(object sender, EventArgs e)
        {
            if (lvWhitelisted.SelectedItems.Count > 0)
            {
                try
                {
                    var result = MessageBox.Show(
                        "Are you sure you want to remove this device from the whitelist?",
                        "Confirm Remove",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        var device = (UsbDevice)lvWhitelisted.SelectedItems[0].Tag;
                        _settingsManager.RemoveFromWhitelist(device);
                        ScanDevices();
                        UpdateStatus($"Removed {device.Description} from whitelist");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error removing device from whitelist: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnUnblockSelected_Click(object sender, EventArgs e)
        {
            if (lvBlocked.SelectedItems.Count > 0)
            {
                try
                {
                    var device = (UsbDevice)lvBlocked.SelectedItems[0].Tag;
                    _usbManager.UnblockDevice(device);
                    ScanDevices();
                    UpdateStatus($"Unblocked device: {device.Description}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error unblocking device: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnUnblockAll_Click(object sender, EventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Are you sure you want to unblock all devices?",
                    "Confirm Unblock All",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    var devices = _usbManager.GetAllUsbDevices();
                    _usbManager.UnblockAllDevices(devices);
                    ScanDevices();
                    UpdateStatus("All devices have been unblocked");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error unblocking devices: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}