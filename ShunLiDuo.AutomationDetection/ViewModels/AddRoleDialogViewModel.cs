using Prism.Mvvm;
using Prism.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class AddRoleDialogViewModel : BindableBase
    {
        private string _roleName;
        private string _remark;
        private ObservableCollection<PermissionItem> _permissions;
        private bool _isValid;

        public AddRoleDialogViewModel()
        {
            InitializePermissions();
            SelectAllCommand = new DelegateCommand(OnSelectAll);
            DeselectAllCommand = new DelegateCommand(OnDeselectAll);
        }

        public AddRoleDialogViewModel(string existingPermissions) : this()
        {
            LoadPermissions(existingPermissions);
        }

        private void InitializePermissions()
        {
            Permissions = new ObservableCollection<PermissionItem>
            {
                new PermissionItem
                {
                    Name = "任务管理",
                    Code = "TaskManagement",
                    Children = new ObservableCollection<PermissionItem>
                    {
                        new PermissionItem { Name = "新增", Code = "TaskManagement.Add" },
                        new PermissionItem { Name = "编辑", Code = "TaskManagement.Edit" },
                        new PermissionItem { Name = "删除", Code = "TaskManagement.Delete" },
                        new PermissionItem { Name = "查看", Code = "TaskManagement.View" }
                    }
                },
                new PermissionItem
                {
                    Name = "调度规则管理",
                    Code = "RuleManagement",
                    Children = new ObservableCollection<PermissionItem>
                    {
                        new PermissionItem { Name = "新增", Code = "RuleManagement.Add" },
                        new PermissionItem { Name = "编辑", Code = "RuleManagement.Edit" },
                        new PermissionItem { Name = "删除", Code = "RuleManagement.Delete" },
                        new PermissionItem { Name = "查看", Code = "RuleManagement.View" }
                    }
                },
                new PermissionItem
                {
                    Name = "物流盒管理",
                    Code = "LogisticsBoxManagement",
                    Children = new ObservableCollection<PermissionItem>
                    {
                        new PermissionItem { Name = "新增", Code = "LogisticsBoxManagement.Add" },
                        new PermissionItem { Name = "编辑", Code = "LogisticsBoxManagement.Edit" },
                        new PermissionItem { Name = "删除", Code = "LogisticsBoxManagement.Delete" },
                        new PermissionItem { Name = "查看", Code = "LogisticsBoxManagement.View" }
                    }
                },
                new PermissionItem
                {
                    Name = "检测室管理",
                    Code = "DetectionRoomManagement",
                    Children = new ObservableCollection<PermissionItem>
                    {
                        new PermissionItem { Name = "新增", Code = "DetectionRoomManagement.Add" },
                        new PermissionItem { Name = "编辑", Code = "DetectionRoomManagement.Edit" },
                        new PermissionItem { Name = "删除", Code = "DetectionRoomManagement.Delete" },
                        new PermissionItem { Name = "查看", Code = "DetectionRoomManagement.View" }
                    }
                },
                new PermissionItem
                {
                    Name = "账户管理",
                    Code = "AccountManagement",
                    Children = new ObservableCollection<PermissionItem>
                    {
                        new PermissionItem { Name = "新增", Code = "AccountManagement.Add" },
                        new PermissionItem { Name = "编辑", Code = "AccountManagement.Edit" },
                        new PermissionItem { Name = "删除", Code = "AccountManagement.Delete" },
                        new PermissionItem { Name = "查看", Code = "AccountManagement.View" }
                    }
                },
                new PermissionItem
                {
                    Name = "角色管理",
                    Code = "RoleManagement",
                    Children = new ObservableCollection<PermissionItem>
                    {
                        new PermissionItem { Name = "新增", Code = "RoleManagement.Add" },
                        new PermissionItem { Name = "编辑", Code = "RoleManagement.Edit" },
                        new PermissionItem { Name = "删除", Code = "RoleManagement.Delete" },
                        new PermissionItem { Name = "查看", Code = "RoleManagement.View" }
                    }
                }
            };
        }

        private void LoadPermissions(string permissionsString)
        {
            if (string.IsNullOrWhiteSpace(permissionsString))
                return;

            var selectedCodes = permissionsString.Split(',').Select(s => s.Trim()).ToHashSet();
            
            foreach (var permission in Permissions)
            {
                permission.IsSelected = selectedCodes.Contains(permission.Code);
                foreach (var child in permission.Children)
                {
                    child.IsSelected = selectedCodes.Contains(child.Code);
                }
                permission.UpdateSelectionState();
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
                // 如果父权限被选中，添加父权限代码
                if (permission.IsSelected && !permission.IsIndeterminate)
                {
                    selectedPermissions.Add(permission.Code);
                }
                // 如果父权限是半选状态，只添加选中的子权限
                else if (permission.IsIndeterminate || !permission.IsSelected)
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

