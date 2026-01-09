using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.ViewModels;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Views;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class RoleManagementView : UserControl
    {
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;
        private readonly RoleManagementViewModel _viewModel;

        public RoleManagementView(
            IRoleService roleService, 
            IPermissionService permissionService,
            IAccountService accountService,
            ICurrentUserService currentUserService)
        {
            InitializeComponent();
            _roleService = roleService;
            _permissionService = permissionService;
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

            var details = new List<ViewDetailDialog.DetailItem>
            {
                new ViewDetailDialog.DetailItem { Label = "角色编号", Value = role.RoleNo ?? "" },
                new ViewDetailDialog.DetailItem { Label = "角色名称", Value = role.RoleName ?? "" },
                new ViewDetailDialog.DetailItem { Label = "备注", Value = role.Remark ?? "" },
                new ViewDetailDialog.DetailItem { Label = "权限", Value = role.Permissions ?? "" }
            };

            var dialog = new ViewDetailDialog("角色详情", details);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

    }
}

