using System.Windows;
using System.Windows.Threading;
using Prism.Regions;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private readonly IRegionManager _regionManager;

        public MainWindow(IRegionManager regionManager)
        {
            InitializeComponent();
            _regionManager = regionManager;
            var viewModel = new ViewModels.MainWindowViewModel(regionManager);
            DataContext = viewModel;
            InitializeTimer();
            
            // 确保任务管理界面立即加载
            Loaded += (s, e) =>
            {
                if (viewModel.CurrentMainView == "TaskManagementView")
                {
                    regionManager.RequestNavigate("MainContentRegion", "TaskManagementView");
                }
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
    }
}
