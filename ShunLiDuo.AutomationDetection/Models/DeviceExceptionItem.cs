using System;

namespace ShunLiDuo.AutomationDetection.Models
{
    public class DeviceExceptionItem
    {
        public int Id { get; set; }
        public string ExceptionNo { get; set; }
        public DateTime CreateTime { get; set; }
        public string DeviceName { get; set; }
        public string RoomName { get; set; }
        public int? ExceptionType { get; set; }
        public string Remark { get; set; }
    }
}

