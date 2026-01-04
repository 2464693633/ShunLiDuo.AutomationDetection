using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public interface IRoleRepository
    {
        Task<List<RoleItem>> GetAllRolesAsync();
        Task<List<RoleItem>> SearchRolesAsync(string keyword);
        Task<RoleItem> GetRoleByIdAsync(int id);
        Task<int> InsertRoleAsync(RoleItem role);
        Task<bool> UpdateRoleAsync(RoleItem role);
        Task<bool> DeleteRoleAsync(int id);
    }
}

