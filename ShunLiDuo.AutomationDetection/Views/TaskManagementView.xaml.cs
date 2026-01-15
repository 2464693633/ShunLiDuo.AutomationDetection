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

        private void WorkOrderNo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is ViewModels.TaskManagementViewModel viewModel)
                {
                    // 如果物流盒编码已输入，则触发任务创建或更新
                    if (!string.IsNullOrWhiteSpace(viewModel.LogisticsBoxCode))
                    {
                        viewModel.AddLogisticsBoxCode(viewModel.LogisticsBoxCode);
                        e.Handled = true;
                    }
                    else
                    {
                        // 如果物流盒编码为空，尝试更新已存在任务的报工单编号
                        viewModel.UpdateWorkOrderNoForLatestTask();
                        e.Handled = true;
                    }
                }
            }
        }
    }
}

