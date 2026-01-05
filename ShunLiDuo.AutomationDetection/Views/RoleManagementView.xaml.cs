using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.ViewModels;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class RoleManagementView : UserControl
    {
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;
        private readonly RoleManagementViewModel _viewModel;

        public RoleManagementView(IRoleService roleService, IPermissionService permissionService)
        {
            InitializeComponent();
            _roleService = roleService;
            _permissionService = permissionService;
            _viewModel = new RoleManagementViewModel(roleService, permissionService);
            DataContext = _viewModel;
        }

        private async void Edit_Click(object sender, MouseButtonEventArgs e)
        {
            var roleItem = GetSelectedRoleItem(sender);
            if (roleItem == null)
            {
                MessageBox.Show("请选择要编辑的角色", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var role = await _roleService.GetRoleByIdAsync(roleItem.Id);
            if (role == null)
            {
                MessageBox.Show("角色不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show("角色更新成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    _viewModel.LoadRolesAsync();
                }
                else
                {
                    MessageBox.Show("角色更新失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void Delete_Click(object sender, MouseButtonEventArgs e)
        {
            var roleItem = GetSelectedRoleItem(sender);
            if (roleItem == null)
            {
                MessageBox.Show("请选择要删除的角色", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"确定要删除角色 '{roleItem.RoleName}' 吗？", "确认删除", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                var success = await _roleService.DeleteRoleAsync(roleItem.Id);
                if (success)
                {
                    MessageBox.Show("角色删除成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    _viewModel.LoadRolesAsync();
                }
                else
                {
                    MessageBox.Show("角色删除失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void View_Click(object sender, MouseButtonEventArgs e)
        {
            var roleItem = GetSelectedRoleItem(sender);
            if (roleItem == null)
            {
                MessageBox.Show("请选择要查看的角色", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var role = await _roleService.GetRoleByIdAsync(roleItem.Id);
            if (role == null)
            {
                MessageBox.Show("角色不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private RoleItem GetSelectedRoleItem(object sender)
        {
            var textBlock = sender as TextBlock;
            if (textBlock == null) return null;

            var dataGridRow = FindParent<DataGridRow>(textBlock);
            if (dataGridRow == null) return null;

            return dataGridRow.Item as RoleItem;
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;
            else
                return FindParent<T>(parentObject);
        }
    }
}

