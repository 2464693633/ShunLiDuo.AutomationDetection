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
    public class RoleManagementViewModel : BindableBase
    {
        private string _searchKeyword;
        private ObservableCollection<RoleItem> _roles;
        private RoleItem _selectedItem;
        private int _totalCount;
        private bool _isLoading;
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;
        private readonly IAccountService _accountService;
        private readonly ICurrentUserService _currentUserService;
        
        // 权限属性
        private bool _canAdd;
        private bool _canEdit;
        private bool _canDelete;
        private bool _canView;
        private HashSet<string> _userPermissions = new HashSet<string>();

        public RoleManagementViewModel(
            IRoleService roleService, 
            IPermissionService permissionService,
            IAccountService accountService,
            ICurrentUserService currentUserService)
        {
            _roleService = roleService;
            _permissionService = permissionService;
            _accountService = accountService;
            _currentUserService = currentUserService;
            Roles = new ObservableCollection<RoleItem>();
            SearchCommand = new DelegateCommand(OnSearch, () => !IsLoading);
            AddCommand = new DelegateCommand(OnAdd, () => !IsLoading);
            EditCommand = new DelegateCommand(OnEdit, () => !IsLoading && SelectedItem != null);
            DeleteCommand = new DelegateCommand(OnDelete, () => !IsLoading && SelectedItem != null);
            ViewCommand = new DelegateCommand(OnView, () => !IsLoading && SelectedItem != null);
            LoadPermissionsAsync();
            LoadRolesAsync();
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
                CanAdd = HasPermission("RoleManagement.Add");
                CanEdit = HasPermission("RoleManagement.Edit");
                CanDelete = HasPermission("RoleManagement.Delete");
                CanView = HasPermission("RoleManagement.View");
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

            // 检查是否有模块权限（例如：RoleManagement 包含 RoleManagement.Add）
            return _userPermissions.Any(p => p.StartsWith(permissionCode + ".") || p == permissionCode);
        }

        public async void LoadRolesAsync()
        {
            IsLoading = true;
            try
            {
                var roles = await _roleService.GetAllRolesAsync();
                Roles.Clear();
                foreach (var role in roles)
                {
                    Roles.Add(role);
                }
                TotalCount = Roles.Count;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"加载角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var roles = await _roleService.SearchRolesAsync(SearchKeyword ?? string.Empty);
                Roles.Clear();
                foreach (var role in roles)
                {
                    Roles.Add(role);
                }
                TotalCount = Roles.Count;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"搜索角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnAdd()
        {
            var dialog = new Views.AddRoleDialog(_permissionService);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                try
                {
                    var newRole = new RoleItem
                    {
                        RoleName = dialog.RoleName ?? string.Empty,
                        Remark = dialog.Remark ?? string.Empty,
                        Permissions = dialog.Permissions ?? string.Empty
                    };

                    var success = await _roleService.AddRoleAsync(newRole);
                    if (success)
                    {
                        MessageBox.Show("角色添加成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadRolesAsync();
                    }
                    else
                    {
                        MessageBox.Show("角色添加失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"添加角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        public ObservableCollection<RoleItem> Roles
        {
            get => _roles;
            set => SetProperty(ref _roles, value);
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

        public RoleItem SelectedItem
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

