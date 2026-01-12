using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.ViewModels;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Views;
using ShunLiDuo.AutomationDetection.Data;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class RoleManagementView : UserControl
    {
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;
        private readonly IPermissionRepository _permissionRepository;
        private readonly RoleManagementViewModel _viewModel;

        public RoleManagementView(
            IRoleService roleService, 
            IPermissionService permissionService,
            IPermissionRepository permissionRepository,
            IAccountService accountService,
            ICurrentUserService currentUserService)
        {
            InitializeComponent();
            _roleService = roleService;
            _permissionService = permissionService;
            _permissionRepository = permissionRepository;
            _viewModel = new RoleManagementViewModel(roleService, permissionService, accountService, currentUserService);
            DataContext = _viewModel;
            
            // 订阅ViewModel的事件
            _viewModel.EditRequested += (s, e) => Edit_Click(null, null);
            _viewModel.DeleteRequested += (s, e) => Delete_Click(null, null);
            _viewModel.ViewRequested += (s, e) => View_Click(null, null);
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            var roleItem = _viewModel.SelectedItem;
            if (roleItem == null)
            {
                CustomMessageBox.ShowWarning("请选择要编辑的角色");
                return;
            }

            var role = await _roleService.GetRoleByIdAsync(roleItem.Id);
            if (role == null)
            {
                CustomMessageBox.ShowError("角色不存在");
                return;
            }

            var dialog = new AddRoleDialog(_permissionService, role);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                var updatedRole = new RoleItem
                {
                    Id = role.Id,
                    RoleNo = role.RoleNo,
                    RoleName = dialog.ViewModel.RoleName ?? string.Empty,
                    Remark = dialog.ViewModel.Remark ?? string.Empty,
                    Permissions = dialog.ViewModel.GetSelectedPermissions() ?? string.Empty
                };

                var success = await _roleService.UpdateRoleAsync(updatedRole);
                if (success)
                {
                    CustomMessageBox.ShowInformation("角色更新成功");
                    _viewModel.LoadRolesAsync();
                }
                else
                {
                    CustomMessageBox.ShowError("角色更新失败");
                }
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            var roleItem = _viewModel.SelectedItem;
            if (roleItem == null)
            {
                CustomMessageBox.ShowWarning("请选择要删除的角色");
                return;
            }

            // 检查是否为管理员角色，管理员角色不能删除
            if (roleItem.RoleName == "管理员")
            {
                CustomMessageBox.ShowWarning("管理员角色不能删除！");
                return;
            }

            var result = CustomMessageBox.ShowQuestion($"确定要删除角色 '{roleItem.RoleName}' 吗？", "确认删除");
            
            if (result == CustomMessageBoxResult.Yes)
            {
                var success = await _roleService.DeleteRoleAsync(roleItem.Id);
                if (success)
                {
                    CustomMessageBox.ShowInformation("角色删除成功");
                    _viewModel.LoadRolesAsync();
                }
                else
                {
                    CustomMessageBox.ShowError("角色删除失败");
                }
            }
        }

        private async void View_Click(object sender, RoutedEventArgs e)
        {
            var roleItem = _viewModel.SelectedItem;
            if (roleItem == null)
            {
                CustomMessageBox.ShowWarning("请选择要查看的角色");
                return;
            }

            var role = await _roleService.GetRoleByIdAsync(roleItem.Id);
            if (role == null)
            {
                CustomMessageBox.ShowError("角色不存在");
                return;
            }

            // 转换权限代码为易读格式
            string permissionsDisplay = await FormatPermissionsAsync(role.Permissions);

            var details = new List<ViewDetailDialog.DetailItem>
            {
                new ViewDetailDialog.DetailItem { Label = "角色编号", Value = role.RoleNo ?? "" },
                new ViewDetailDialog.DetailItem { Label = "角色名称", Value = role.RoleName ?? "" },
                new ViewDetailDialog.DetailItem { Label = "备注", Value = role.Remark ?? "" },
                new ViewDetailDialog.DetailItem { Label = "权限", Value = permissionsDisplay }
            };

            var dialog = new ViewDetailDialog("角色详情", details);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private async Task<string> FormatPermissionsAsync(string permissionsString)
        {
            if (string.IsNullOrWhiteSpace(permissionsString))
            {
                return "无";
            }

            // 获取所有权限映射
            var allPermissions = await _permissionRepository.GetAllPermissionsAsync();
            var permissionMap = allPermissions.Where(p => !string.IsNullOrEmpty(p.Code))
                .ToDictionary(p => p.Code, p => new { p.Name, p.ModuleName, p.ParentId });

            // 解析权限代码
            var permissionCodes = permissionsString.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            if (permissionCodes.Count == 0)
            {
                return "无";
            }

            // 按模块分组
            var groupedByModule = permissionCodes
                .Select(code =>
                {
                    if (permissionMap.TryGetValue(code, out var perm))
                    {
                        // 如果是模块根权限（ParentId为null），只显示模块名称
                        string displayName = perm.ParentId == null ? perm.ModuleName : perm.Name;
                        return new { Code = code, ModuleName = perm.ModuleName, PermissionName = displayName };
                    }
                    return new { Code = code, ModuleName = "未知模块", PermissionName = code };
                })
                .GroupBy(p => p.ModuleName)
                .OrderBy(g => g.Key)
                .ToList();

            // 构建显示字符串
            var sb = new StringBuilder();
            foreach (var group in groupedByModule)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }
                sb.Append($"{group.Key}：");
                var permissionNames = group.Select(p => p.PermissionName).Distinct().ToList();
                sb.Append(string.Join("、", permissionNames));
            }

            return sb.ToString();
        }

    }
}

