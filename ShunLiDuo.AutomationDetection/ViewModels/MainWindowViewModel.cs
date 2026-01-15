using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private bool _isLeftNavVisible = true; // 默认显示侧栏
        private string _currentBottomNav = "TaskManagement";
        private string _currentLeftNav = "";
        private HashSet<string> _userPermissions = new HashSet<string>();
        
        // 菜单展开状态
        private bool _isRuleManagementExpanded = false;
        private bool _isAlarmExpanded = false;
        private bool _isSystemSettingsExpanded = false;
        
        // 标签页管理
        private ObservableCollection<OpenTabItem> _openTabs = new ObservableCollection<OpenTabItem>();
        private OpenTabItem _selectedTab;
        
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
            // 初始化时设置为任务管理，显示侧栏
            CurrentMainView = "TaskManagementView";
            IsLeftNavVisible = true; // 始终显示侧栏菜单
            CurrentBottomNav = "TaskManagement";
            CurrentLeftNav = "";
            InitializeCommands();
            // 默认显示任务管理
            OpenTab("TaskManagementView", "任务管理");
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
                // 没有用户，所有模块仍然显示
                CanAccessRuleManagement = true;
                CanAccessSystemSettings = true;
                CanAccessLogisticsBox = true;
                CanAccessDetectionRoom = true;
                CanAccessRuleManagementMenu = true;
                CanAccessAccountManagement = true;
                CanAccessRoleManagement = true;
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
                
                // 所有模块都始终显示，不需要权限控制
                // 权限只控制模块内的增删改查按钮
                CanAccessRuleManagement = true;
                CanAccessSystemSettings = true;
                CanAccessLogisticsBox = true;
                CanAccessDetectionRoom = true;
                CanAccessRuleManagementMenu = true;
                CanAccessAccountManagement = true;
                CanAccessRoleManagement = true;
            }
            catch
            {
                // 加载权限失败，所有模块仍然显示
                CanAccessRuleManagement = true;
                CanAccessSystemSettings = true;
                CanAccessLogisticsBox = true;
                CanAccessDetectionRoom = true;
                CanAccessRuleManagementMenu = true;
                CanAccessAccountManagement = true;
                CanAccessRoleManagement = true;
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
            
            // 检查是否有模块权限（例如：LogisticsBoxManagement 包含 LogisticsBoxManagement.Add）
            return _userPermissions.Any(p => p.StartsWith(permissionCode + ".") || p == permissionCode);
        }

        private void InitializeCommands()
        {
            NavigateToTaskCommand = new DelegateCommand(() => 
            {
                CurrentMainView = "TaskManagementView";
                IsLeftNavVisible = true;
                CurrentBottomNav = "TaskManagement";
                CurrentLeftNav = "";
                OpenTab("TaskManagementView", "任务管理");
            });
            
            NavigateToRuleCommand = new DelegateCommand(() => 
            {
                CurrentMainView = "RuleManagementView";
                IsLeftNavVisible = true;
                CurrentBottomNav = "RuleManagement";
                if (!IsRuleManagementExpanded)
                {
                    IsRuleManagementExpanded = true; // 展开调度规则菜单
                }
                CurrentLeftNav = "LogisticsBox";
                OpenTab("LogisticsBoxManagementView", "物流盒管理"); // 默认显示物流盒管理
            });
            
            ToggleRuleManagementExpandedCommand = new DelegateCommand(() => 
            {
                IsRuleManagementExpanded = !IsRuleManagementExpanded;
            });
            
            NavigateToAlarmCommand = new DelegateCommand(() => 
            {
                CurrentMainView = "AlarmView";
                IsLeftNavVisible = true;
                CurrentBottomNav = "Alarm";
                if (!IsAlarmExpanded)
                {
                    IsAlarmExpanded = true; // 展开报警菜单
                }
                CurrentLeftNav = "DeviceAlarm";
                OpenTab("DeviceAlarmView", "设备报警");
            });
            
            ToggleAlarmExpandedCommand = new DelegateCommand(() => 
            {
                IsAlarmExpanded = !IsAlarmExpanded;
            });
            
            NavigateToSystemSettingsCommand = new DelegateCommand(() => 
            {
                CurrentMainView = "SystemSettingsView";
                IsLeftNavVisible = true;
                CurrentBottomNav = "SystemSettings";
                if (!IsSystemSettingsExpanded)
                {
                    IsSystemSettingsExpanded = true; // 展开系统设置菜单
                }
                CurrentLeftNav = "AccountManagement";
                OpenTab("AccountManagementView", "账户管理");
            });
            
            ToggleSystemSettingsExpandedCommand = new DelegateCommand(() => 
            {
                IsSystemSettingsExpanded = !IsSystemSettingsExpanded;
            });

            // 侧栏菜单命令
            NavigateToLogisticsBoxCommand = new DelegateCommand(() => 
            {
                CurrentLeftNav = "LogisticsBox";
                OpenTab("LogisticsBoxManagementView", "物流盒管理");
            });
            NavigateToDetectionRoomCommand = new DelegateCommand(() => 
            {
                CurrentLeftNav = "DetectionRoom";
                OpenTab("DetectionRoomManagementView", "检测室管理");
            });
            NavigateToRuleManagementCommand = new DelegateCommand(() => 
            {
                CurrentLeftNav = "RuleManagement";
                OpenTab("RuleManagementView", "规则管理");
            });
            NavigateToDeviceAlarmCommand = new DelegateCommand(() => 
            {
                CurrentLeftNav = "DeviceAlarm";
                OpenTab("DeviceAlarmView", "设备报警");
            });
            NavigateToAccountManagementCommand = new DelegateCommand(() => 
            {
                CurrentLeftNav = "AccountManagement";
                OpenTab("AccountManagementView", "账户管理");
            });
            NavigateToRoleManagementCommand = new DelegateCommand(() => 
            {
                CurrentLeftNav = "RoleManagement";
                OpenTab("RoleManagementView", "角色管理");
            });
            NavigateToCommunicationSettingsCommand = new DelegateCommand(() => 
            {
                CurrentLeftNav = "CommunicationSettings";
                OpenTab("CommunicationSettingsView", "通讯设置");
            });
            NavigateToDetectionLogCommand = new DelegateCommand(() => 
            {
                CurrentMainView = "DetectionLogView";
                IsLeftNavVisible = true;
                CurrentBottomNav = "DetectionLog";
                CurrentLeftNav = "DetectionLog";
                OpenTab("DetectionLogView", "检测历史");
            });
        }

        private void NavigateTo(string viewName)
        {
            _regionManager.RequestNavigate("MainContentRegion", viewName);
        }
        
        private void OpenTab(string viewName, string title)
        {
            // 检查标签页是否已存在
            var existingTab = _openTabs.FirstOrDefault(t => t.ViewName == viewName);
            if (existingTab != null)
            {
                SelectedTab = existingTab;
                NavigateTo(viewName);
                return;
            }
            
            // 创建新标签页
            var newTab = new OpenTabItem
            {
                ViewName = viewName,
                Title = title
            };
            _openTabs.Add(newTab);
            SelectedTab = newTab;
            NavigateTo(viewName);
        }
        
        public void CloseTab(OpenTabItem tab)
        {
            if (tab == null) return;
            
            int index = _openTabs.IndexOf(tab);
            _openTabs.Remove(tab);
            
            // 如果关闭的是当前选中的标签页，切换到其他标签页
            if (SelectedTab == tab)
            {
                if (_openTabs.Count > 0)
                {
                    // 优先选择下一个，如果没有则选择上一个
                    int newIndex = index < _openTabs.Count ? index : index - 1;
                    SelectedTab = _openTabs[newIndex];
                    NavigateTo(SelectedTab.ViewName);
                }
                else
                {
                    // 如果没有标签页了，打开默认的任务管理
                    OpenTab("TaskManagementView", "任务管理");
                }
            }
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
        public DelegateCommand NavigateToDeviceAlarmCommand { get; private set; }
        public DelegateCommand NavigateToAccountManagementCommand { get; private set; }
        public DelegateCommand NavigateToRoleManagementCommand { get; private set; }
        public DelegateCommand NavigateToCommunicationSettingsCommand { get; private set; }
        public DelegateCommand NavigateToDetectionLogCommand { get; private set; }
        
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
        
        // 菜单展开状态
        public bool IsRuleManagementExpanded
        {
            get => _isRuleManagementExpanded;
            set => SetProperty(ref _isRuleManagementExpanded, value);
        }
        
        public bool IsAlarmExpanded
        {
            get => _isAlarmExpanded;
            set => SetProperty(ref _isAlarmExpanded, value);
        }
        
        public bool IsSystemSettingsExpanded
        {
            get => _isSystemSettingsExpanded;
            set => SetProperty(ref _isSystemSettingsExpanded, value);
        }
        
        // 标签页管理
        public ObservableCollection<OpenTabItem> OpenTabs
        {
            get => _openTabs;
            set => SetProperty(ref _openTabs, value);
        }
        
        public OpenTabItem SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (SetProperty(ref _selectedTab, value) && value != null)
                {
                    NavigateTo(value.ViewName);
                }
            }
        }
        
        // 菜单展开/收起命令
        public DelegateCommand ToggleRuleManagementExpandedCommand { get; private set; }
        public DelegateCommand ToggleAlarmExpandedCommand { get; private set; }
        public DelegateCommand ToggleSystemSettingsExpandedCommand { get; private set; }
    }
    
    // 打开的标签页项
    public class OpenTabItem : BindableBase
    {
        private string _viewName;
        private string _title;
        
        public string ViewName
        {
            get => _viewName;
            set => SetProperty(ref _viewName, value);
        }
        
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
    }
}

