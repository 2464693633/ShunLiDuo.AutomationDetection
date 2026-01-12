using Prism.Mvvm;
using Prism.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.Views;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class AddRoleDialogViewModel : BindableBase
    {
        private string _roleName;
        private string _remark;
        private ObservableCollection<PermissionItem> _permissions;
        private bool _isValid;
        private readonly IPermissionService _permissionService;

        public AddRoleDialogViewModel(IPermissionService permissionService)
        {
            _permissionService = permissionService;
            Permissions = new ObservableCollection<PermissionItem>();
            SelectAllCommand = new DelegateCommand(OnSelectAll);
            DeselectAllCommand = new DelegateCommand(OnDeselectAll);
            LoadPermissionsAsync();
        }

        public AddRoleDialogViewModel(IPermissionService permissionService, string existingPermissions) : this(permissionService)
        {
            LoadPermissions(existingPermissions);
        }

        private string _pendingPermissionsString;

        public AddRoleDialogViewModel(IPermissionService permissionService, Models.RoleItem role) : this(permissionService)
        {
            if (role != null)
            {
                RoleName = role.RoleName ?? string.Empty;
                Remark = role.Remark ?? string.Empty;
                _pendingPermissionsString = role.Permissions ?? string.Empty;
            }
        }

        private async void LoadPermissionsAsync()
        {
            try
            {
                var permissionTree = await _permissionService.GetPermissionTreeAsync();
                Permissions.Clear();
                foreach (var permission in permissionTree)
                {
                    Permissions.Add(permission);
                }
                
                // 如果有待加载的权限字符串，现在加载它
                if (!string.IsNullOrWhiteSpace(_pendingPermissionsString))
                {
                    LoadPermissions(_pendingPermissionsString);
                    _pendingPermissionsString = null;
                }
            }
            catch (System.Exception ex)
            {
                CustomMessageBox.ShowError($"加载权限失败: {ex.Message}");
            }
        }

        private void LoadPermissions(string permissionsString)
        {
            if (string.IsNullOrWhiteSpace(permissionsString))
                return;

            // 确保 Permissions 集合已经加载
            if (Permissions == null || Permissions.Count == 0)
            {
                _pendingPermissionsString = permissionsString;
                return;
            }

            var selectedCodes = permissionsString.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToHashSet();
            
            // 先设置所有子权限的选择状态，避免触发父权限更新
            foreach (var permission in Permissions)
            {
                if (permission == null) continue;
                
                if (permission.Children != null && permission.Children.Count > 0)
                {
                    foreach (var child in permission.Children)
                    {
                        if (child != null)
                        {
                            var isSelected = selectedCodes.Contains(child.Code);
                            if (child.IsSelected != isSelected)
                            {
                                child.IsSelected = isSelected;
                            }
                        }
                    }
                }
            }
            
            // 然后更新父权限的选择状态（根据子权限状态自动更新）
            foreach (var permission in Permissions)
            {
                if (permission == null) continue;
                
                // 只更新有子权限的父权限状态，根据子权限状态自动计算
                if (permission.Children != null && permission.Children.Count > 0)
                {
                    permission.UpdateSelectionState();
                }
                else
                {
                    // 如果没有子权限，直接检查父权限的 Code
                    var isSelected = selectedCodes.Contains(permission.Code);
                    if (permission.IsSelected != isSelected)
                    {
                        permission.IsSelected = isSelected;
                    }
                }
            }
        }

        public string RoleName
        {
            get => _roleName;
            set
            {
                SetProperty(ref _roleName, value);
                Validate();
            }
        }

        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        public ObservableCollection<PermissionItem> Permissions
        {
            get => _permissions;
            set => SetProperty(ref _permissions, value);
        }

        public bool IsValid
        {
            get => _isValid;
            set => SetProperty(ref _isValid, value);
        }

        private void Validate()
        {
            IsValid = !string.IsNullOrWhiteSpace(RoleName);
        }

        public string GetSelectedPermissions()
        {
            var selectedPermissions = new List<string>();
            
            foreach (var permission in Permissions)
            {
                // 如果父权限被选中且不是半选状态，添加所有子权限代码
                if (permission.IsSelected && !permission.IsIndeterminate)
                {
                    // 当父权限被选中时，所有子权限也应该被选中（根据 PermissionItem 的逻辑）
                    // 所以添加所有子权限的代码
                    if (permission.Children != null && permission.Children.Count > 0)
                    {
                        foreach (var child in permission.Children)
                        {
                            selectedPermissions.Add(child.Code);
                        }
                    }
                    else
                    {
                        // 如果没有子权限，添加父权限代码
                        selectedPermissions.Add(permission.Code);
                    }
                }
                // 如果父权限是半选状态或未选中，只添加选中的子权限
                else
                {
                    if (permission.Children != null && permission.Children.Count > 0)
                    {
                        foreach (var child in permission.Children)
                        {
                            if (child.IsSelected)
                            {
                                selectedPermissions.Add(child.Code);
                            }
                        }
                    }
                }
            }
            
            return string.Join(",", selectedPermissions);
        }

        private void OnSelectAll()
        {
            // 先设置所有子权限，避免触发父权限的状态更新冲突
            foreach (var permission in Permissions)
            {
                if (permission.Children != null && permission.Children.Count > 0)
                {
                    foreach (var child in permission.Children)
                    {
                        child.IsSelected = true;
                    }
                }
            }
            
            // 然后设置父权限（这会根据子权限状态自动更新父权限状态）
            foreach (var permission in Permissions)
            {
                if (permission.Children != null && permission.Children.Count > 0)
                {
                    permission.UpdateSelectionState();
                }
                else
                {
                    // 如果没有子权限，直接设置父权限
                    permission.IsSelected = true;
                }
            }
        }

        private void OnDeselectAll()
        {
            // 先设置所有子权限，避免触发父权限的状态更新冲突
            foreach (var permission in Permissions)
            {
                if (permission.Children != null && permission.Children.Count > 0)
                {
                    foreach (var child in permission.Children)
                    {
                        child.IsSelected = false;
                    }
                }
            }
            
            // 然后设置父权限（这会根据子权限状态自动更新父权限状态）
            foreach (var permission in Permissions)
            {
                if (permission.Children != null && permission.Children.Count > 0)
                {
                    permission.UpdateSelectionState();
                }
                else
                {
                    // 如果没有子权限，直接设置父权限
                    permission.IsSelected = false;
                }
            }
        }

        public DelegateCommand SelectAllCommand { get; private set; }
        public DelegateCommand DeselectAllCommand { get; private set; }
    }
}

