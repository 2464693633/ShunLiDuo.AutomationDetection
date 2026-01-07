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
    public class LogisticsBoxManagementViewModel : BindableBase
    {
        private string _searchKeyword;
        private ObservableCollection<LogisticsBoxItem> _logisticsBoxes;
        private LogisticsBoxItem _selectedItem;
        private int _totalCount;
        private bool _isLoading;
        private readonly ILogisticsBoxService _logisticsBoxService;
        private readonly IAccountService _accountService;
        private readonly ICurrentUserService _currentUserService;
        
        // 权限属性
        private bool _canAdd;
        private bool _canEdit;
        private bool _canDelete;
        private bool _canView;
        private HashSet<string> _userPermissions = new HashSet<string>();

        public LogisticsBoxManagementViewModel(
            ILogisticsBoxService logisticsBoxService,
            IAccountService accountService,
            ICurrentUserService currentUserService)
        {
            _logisticsBoxService = logisticsBoxService;
            _accountService = accountService;
            _currentUserService = currentUserService;
            LogisticsBoxes = new ObservableCollection<LogisticsBoxItem>();
            SearchCommand = new DelegateCommand(OnSearch, () => !IsLoading);
            AddCommand = new DelegateCommand(OnAdd, () => !IsLoading);
            EditCommand = new DelegateCommand(OnEdit, () => !IsLoading && SelectedItem != null);
            DeleteCommand = new DelegateCommand(OnDelete, () => !IsLoading && SelectedItem != null);
            ViewCommand = new DelegateCommand(OnView, () => !IsLoading && SelectedItem != null);
            LoadPermissionsAsync();
            LoadBoxesAsync();
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
                CanAdd = HasPermission("LogisticsBoxManagement.Add");
                CanEdit = HasPermission("LogisticsBoxManagement.Edit");
                CanDelete = HasPermission("LogisticsBoxManagement.Delete");
                CanView = HasPermission("LogisticsBoxManagement.View");
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

            // 检查是否有模块权限（例如：LogisticsBoxManagement 包含 LogisticsBoxManagement.Add）
            return _userPermissions.Any(p => p.StartsWith(permissionCode + ".") || p == permissionCode);
        }

        public async void LoadBoxesAsync()
        {
            IsLoading = true;
            try
            {
                var boxes = await _logisticsBoxService.GetAllBoxesAsync();
                LogisticsBoxes.Clear();
                foreach (var box in boxes)
                {
                    LogisticsBoxes.Add(box);
                }
                TotalCount = LogisticsBoxes.Count;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"加载物流盒失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnSearch()
        {
            // 搜索逻辑
            LoadBoxesAsync();
        }

        private async void OnAdd()
        {
            var dialog = new Views.AddLogisticsBoxDialog();
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                try
                {
                    // 创建新的物流盒
                    var newBox = new LogisticsBoxItem
                    {
                        BoxNo = dialog.BoxNo ?? string.Empty,
                        BoxName = dialog.BoxName ?? string.Empty,
                        Remark = dialog.Remark ?? string.Empty,
                        IsSelected = false
                    };
                    
                    var success = await _logisticsBoxService.AddBoxAsync(newBox);
                    if (success)
                    {
                        MessageBox.Show("物流盒添加成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadBoxesAsync();
                    }
                    else
                    {
                        MessageBox.Show("物流盒添加失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"添加物流盒失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        public ObservableCollection<LogisticsBoxItem> LogisticsBoxes
        {
            get => _logisticsBoxes;
            set => SetProperty(ref _logisticsBoxes, value);
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

        public LogisticsBoxItem SelectedItem
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

