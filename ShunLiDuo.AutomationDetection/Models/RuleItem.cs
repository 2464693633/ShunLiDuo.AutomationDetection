namespace ShunLiDuo.AutomationDetection.Models
{
    public class RuleItem
    {
        public bool IsSelected { get; set; }
        public int Id { get; set; }
        public string RuleNo { get; set; }
        public string RuleName { get; set; }
        public string DetectionRooms { get; set; } // 检测室，多个用逗号分隔
        public string LogisticsBoxNos { get; set; } // 物流盒编号，多个用逗号分隔
        public string Remark { get; set; }
    }
}

