using System;
using System.Windows;
using Prism.DryIoc;
using Prism.Ioc;
using ShunLiDuo.AutomationDetection.Views;
using ShunLiDuo.AutomationDetection.Data;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.Models;
using S7.Net;
using ShunLiDuo.AutomationDetection.ViewModels;

namespace ShunLiDuo.AutomationDetection
{
    public partial class App : PrismApplication
    {
        private ApiHostService _apiHostService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 处理未捕获的异常，静默处理PLC连接异常
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // 如果是PLC连接相关的异常，静默处理
            if (IsPlcConnectionException(e.Exception))
            {
                System.Diagnostics.Debug.WriteLine($"PLC连接异常（已静默处理）: {e.Exception.Message}");
                e.Handled = true; // 标记为已处理，不显示错误对话框
                return;
            }
            
            // 其他异常可以正常处理或显示
            e.Handled = false;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex && IsPlcConnectionException(ex))
            {
                System.Diagnostics.Debug.WriteLine($"PLC连接异常（已静默处理）: {ex.Message}");
                return;
            }
        }

        private bool IsPlcConnectionException(Exception ex)
        {
            if (ex == null) return false;
            
            // 检查是否是S7.Net的PLC异常
            if (ex is S7.Net.PlcException)
                return true;
            
            // 检查异常消息中是否包含连接相关的关键词
            string message = ex.Message ?? string.Empty;
            if (message.Contains("127.0.0.1") || 
                message.Contains("无法连接") || 
                message.Contains("积极拒绝") ||
                message.Contains("actively refused") ||
                message.Contains("Couldn't establish the connection"))
                return true;
            
            // 检查内部异常
            if (ex.InnerException != null)
                return IsPlcConnectionException(ex.InnerException);
            
            return false;
        }

        protected override Window CreateShell()
        {
            // 初始化数据库
            InitializeDatabase();
            
            // 创建主窗口但先隐藏
            var mainWindow = Container.Resolve<MainWindow>();
            mainWindow.Visibility = Visibility.Hidden;
            
            // 显示登录窗口
            var loginWindow = Container.Resolve<Views.LoginWindow>();
            var loginResult = loginWindow.ShowDialog();
            
            if (loginResult == true && loginWindow.CurrentUser != null)
            {
                // 登录成功，保存当前用户信息
                var currentUserService = Container.Resolve<ICurrentUserService>();
                currentUserService.CurrentUser = loginWindow.CurrentUser;
                
                // 刷新主窗口的用户信息显示和权限
                if (mainWindow.DataContext is ViewModels.MainWindowViewModel viewModel)
                {
                    viewModel.RefreshUserInfo();
                }
                
                // 自动连接PLC（如果启用了自动连接）
                AutoConnectPlcAsync();
                
                // 自动连接所有检测室的串口
                AutoConnectAllScannersAsync();
                
                // 启动API服务器（在登录成功后）
                try
                {
                    _apiHostService = new ApiHostService(Container, "http://localhost:8080");
                    _apiHostService.Start();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"API服务器启动失败: {ex.Message}");
                }
                
                // 显示主窗口
                mainWindow.Visibility = Visibility.Visible;
                mainWindow.Show();
                return mainWindow;
            }
            else
            {
                // 登录取消或失败，关闭应用
                Current.Shutdown();
                return null;
            }
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
            containerRegistry.Register<IPermissionRepository, PermissionRepository>();
            containerRegistry.Register<IPlcMonitorConfigRepository, PlcMonitorConfigRepository>();
            containerRegistry.Register<IDetectionLogRepository, DetectionLogRepository>();
            containerRegistry.Register<IAlarmRecordRepository, AlarmRecordRepository>();
            
            // 注册服务层
            containerRegistry.Register<IRuleService, RuleService>();
            containerRegistry.Register<IDetectionRoomService, DetectionRoomService>();
            containerRegistry.Register<ILogisticsBoxService, LogisticsBoxService>();
            containerRegistry.Register<IAccountService, AccountService>();
            containerRegistry.Register<IRoleService, RoleService>();
            containerRegistry.Register<IPermissionService, PermissionService>();
            containerRegistry.RegisterSingleton<ICurrentUserService, CurrentUserService>();
            containerRegistry.RegisterSingleton<IS7CommunicationService, S7CommunicationService>();
            containerRegistry.RegisterSingleton<IScannerCommunicationService, ScannerCommunicationService>();
            containerRegistry.Register<ICommunicationConfigService, CommunicationConfigService>();
            containerRegistry.Register<IPlcMonitorConfigService, PlcMonitorConfigService>();
            containerRegistry.Register<IDetectionLogService, DetectionLogService>();
            containerRegistry.Register<IAlarmRecordService, AlarmRecordService>();
            
            // 注册登录窗口
            containerRegistry.Register<Views.LoginWindow>();
            
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
            
            // 注册通讯设置视图
            containerRegistry.Register<Views.CommunicationSettingsView>();
            containerRegistry.RegisterForNavigation<Views.CommunicationSettingsView>("CommunicationSettingsView");
            
            // 注册其他视图
            containerRegistry.RegisterForNavigation<TaskManagementView>("TaskManagementView");
            containerRegistry.RegisterForNavigation<DeviceAlarmView>("DeviceAlarmView");
            
            // 注册检测历史记录视图
            containerRegistry.Register<Views.DetectionLogView>();
            containerRegistry.RegisterForNavigation<Views.DetectionLogView>("DetectionLogView");
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
                CustomMessageBox.ShowError($"数据库初始化失败: {ex.Message}");
            }
        }

        private async void AutoConnectPlcAsync()
        {
            try
            {
                var configService = Container.Resolve<ICommunicationConfigService>();
                var s7Service = Container.Resolve<IS7CommunicationService>();
                
                // 加载保存的配置
                var config = await configService.GetConfigAsync();
                
                // 如果启用了自动连接，则自动连接
                if (config.AutoConnect && !string.IsNullOrWhiteSpace(config.IpAddress))
                {
                    // 延迟一下，确保UI已经加载完成
                    await System.Threading.Tasks.Task.Delay(1000);
                    
                    // 执行自动连接
                    bool connected = await s7Service.ConnectAsync(
                        config.IpAddress, 
                        config.CpuType, 
                        config.Rack, 
                        config.Slot);
                    
                    if (!connected)
                    {
                        // 连接失败，但不显示错误消息（避免启动时弹出太多提示）
                        System.Diagnostics.Debug.WriteLine($"PLC自动连接失败: {s7Service.ConnectionStatus}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                // 自动连接失败，但不影响应用启动
                System.Diagnostics.Debug.WriteLine($"PLC自动连接异常: {ex.Message}");
            }
        }

        private async void AutoConnectAllScannersAsync()
        {
            try
            {
                var detectionRoomService = Container.Resolve<IDetectionRoomService>();
                var scannerService = Container.Resolve<IScannerCommunicationService>();
                
                // 延迟一下，确保UI已经加载完成
                await System.Threading.Tasks.Task.Delay(1500);
                
                // 加载所有检测室
                var rooms = await detectionRoomService.GetAllRoomsAsync();
                
                if (rooms != null && rooms.Count > 0)
                {
                    // 自动连接所有已启用扫码器的检测室
                    await scannerService.AutoConnectAllScannersAsync(rooms);
                }
            }
            catch (System.Exception ex)
            {
                // 自动连接失败，但不影响应用启动
                System.Diagnostics.Debug.WriteLine($"串口自动连接异常: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 停止API服务器
            _apiHostService?.Stop();
            base.OnExit(e);
        }
    }
}
