using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private string _currentMainView = "TaskManagementView";
        private bool _isLeftNavVisible = false;

        public MainWindowViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            // 初始化时设置为任务管理，不显示侧栏
            CurrentMainView = "TaskManagementView";
            IsLeftNavVisible = false;
            InitializeCommands();
            // 默认显示任务管理
            NavigateTo("TaskManagementView");
        }

        private void InitializeCommands()
        {
            NavigateToTaskCommand = new DelegateCommand(() => 
            {
                CurrentMainView = "TaskManagementView";
                IsLeftNavVisible = false;
                NavigateTo("TaskManagementView");
            });
            
            NavigateToRuleCommand = new DelegateCommand(() => 
            {
                CurrentMainView = "RuleManagementView";
                IsLeftNavVisible = true;
                NavigateTo("LogisticsBoxManagementView"); // 默认显示物流盒管理
            });
            
            NavigateToAlarmCommand = new DelegateCommand(() => 
            {
                CurrentMainView = "AlarmView";
                IsLeftNavVisible = true;
                NavigateTo("DeviceExceptionView");
            });
            
            NavigateToSystemSettingsCommand = new DelegateCommand(() => 
            {
                CurrentMainView = "SystemSettingsView";
                IsLeftNavVisible = true;
                NavigateTo("AccountManagementView");
            });

            // 侧栏菜单命令
            NavigateToLogisticsBoxCommand = new DelegateCommand(() => NavigateTo("LogisticsBoxManagementView"));
            NavigateToDetectionRoomCommand = new DelegateCommand(() => NavigateTo("DetectionRoomManagementView"));
            NavigateToRuleManagementCommand = new DelegateCommand(() => NavigateTo("RuleManagementView"));
            NavigateToDeviceExceptionCommand = new DelegateCommand(() => NavigateTo("DeviceExceptionView"));
            NavigateToDeviceAlarmCommand = new DelegateCommand(() => NavigateTo("DeviceAlarmView"));
            NavigateToAccountManagementCommand = new DelegateCommand(() => NavigateTo("AccountManagementView"));
            NavigateToRoleManagementCommand = new DelegateCommand(() => NavigateTo("RoleManagementView"));
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
    }
}

