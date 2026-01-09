using System;

namespace ShunLiDuo.AutomationDetection.Models
{
    public class DetectionLogItem
    {
        public int Id { get; set; }
        public string LogisticsBoxCode { get; set; } // 物流盒编码
        public int? RoomId { get; set; } // 检测室ID
        public string RoomName { get; set; } // 检测室名称
        public string Status { get; set; } // 状态：未检测、检测中、检测完成
        public DateTime? StartTime { get; set; } // 检测开始时间
        public DateTime? EndTime { get; set; } // 检测完成时间
        public DateTime CreateTime { get; set; } // 创建时间
        public string Remark { get; set; } // 备注
    }
}

