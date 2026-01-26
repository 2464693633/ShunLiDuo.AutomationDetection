using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using Prism.Regions;
using ShunLiDuo.AutomationDetection.Services;
using System.Windows.Controls;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private readonly IRegionManager _regionManager;

        public MainWindow(IRegionManager regionManager, ICurrentUserService currentUserService, Services.IAccountService accountService, IS7CommunicationService s7Service)
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
                    regionManager.RequestNavigate("MainContentRegion", "TaskManagementView", result =>
                    {
                        if (result.Error != null)
                        {
                            MessageBox.Show($"导航到任务管理页面失败: {result.Error.Message}", "导航错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    });
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
        
        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is ViewModels.OpenTabItem tab)
            {
                var viewModel = DataContext as ViewModels.MainWindowViewModel;
                viewModel?.CloseTab(tab);
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
            if (MaximizeRestoreButton != null)
            {
                if (WindowState == WindowState.Maximized)
                {
                    MaximizeRestoreButton.Content = "❐";
                    MaximizeRestoreButton.ToolTip = "还原";
                }
                else
                {
                    MaximizeRestoreButton.Content = "□";
                    MaximizeRestoreButton.ToolTip = "最大化";
                }
            }
        }
    }
}
