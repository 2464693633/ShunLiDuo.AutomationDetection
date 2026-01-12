using System;

namespace ShunLiDuo.AutomationDetection.Models
{
    /// <summary>
    /// 报警记录模型
    /// </summary>
    public class AlarmRecord
    {
        public int Id { get; set; }
        public string AlarmCode { get; set; } // 报警编号（自动生成）
        public string AlarmTitle { get; set; } // 报警标题
        public string AlarmMessage { get; set; } // 报警详细信息
        public string AlarmType { get; set; } // 报警类型（如：设备故障、超时、配置错误等）
        public string AlarmLevel { get; set; } // 报警级别（如：严重、警告、信息等）
        public int? RoomId { get; set; } // 检测室ID
        public string RoomName { get; set; } // 检测室名称
        public string DeviceName { get; set; } // 设备名称（如：推箱气缸、阻挡气缸、传感器等）
        public string Status { get; set; } // 状态：未处理、已处理
        public DateTime CreateTime { get; set; } // 报警时间
        public DateTime? HandleTime { get; set; } // 处理时间
        public string Handler { get; set; } // 处理人
        public string Remark { get; set; } // 备注
    }
}

