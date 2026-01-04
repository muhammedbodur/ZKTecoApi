using System;

namespace ZKTecoApi.DTOs.Response
{
    public class AttendanceLogResponse
    {
        public string EnrollNumber { get; set; }
        public DateTime DateTime { get; set; }
        public int VerifyMethod { get; set; }
        public int InOutMode { get; set; }
        public int WorkCode { get; set; }
        public string DeviceIp { get; set; }
    }
}
