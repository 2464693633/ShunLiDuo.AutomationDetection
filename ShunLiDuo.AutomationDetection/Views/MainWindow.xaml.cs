using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Prism.Regions;
using ShunLiDuo.AutomationDetection.Services;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private readonly IRegionManager _regionManager;

        public MainWindow(IRegionManager regionManager, ICurrentUserService currentUserService, Services.IAccountService accountService)
        {
            InitializeComponent();
            _regionManager = regionManager;
            var viewModel = new ViewModels.MainWindowViewModel(regionManager, currentUserService, accountService);
            DataContext = viewModel;
            InitializeTimer();
            
            // 确保任务管理界面立即加载
            Loaded += (s, e) =>
            {
                if (viewModel.CurrentMainView == "TaskManagementView")
                {
                    regionManager.RequestNavigate("MainContentRegion", "TaskManagementView");
                }
                UpdateMaximizeButtonContent();
            };
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = System.TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateTime();
            _timer.Start();
            UpdateTime();
        }

        private void UpdateTime()
        {
            if (CurrentTimeText != null)
            {
                CurrentTimeText.Text = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        // 窗口控制方法
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
            UpdateMaximizeButtonContent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // 窗口拖拽
        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        // 更新最大化按钮图标
        private void UpdateMaximizeButtonContent()
        {
            if (MaximizeIcon != null)
            {
                if (WindowState == WindowState.Maximized)
                {
                    MaximizeIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.WindowRestore;
                    MaximizeRestoreButton.ToolTip = "还原";
                }
                else
                {
                    MaximizeIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.WindowMaximize;
                    MaximizeRestoreButton.ToolTip = "最大化";
                }
            }
        }
    }
}
