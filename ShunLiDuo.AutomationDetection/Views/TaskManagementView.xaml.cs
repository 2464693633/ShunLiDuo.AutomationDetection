using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Prism.Regions;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class TaskManagementView : UserControl, INavigationAware
    {
        public TaskManagementView()
        {
            InitializeComponent();
        }
        
        // 实现INavigationAware接口，在导航到该视图时刷新数据
        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 导航到该视图时刷新规则和检测室列表
            if (DataContext is ViewModels.TaskManagementViewModel viewModel)
            {
                await viewModel.RefreshAllDataAsync();
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 导航离开时的处理（如果需要）
        }

        private void ReInspect_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("重新检测功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LogisticsBoxCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = sender as TextBox;
                if (textBox != null && DataContext is ViewModels.TaskManagementViewModel viewModel)
                {
                    viewModel.AddLogisticsBoxCode(textBox.Text);
                    e.Handled = true;
                }
            }
        }
    }
}

