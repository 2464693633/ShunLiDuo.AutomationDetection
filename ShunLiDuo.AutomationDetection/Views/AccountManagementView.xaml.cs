using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.ViewModels;
using ShunLiDuo.AutomationDetection.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class AccountManagementView : UserControl
    {
        private readonly IAccountService _accountService;
        private readonly IRoleService _roleService;
        private readonly AccountManagementViewModel _viewModel;

        public AccountManagementView(
            IAccountService accountService, 
            IRoleService roleService,
            ICurrentUserService currentUserService)
        {
            InitializeComponent();
            _accountService = accountService;
            _roleService = roleService;
            _viewModel = new AccountManagementViewModel(accountService, roleService, currentUserService);
            DataContext = _viewModel;
            
            // 订阅ViewModel的事件
            _viewModel.EditRequested += (s, e) => Edit_Click(null, null);
            _viewModel.DeleteRequested += (s, e) => Delete_Click(null, null);
            _viewModel.ViewRequested += (s, e) => View_Click(null, null);
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            var userItem = _viewModel.SelectedItem;
            if (userItem == null)
            {
                MessageBox.Show("请选择要编辑的账户", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var account = await _accountService.GetAccountByIdAsync(userItem.Id);
            if (account == null)
            {
                MessageBox.Show("账户不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new AddAccountDialog(_roleService, account);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                var updatedAccount = new UserItem
                {
                    Id = account.Id,
                    AccountNo = dialog.AccountNo ?? string.Empty,
                    LoginAccount = dialog.LoginAccount ?? string.Empty,
                    Password = dialog.Password ?? string.Empty,
                    Name = dialog.Name ?? string.Empty,
                    Gender = dialog.Gender ?? string.Empty,
                    Phone = dialog.Phone ?? string.Empty,
                    EmployeeNo = dialog.EmployeeNo ?? string.Empty,
                    RoleId = dialog.RoleId,
                    Remark = dialog.Remark ?? string.Empty
                };

                var success = await _accountService.UpdateAccountAsync(updatedAccount);
                if (success)
                {
                    MessageBox.Show("账户更新成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    _viewModel.LoadAccountsAsync();
                }
                else
                {
                    MessageBox.Show("账户更新失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            var userItem = _viewModel.SelectedItem;
            if (userItem == null)
            {
                MessageBox.Show("请选择要删除的账户", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"确定要删除账户 '{userItem.Name}' 吗？", "确认删除", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                var success = await _accountService.DeleteAccountAsync(userItem.Id);
                if (success)
                {
                    MessageBox.Show("账户删除成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    _viewModel.LoadAccountsAsync();
                }
                else
                {
                    MessageBox.Show("账户删除失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void View_Click(object sender, RoutedEventArgs e)
        {
            var userItem = _viewModel.SelectedItem;
            if (userItem == null)
            {
                MessageBox.Show("请选择要查看的账户", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var account = await _accountService.GetAccountByIdAsync(userItem.Id);
            if (account == null)
            {
                MessageBox.Show("账户不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var details = new List<ViewDetailDialog.DetailItem>
            {
                new ViewDetailDialog.DetailItem { Label = "账户编号", Value = account.AccountNo ?? "" },
                new ViewDetailDialog.DetailItem { Label = "登录账户", Value = account.LoginAccount ?? "" },
                new ViewDetailDialog.DetailItem { Label = "账户密码", Value = account.Password ?? "" },
                new ViewDetailDialog.DetailItem { Label = "姓名", Value = account.Name ?? "" },
                new ViewDetailDialog.DetailItem { Label = "性别", Value = account.Gender ?? "" },
                new ViewDetailDialog.DetailItem { Label = "电话", Value = account.Phone ?? "" },
                new ViewDetailDialog.DetailItem { Label = "工号", Value = account.EmployeeNo ?? "" },
                new ViewDetailDialog.DetailItem { Label = "角色", Value = account.Role ?? "" },
                new ViewDetailDialog.DetailItem { Label = "备注", Value = account.Remark ?? "" }
            };

            var dialog = new ViewDetailDialog("账户详情", details);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

    }
}

