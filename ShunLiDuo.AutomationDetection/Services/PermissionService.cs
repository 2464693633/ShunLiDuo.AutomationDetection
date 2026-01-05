using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Data;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _repository;

        public PermissionService(IPermissionRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<PermissionItem>> GetPermissionTreeAsync()
        {
            var allPermissions = await _repository.GetAllPermissionsAsync();
            
            // 获取所有模块根权限（ParentId为null）
            var moduleRoots = allPermissions.Where(p => p.ParentId == null).OrderBy(p => p.SortOrder).ToList();
            
            var permissionTree = new List<PermissionItem>();
            
            foreach (var moduleRoot in moduleRoots)
            {
                var permissionItem = new PermissionItem
                {
                    Code = moduleRoot.Code,
                    Name = moduleRoot.Name,
                    Children = new ObservableCollection<PermissionItem>()
                };
                
                // 获取该模块的所有子权限
                var childPermissions = allPermissions
                    .Where(p => p.ParentId == moduleRoot.Id)
                    .OrderBy(p => p.SortOrder)
                    .ToList();
                
                foreach (var childPerm in childPermissions)
                {
                    permissionItem.Children.Add(new PermissionItem
                    {
                        Code = childPerm.Code,
                        Name = childPerm.Name,
                        Parent = permissionItem
                    });
                }
                
                permissionTree.Add(permissionItem);
            }
            
            return permissionTree;
        }
    }
}

