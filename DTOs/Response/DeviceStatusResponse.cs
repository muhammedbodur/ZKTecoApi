using System;

namespace ZKTecoApi.DTOs.Response
{
    public class DeviceStatusResponse
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public bool IsConnected { get; set; }
        public string Platform { get; set; }
        public string FirmwareVersion { get; set; }
        public string SerialNumber { get; set; }
        public string DeviceModel { get; set; }
        public int UserCount { get; set; }
        public int LogCount { get; set; }
        public int UserCapacity { get; set; }
        public int LogCapacity { get; set; }
        public DateTime? DeviceTime { get; set; }
        public bool IsEnabled { get; set; }
    }
}
