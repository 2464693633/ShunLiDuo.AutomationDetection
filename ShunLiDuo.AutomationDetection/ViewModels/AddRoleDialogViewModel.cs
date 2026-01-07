using Prism.Mvvm;
using Prism.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Services;

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
                System.Windows.MessageBox.Show($"加载权限失败: {ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                            // 直接设置字段值，避免触发属性变更通知
                            var isSelected = selectedCodes.Contains(child.Code);
                            if (child.IsSelected != isSelected)
                            {
                                child.IsSelected = isSelected;
                            }
                        }
                    }
                }
            }
            
            // 然后设置父权限的选择状态
            foreach (var permission in Permissions)
            {
                if (permission == null) continue;
                
                var isSelected = selectedCodes.Contains(permission.Code);
                if (permission.IsSelected != isSelected)
                {
                    permission.IsSelected = isSelected;
                }
                
                // 最后更新父权限的选择状态（这会根据子权限的状态更新父权限）
                if (permission.Children != null && permission.Children.Count > 0)
                {
                    permission.UpdateSelectionState();
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
            foreach (var permission in Permissions)
            {
                permission.IsSelected = true;
            }
        }

        private void OnDeselectAll()
        {
            foreach (var permission in Permissions)
            {
                permission.IsSelected = false;
            }
        }

        public DelegateCommand SelectAllCommand { get; private set; }
        public DelegateCommand DeselectAllCommand { get; private set; }
    }
}

