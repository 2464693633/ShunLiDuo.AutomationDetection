using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Services;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class AccountManagementViewModel : BindableBase
    {
        private string _searchKeyword;
        private ObservableCollection<UserItem> _users;
        private UserItem _selectedItem;
        private int _totalCount;
        private bool _isLoading;
        private readonly IAccountService _accountService;
        private readonly IRoleService _roleService;
        private readonly ICurrentUserService _currentUserService;
        
        // 权限属性
        private bool _canAdd;
        private bool _canEdit;
        private bool _canDelete;
        private bool _canView;
        private HashSet<string> _userPermissions = new HashSet<string>();

        public AccountManagementViewModel(
            IAccountService accountService, 
            IRoleService roleService,
            ICurrentUserService currentUserService)
        {
            _accountService = accountService;
            _roleService = roleService;
            _currentUserService = currentUserService;
            Users = new ObservableCollection<UserItem>();
            SearchCommand = new DelegateCommand(OnSearch, () => !IsLoading);
            AddCommand = new DelegateCommand(OnAdd, () => !IsLoading);
            EditCommand = new DelegateCommand(OnEdit, () => !IsLoading && SelectedItem != null);
            DeleteCommand = new DelegateCommand(OnDelete, () => !IsLoading && SelectedItem != null);
            ViewCommand = new DelegateCommand(OnView, () => !IsLoading && SelectedItem != null);
            LoadPermissionsAsync();
            LoadAccountsAsync();
        }

        private async void LoadPermissionsAsync()
        {
            if (_currentUserService?.CurrentUser == null)
            {
                CanAdd = false;
                CanEdit = false;
                CanDelete = false;
                CanView = false;
                return;
            }

            try
            {
                var permissionsString = await _accountService.GetAccountPermissionsAsync(_currentUserService.CurrentUser.Id);
                _userPermissions.Clear();

                if (!string.IsNullOrWhiteSpace(permissionsString))
                {
                    var permissions = permissionsString.Split(',');
                    foreach (var perm in permissions)
                    {
                        var trimmedPerm = perm.Trim();
                        if (!string.IsNullOrEmpty(trimmedPerm))
                        {
                            _userPermissions.Add(trimmedPerm);
                        }
                    }
                }

                // 检查权限
                CanAdd = HasPermission("AccountManagement.Add");
                CanEdit = HasPermission("AccountManagement.Edit");
                CanDelete = HasPermission("AccountManagement.Delete");
                CanView = HasPermission("AccountManagement.View");
            }
            catch
            {
                CanAdd = false;
                CanEdit = false;
                CanDelete = false;
                CanView = false;
            }
        }

        private bool HasPermission(string permissionCode)
        {
            if (string.IsNullOrEmpty(permissionCode))
                return false;

            // 检查是否有精确匹配的权限
            if (_userPermissions.Contains(permissionCode))
                return true;

            // 检查是否有模块权限（例如：AccountManagement 包含 AccountManagement.Add）
            return _userPermissions.Any(p => p.StartsWith(permissionCode + ".") || p == permissionCode);
        }

        public async void LoadAccountsAsync()
        {
            IsLoading = true;
            try
            {
                var accounts = await _accountService.GetAllAccountsAsync();
                Users.Clear();
                foreach (var account in accounts)
                {
                    Users.Add(account);
                }
                TotalCount = Users.Count;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"加载账户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnSearch()
        {
            IsLoading = true;
            try
            {
                var accounts = await _accountService.SearchAccountsAsync(SearchKeyword ?? string.Empty);
                Users.Clear();
                foreach (var account in accounts)
                {
                    Users.Add(account);
                }
                TotalCount = Users.Count;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"搜索账户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnAdd()
        {
            var dialog = new Views.AddAccountDialog(_roleService);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                try
                {
                    var newAccount = new UserItem
                    {
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

                    var success = await _accountService.AddAccountAsync(newAccount);
                    if (success)
                    {
                        MessageBox.Show("账户添加成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadAccountsAsync();
                    }
                    else
                    {
                        MessageBox.Show("账户添加失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"添加账户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        public ObservableCollection<UserItem> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                SearchCommand.RaiseCanExecuteChanged();
                AddCommand.RaiseCanExecuteChanged();
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                ViewCommand.RaiseCanExecuteChanged();
            }
        }

        public UserItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                ViewCommand.RaiseCanExecuteChanged();
            }
        }

        public event EventHandler EditRequested;
        public event EventHandler DeleteRequested;
        public event EventHandler ViewRequested;

        private void OnEdit()
        {
            if (SelectedItem != null)
            {
                EditRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnDelete()
        {
            if (SelectedItem != null)
            {
                DeleteRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnView()
        {
            if (SelectedItem != null)
            {
                ViewRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        // 权限可见性属性
        public bool CanAdd
        {
            get => _canAdd;
            set => SetProperty(ref _canAdd, value);
        }

        public bool CanEdit
        {
            get => _canEdit;
            set => SetProperty(ref _canEdit, value);
        }

        public bool CanDelete
        {
            get => _canDelete;
            set => SetProperty(ref _canDelete, value);
        }

        public bool CanView
        {
            get => _canView;
            set => SetProperty(ref _canView, value);
        }

        public DelegateCommand SearchCommand { get; private set; }
        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand EditCommand { get; private set; }
        public DelegateCommand DeleteCommand { get; private set; }
        public DelegateCommand ViewCommand { get; private set; }
    }
}

