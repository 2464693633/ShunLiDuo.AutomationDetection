using System;

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
        
        // PLC配置 - 气缸1（阻挡气缸）
        public string Cylinder1ExtendAddress { get; set; }
        public string Cylinder1RetractAddress { get; set; }
        public string Cylinder1ExtendFeedbackAddress { get; set; }
        public string Cylinder1RetractFeedbackAddress { get; set; }
        public string Cylinder1DataType { get; set; }
        
        // PLC配置 - 气缸2（推箱气缸）
        public string Cylinder2ExtendAddress { get; set; }
        public string Cylinder2RetractAddress { get; set; }
        public string Cylinder2ExtendFeedbackAddress { get; set; }
        public string Cylinder2RetractFeedbackAddress { get; set; }
        public string Cylinder2DataType { get; set; }
        
        // PLC配置 - 传感器
        public string SensorAddress { get; set; }
        public string SensorDataType { get; set; }
        
        // 反馈报警延时时间设置（单位：毫秒）
        public int PushCylinderRetractTimeout { get; set; } = 30000;      // 推箱气缸收缩超时（匹配流程）
        public int PushCylinderExtendTimeout { get; set; } = 30000;      // 推箱气缸伸出超时（不匹配流程）
        public int BlockingCylinderRetractTimeout { get; set; } = 30000;  // 阻挡气缸收缩超时
        public int BlockingCylinderExtendTimeout { get; set; } = 30000;   // 阻挡气缸伸出超时
        public int SensorDetectTimeout { get; set; } = 15000;            // 传感器检测超时
        public int PassageDelayTime { get; set; } = 5000;                // 放行等待时间（不匹配流程）
        public int SensorConfirmDelayTime { get; set; } = 3000;          // 传感器确认延时（匹配流程，传感器检测到后的等待时间）
        
        
        // 容错模式设置
        public bool EnableBlockingCylinderRetractFeedback { get; set; } = false;  // 是否启用阻挡气缸收缩反馈检查（默认关闭，需显式启用）
        
        // 扫码数据（运行时属性，不存储到配置文件）
        public string LastScannedData { get; set; }        // 最后扫描的物流盒编码
        public DateTime? LastScanTime { get; set; }        // 物流盒编码扫描时间
        public string WorkOrderNumber { get; set; }        // 报工单编号
        public DateTime? WorkOrderScanTime { get; set; }   // 报工单扫描时间
        
        // 串口连接状态（运行时属性，不存储到数据库）
        public bool IsScannerConnected { get; set; } = false;
    }
}

