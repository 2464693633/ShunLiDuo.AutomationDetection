namespace ShunLiDuo.AutomationDetection.Models
{
    public class DetectionRoomItem
    {
        public bool IsSelected { get; set; }
        public int Id { get; set; }
        public string RoomNo { get; set; }
        public string RoomName { get; set; }
        public string Remark { get; set; }
        
        // 扫码器串口配置
        public string ScannerPortName { get; set; }
        public int ScannerBaudRate { get; set; } = 9600;
        public int ScannerDataBits { get; set; } = 8;
        public int ScannerStopBits { get; set; } = 1;
        public string ScannerParity { get; set; } = "None";
        public bool ScannerIsEnabled { get; set; } = false;
        
        // 串口连接状态（运行时属性，不存储到数据库）
        public bool IsScannerConnected { get; set; } = false;
    }
}

