namespace ShunLiDuo.AutomationDetection.Models
{
    public class RoleItem
    {
        public int Id { get; set; }
        public string RoleNo { get; set; }
        public string RoleName { get; set; }
        public string Remark { get; set; }
        public string Permissions { get; set; } // JSON格式存储权限
    }
}

