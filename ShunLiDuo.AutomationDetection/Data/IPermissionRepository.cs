using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public interface IPermissionRepository
    {
        Task<List<PermissionModel>> GetAllPermissionsAsync();
        Task<List<PermissionModel>> GetModulePermissionsAsync(string moduleCode);
    }
}

