namespace ShunLiDuo.AutomationDetection.Models
{
    /// <summary>
    /// 权限数据库模型
    /// </summary>
    public class PermissionModel
    {
        public int Id { get; set; }
        public string Code { get; set; } // 权限代码，如 "RuleManagement.Add"
        public string Name { get; set; } // 权限名称，如 "新增"
        public string ModuleCode { get; set; } // 模块代码，如 "RuleManagement"
        public string ModuleName { get; set; } // 模块名称，如 "调度规则管理"
        public int SortOrder { get; set; } // 排序顺序
        public int? ParentId { get; set; } // 父权限ID，如果为null则为模块根权限
    }
}

