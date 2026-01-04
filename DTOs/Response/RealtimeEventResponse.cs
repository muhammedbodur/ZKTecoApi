using System;

namespace ZKTecoApi.DTOs.Response
{
    public class RealtimeEventResponse
    {
        public string EventType { get; set; }
        public string EnrollNumber { get; set; }
        public DateTime EventTime { get; set; }
        public int VerifyMethod { get; set; }
        public int InOutMode { get; set; }
        public int WorkCode { get; set; }
        public string DeviceIp { get; set; }
        public bool? IsValid { get; set; }
        public long? CardNumber { get; set; }
        public string Message { get; set; }
    }
}
