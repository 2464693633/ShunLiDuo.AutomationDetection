namespace ShunLiDuo.AutomationDetection.Models
{
    public class UserItem
    {
        public int Id { get; set; }
        public string AccountNo { get; set; }
        public string LoginAccount { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public string Phone { get; set; }
        public string EmployeeNo { get; set; }
        public int? RoleId { get; set; }
        public string Role { get; set; } // 角色名称，用于显示
        public string Remark { get; set; }
    }
}

