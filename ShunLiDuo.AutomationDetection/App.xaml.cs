using System.Windows;
using Prism.DryIoc;
using Prism.Ioc;
using ShunLiDuo.AutomationDetection.Views;
using ShunLiDuo.AutomationDetection.Data;
using ShunLiDuo.AutomationDetection.Services;

namespace ShunLiDuo.AutomationDetection
{
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            // 初始化数据库
            InitializeDatabase();
            
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册数据库上下文（单例）
            containerRegistry.RegisterSingleton<DatabaseContext>();
            
            // 注册数据访问层
            containerRegistry.Register<IRuleRepository, RuleRepository>();
            containerRegistry.Register<IDetectionRoomRepository, DetectionRoomRepository>();
            containerRegistry.Register<ILogisticsBoxRepository, LogisticsBoxRepository>();
            containerRegistry.Register<IAccountRepository, AccountRepository>();
            containerRegistry.Register<IRoleRepository, RoleRepository>();
            
            // 注册服务层
            containerRegistry.Register<IRuleService, RuleService>();
            containerRegistry.Register<IDetectionRoomService, DetectionRoomService>();
            containerRegistry.Register<ILogisticsBoxService, LogisticsBoxService>();
            containerRegistry.Register<IAccountService, AccountService>();
            containerRegistry.Register<IRoleService, RoleService>();
            
            // 注册主窗口
            containerRegistry.Register<MainWindow>();
            
            // 注册视图（需要注入服务的视图）
            containerRegistry.Register<Views.RuleManagementView>();
            containerRegistry.RegisterForNavigation<Views.RuleManagementView>("RuleManagementView");
            
            // 注册其他视图（需要注入服务的视图）
            containerRegistry.Register<Views.DetectionRoomManagementView>();
            containerRegistry.Register<Views.LogisticsBoxManagementView>();
            containerRegistry.RegisterForNavigation<Views.DetectionRoomManagementView>("DetectionRoomManagementView");
            containerRegistry.RegisterForNavigation<Views.LogisticsBoxManagementView>("LogisticsBoxManagementView");
            
            // 注册其他视图（需要注入服务的视图）
            containerRegistry.Register<Views.AccountManagementView>();
            containerRegistry.RegisterForNavigation<Views.AccountManagementView>("AccountManagementView");
            
            // 注册其他视图（需要注入服务的视图）
            containerRegistry.Register<Views.RoleManagementView>();
            containerRegistry.RegisterForNavigation<Views.RoleManagementView>("RoleManagementView");
            
            // 注册其他视图
            containerRegistry.RegisterForNavigation<TaskManagementView>("TaskManagementView");
            containerRegistry.RegisterForNavigation<DeviceExceptionView>("DeviceExceptionView");
            containerRegistry.RegisterForNavigation<DeviceAlarmView>("DeviceAlarmView");
        }

        protected override void ConfigureModuleCatalog(Prism.Modularity.IModuleCatalog moduleCatalog)
        {
            // 单项目结构，不需要模块
        }

        private void InitializeDatabase()
        {
            try
            {
                using (var dbContext = new DatabaseContext())
                {
                    dbContext.InitializeDatabase();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"数据库初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
