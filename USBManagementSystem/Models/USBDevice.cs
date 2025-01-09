using System;

namespace UsbManagementSystem.Models
{
    public class UsbDevice
    {
        public string DeviceID { get; set; }
        public string VID { get; set; }
        public string PID { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public bool IsWhitelisted { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime LastSeen { get; set; }

        public string DisplayName => $"{Description} (VID:{VID} PID:{PID})";
    }
}