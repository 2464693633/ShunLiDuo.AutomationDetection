using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAccountService _accountService;
        private string _currentMainView = "TaskManagementView";
        private bool _isLeftNavVisible = false;
        private string _currentBottomNav = "TaskManagement";
        private string _currentLeftNav = "";
        private HashSet<string> _userPermissions = new HashSet<string>();
        
        // 权限可见性属性
        private bool _canAccessRuleManagement = true;
        private bool _canAccessSystemSettings = true;
        private bool _canAccessLogisticsBox = true;
        private bool _canAccessDetectionRoom = true;
        private bool _canAccessRuleManagementMenu = true;
        private bool _canAccessAccountManagement = true;
        private bool _canAccessRoleManagement = true;

        public MainWindowViewModel(IRegionManager regionManager, ICurrentUserService currentUserService, IAccountService accountService)
        {
            _regionManager = regionManager;
            _currentUserService = currentUserService;
            _accountService = accountService;
            // 初始化时设置为任务管理，不显示侧栏
            CurrentMainView = "TaskManagementView";
            IsLeftNavVisible = false;
            CurrentBottomNav = "TaskManagement";
            CurrentLeftNav = "";
            InitializeCommands();
            // 默认显示任务管理
            NavigateTo("TaskManagementView");
            
            // 监听用户信息变化
            if (_currentUserService is System.ComponentModel.INotifyPropertyChanged notifyService)
            {
                notifyService.PropertyChanged += async (s, e) =>
                {
                    if (e.PropertyName == nameof(ICurrentUserService.CurrentUser))
                    {
                        RaisePropertyChanged(nameof(CurrentUserName));
                        RaisePropertyChanged(nameof(CurrentUserEmployeeNo));
                        RaisePropertyChanged(nameof(CurrentUserLoginAccount));
                        // 用户信息变化时，重新加载权限
                        await LoadUserPermissionsAsync();
                    }
                };
            }
        }
        
        public async void RefreshUserInfo()
        {
            RaisePropertyChanged(nameof(CurrentUserName));
            RaisePropertyChanged(nameof(CurrentUserEmployeeNo));
            RaisePropertyChanged(nameof(CurrentUserLoginAccount));
            // 刷新用户信息时，重新加载权限
            await LoadUserPermissionsAsync();
            // 登录成功后，默认显示任务管理页面
            CurrentMainView = "TaskManagementView";
            IsLeftNavVisible = false;
            CurrentBottomNav = "TaskManagement";
            CurrentLeftNav = "";
            NavigateTo("TaskManagementView");
        }
        
        public async Task LoadUserPermissionsAsync()
        {
            if (_currentUserService?.CurrentUser == null)
            {
                // 没有用户，隐藏所有需要权限的功能
                CanAccessRuleManagement = false;
                CanAccessSystemSettings = false;
                CanAccessLogisticsBox = false;
                CanAccessDetectionRoom = false;
                CanAccessRuleManagementMenu = false;
                CanAccessAccountManagement = false;
                CanAccessRoleManagement = false;
                return;
            }
            
            try
            {
                // 获取用户权限
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
                
                // 更新权限可见性
                // 调度规则管理：需要 RuleManagement 权限
                CanAccessRuleManagement = HasPermission("RuleManagement");
                
                // 系统设置：需要 AccountManagement 或 RoleManagement 权限
                CanAccessSystemSettings = HasPermission("AccountManagement") || HasPermission("RoleManagement");
                
                // 物流盒管理：需要 LogisticsBoxManagement 权限
                CanAccessLogisticsBox = HasPermission("LogisticsBoxManagement");
                
                // 检测室管理：需要 DetectionRoomManagement 权限
                CanAccessDetectionRoom = HasPermission("DetectionRoomManagement");
                
                // 规则管理菜单：需要 RuleManagement 权限
                CanAccessRuleManagementMenu = HasPermission("RuleManagement");
                
                // 账户管理：需要 AccountManagement 权限
                CanAccessAccountManagement = HasPermission("AccountManagement");
                
                // 角色管理：需要 RoleManagement 权限
                CanAccessRoleManagement = HasPermission("RoleManagement");
            }
            catch
            {
                // 加载权限失败，默认隐藏所有需要权限的功能
                CanAccessRuleManagement = false;
                CanAccessSystemSettings = false;
                CanAccessLogisticsBox = false;
                CanAccessDetectionRoom = false;
                CanAccessRuleManagementMenu = false;
                CanAccessAccountManagement = false;
                CanAccessRoleManagement = false;
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

        private void InitializeCommands()
        {
            NavigateToTaskCommand = new DelegateCommand(() => 
            {
                CurrentMainView = "TaskManagementView";
                IsLeftNavVisible = false;
                CurrentBottomNav = "TaskManagement";
                CurrentLeftNav = "";
                NavigateTo("TaskManagementView");
            });
            
            NavigateToRuleCommand = new DelegateCommand(() => 
            {
                CurrentMainView = "RuleManagementView";
                IsLeftNavVisible = true;
                CurrentBottomNav = "RuleManagement";
                CurrentLeftNav = "LogisticsBox";
                NavigateTo("LogisticsBoxManagementView"); // 默认显示物流盒管理
            });
            
            NavigateToAlarmCommand = new DelegateCommand(() => 
            {
                CurrentMainView = "AlarmView";
                IsLeftNavVisible = true;
                CurrentBottomNav = "Alarm";
                CurrentLeftNav = "DeviceException";
                NavigateTo("DeviceExceptionView");
            });
            
            NavigateToSystemSettingsCommand = new DelegateCommand(() => 
            {
                CurrentMainView = "SystemSettingsView";
                IsLeftNavVisible = true;
                CurrentBottomNav = "SystemSettings";
                CurrentLeftNav = "AccountManagement";
                NavigateTo("AccountManagementView");
            });

            // 侧栏菜单命令
            NavigateToLogisticsBoxCommand = new DelegateCommand(() => 
            {
                CurrentLeftNav = "LogisticsBox";
                NavigateTo("LogisticsBoxManagementView");
            });
            NavigateToDetectionRoomCommand = new DelegateCommand(() => 
            {
                CurrentLeftNav = "DetectionRoom";
                NavigateTo("DetectionRoomManagementView");
            });
            NavigateToRuleManagementCommand = new DelegateCommand(() => 
            {
                CurrentLeftNav = "RuleManagement";
                NavigateTo("RuleManagementView");
            });
            NavigateToDeviceExceptionCommand = new DelegateCommand(() => 
            {
                CurrentLeftNav = "DeviceException";
                NavigateTo("DeviceExceptionView");
            });
            NavigateToDeviceAlarmCommand = new DelegateCommand(() => 
            {
                CurrentLeftNav = "DeviceAlarm";
                NavigateTo("DeviceAlarmView");
            });
            NavigateToAccountManagementCommand = new DelegateCommand(() => 
            {
                CurrentLeftNav = "AccountManagement";
                NavigateTo("AccountManagementView");
            });
            NavigateToRoleManagementCommand = new DelegateCommand(() => 
            {
                CurrentLeftNav = "RoleManagement";
                NavigateTo("RoleManagementView");
            });
            NavigateToCommunicationSettingsCommand = new DelegateCommand(() => 
            {
                CurrentLeftNav = "CommunicationSettings";
                NavigateTo("CommunicationSettingsView");
            });
        }

        private void NavigateTo(string viewName)
        {
            _regionManager.RequestNavigate("MainContentRegion", viewName);
        }

        public string CurrentMainView
        {
            get => _currentMainView;
            set => SetProperty(ref _currentMainView, value);
        }

        public bool IsLeftNavVisible
        {
            get => _isLeftNavVisible;
            set => SetProperty(ref _isLeftNavVisible, value);
        }

        public string CurrentBottomNav
        {
            get => _currentBottomNav;
            set => SetProperty(ref _currentBottomNav, value);
        }

        public string CurrentLeftNav
        {
            get => _currentLeftNav;
            set => SetProperty(ref _currentLeftNav, value);
        }

        // 当前用户信息
        public string CurrentUserName
        {
            get => _currentUserService?.CurrentUser?.Name ?? "未登录";
        }

        public string CurrentUserEmployeeNo
        {
            get => _currentUserService?.CurrentUser?.EmployeeNo ?? "";
        }

        public string CurrentUserLoginAccount
        {
            get => _currentUserService?.CurrentUser?.LoginAccount ?? "";
        }

        // 底部导航命令
        public DelegateCommand NavigateToTaskCommand { get; private set; }
        public DelegateCommand NavigateToRuleCommand { get; private set; }
        public DelegateCommand NavigateToAlarmCommand { get; private set; }
        public DelegateCommand NavigateToSystemSettingsCommand { get; private set; }

        // 侧栏菜单命令
        public DelegateCommand NavigateToLogisticsBoxCommand { get; private set; }
        public DelegateCommand NavigateToDetectionRoomCommand { get; private set; }
        public DelegateCommand NavigateToRuleManagementCommand { get; private set; }
        public DelegateCommand NavigateToDeviceExceptionCommand { get; private set; }
        public DelegateCommand NavigateToDeviceAlarmCommand { get; private set; }
        public DelegateCommand NavigateToAccountManagementCommand { get; private set; }
        public DelegateCommand NavigateToRoleManagementCommand { get; private set; }
        public DelegateCommand NavigateToCommunicationSettingsCommand { get; private set; }
        
        // 权限可见性属性
        public bool CanAccessRuleManagement
        {
            get => _canAccessRuleManagement;
            set => SetProperty(ref _canAccessRuleManagement, value);
        }
        
        public bool CanAccessSystemSettings
        {
            get => _canAccessSystemSettings;
            set => SetProperty(ref _canAccessSystemSettings, value);
        }
        
        public bool CanAccessLogisticsBox
        {
            get => _canAccessLogisticsBox;
            set => SetProperty(ref _canAccessLogisticsBox, value);
        }
        
        public bool CanAccessDetectionRoom
        {
            get => _canAccessDetectionRoom;
            set => SetProperty(ref _canAccessDetectionRoom, value);
        }
        
        public bool CanAccessRuleManagementMenu
        {
            get => _canAccessRuleManagementMenu;
            set => SetProperty(ref _canAccessRuleManagementMenu, value);
        }
        
        public bool CanAccessAccountManagement
        {
            get => _canAccessAccountManagement;
            set => SetProperty(ref _canAccessAccountManagement, value);
        }
        
        public bool CanAccessRoleManagement
        {
            get => _canAccessRoleManagement;
            set => SetProperty(ref _canAccessRoleManagement, value);
        }
    }
}

