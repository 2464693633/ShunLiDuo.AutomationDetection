using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ShunLiDuo.AutomationDetection.Data;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _repository;
        private readonly IPermissionRepository _permissionRepository;

        public RoleService(IRoleRepository repository, IPermissionRepository permissionRepository)
        {
            _repository = repository;
            _permissionRepository = permissionRepository;
        }

        public async Task<List<RoleItem>> GetAllRolesAsync()
        {
            return await _repository.GetAllRolesAsync();
        }

        public async Task<List<RoleItem>> SearchRolesAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return await GetAllRolesAsync();
            }
            return await _repository.SearchRolesAsync(keyword);
        }

        public async Task<RoleItem> GetRoleByIdAsync(int id)
        {
            return await _repository.GetRoleByIdAsync(id);
        }

        public async Task<bool> AddRoleAsync(RoleItem role)
        {
            if (role == null || string.IsNullOrWhiteSpace(role.RoleName))
            {
                return false;
            }

            // 如果没有角色编号，自动生成
            if (string.IsNullOrWhiteSpace(role.RoleNo))
            {
                var allRoles = await GetAllRolesAsync();
                role.RoleNo = $"ROLE{(allRoles.Count + 1):D4}";
            }

            var id = await _repository.InsertRoleAsync(role);
            if (id > 0)
            {
                role.Id = id;
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateRoleAsync(RoleItem role)
        {
            if (role == null || role.Id <= 0 || string.IsNullOrWhiteSpace(role.RoleName))
            {
                return false;
            }

            // 如果角色是管理员，确保其拥有所有权限
            if (role.RoleName == "管理员")
            {
                var allPermissions = await _permissionRepository.GetAllPermissionsAsync();
                var allPermissionCodes = allPermissions.Select(p => p.Code).ToList();
                role.Permissions = string.Join(",", allPermissionCodes);
            }

            return await _repository.UpdateRoleAsync(role);
        }

        public async Task<bool> DeleteRoleAsync(int id)
        {
            if (id <= 0)
            {
                return false;
            }

            return await _repository.DeleteRoleAsync(id);
        }
    }
}

