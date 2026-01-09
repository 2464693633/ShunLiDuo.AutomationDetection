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
using ShunLiDuo.AutomationDetection.Views;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class RuleManagementViewModel : BindableBase
    {
        private string _searchKeyword;
        private ObservableCollection<RuleItem> _rules;
        private RuleItem _selectedItem;
        private int _totalCount;
        private bool _isLoading;
        private readonly IRuleService _ruleService;
        private readonly IDetectionRoomService _detectionRoomService;
        private readonly ILogisticsBoxService _logisticsBoxService;
        private readonly IAccountService _accountService;
        private readonly ICurrentUserService _currentUserService;
        
        // 权限属性
        private bool _canAdd;
        private bool _canEdit;
        private bool _canDelete;
        private bool _canView;
        private HashSet<string> _userPermissions = new HashSet<string>();

        public RuleManagementViewModel(
            IRuleService ruleService, 
            IDetectionRoomService detectionRoomService, 
            ILogisticsBoxService logisticsBoxService,
            IAccountService accountService,
            ICurrentUserService currentUserService)
        {
            _ruleService = ruleService;
            _detectionRoomService = detectionRoomService;
            _logisticsBoxService = logisticsBoxService;
            _accountService = accountService;
            _currentUserService = currentUserService;
            Rules = new ObservableCollection<RuleItem>();
            SearchCommand = new DelegateCommand(OnSearch, () => !IsLoading);
            AddCommand = new DelegateCommand(OnAdd, () => !IsLoading);
            EditCommand = new DelegateCommand(OnEdit, () => !IsLoading && SelectedItem != null);
            DeleteCommand = new DelegateCommand(OnDelete, () => !IsLoading && SelectedItem != null);
            ViewCommand = new DelegateCommand(OnView, () => !IsLoading && SelectedItem != null);
            LoadPermissionsAsync();
            LoadRulesAsync();
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
                CanAdd = HasPermission("RuleManagement.Add");
                CanEdit = HasPermission("RuleManagement.Edit");
                CanDelete = HasPermission("RuleManagement.Delete");
                CanView = HasPermission("RuleManagement.View");
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

            // 管理员拥有全部权限
            if (_currentUserService?.CurrentUser != null && _currentUserService.CurrentUser.Role == "管理员")
                return true;

            // 检查是否有精确匹配的权限
            if (_userPermissions.Contains(permissionCode))
                return true;

            // 检查是否有模块权限（例如：RuleManagement 包含 RuleManagement.Add）
            return _userPermissions.Any(p => p.StartsWith(permissionCode + ".") || p == permissionCode);
        }

        public async void LoadRulesAsync()
        {
            IsLoading = true;
            try
            {
                var rules = await _ruleService.GetAllRulesAsync();
                Rules.Clear();
                foreach (var rule in rules)
                {
                    Rules.Add(rule);
                }
                TotalCount = Rules.Count;
            }
            catch (System.Exception ex)
            {
                CustomMessageBox.ShowError($"加载规则失败: {ex.Message}");
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
                var rules = await _ruleService.SearchRulesAsync(SearchKeyword ?? string.Empty);
                Rules.Clear();
                foreach (var rule in rules)
                {
                    Rules.Add(rule);
                }
                TotalCount = Rules.Count;
            }
            catch (System.Exception ex)
            {
                CustomMessageBox.ShowError($"搜索规则失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnAdd()
        {
            var dialog = new Views.AddRuleDialog();
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            // 从数据库加载检测室和物流盒数据
            try
            {
                var rooms = await _detectionRoomService.GetAllRoomsAsync();
                var boxes = await _logisticsBoxService.GetAllBoxesAsync();
                
                dialog.ViewModel.LoadDetectionRooms(new ObservableCollection<Models.DetectionRoomItem>(rooms));
                dialog.ViewModel.LoadLogisticsBoxes(new ObservableCollection<Models.LogisticsBoxItem>(boxes));
            }
            catch (System.Exception ex)
            {
                CustomMessageBox.ShowError($"加载数据失败: {ex.Message}");
                return;
            }
            
            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                try
                {
                    var newRule = new RuleItem
                    {
                        RuleNo = dialog.RuleNo ?? string.Empty,
                        RuleName = dialog.RuleName ?? string.Empty,
                        DetectionRooms = dialog.SelectedDetectionRooms ?? string.Empty,
                        LogisticsBoxNos = dialog.SelectedLogisticsBoxNos ?? string.Empty,
                        Remark = dialog.Remark ?? string.Empty,
                        IsSelected = false
                    };

                    var success = await _ruleService.AddRuleAsync(newRule);
                    if (success)
                    {
                        CustomMessageBox.ShowInformation("规则添加成功");
                        LoadRulesAsync();
                    }
                    else
                    {
                        CustomMessageBox.ShowError("规则添加失败");
                    }
                }
                catch (System.Exception ex)
                {
                    CustomMessageBox.ShowError($"添加规则失败: {ex.Message}");
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

        public ObservableCollection<RuleItem> Rules
        {
            get => _rules;
            set => SetProperty(ref _rules, value);
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

        public RuleItem SelectedItem
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

