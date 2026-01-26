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
            try
            {
                InitializeComponent();

            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[严重错误] TaskManagementView 初始化失败: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[内部错误] {ex.InnerException.Message}");
                }
                MessageBox.Show($"界面加载失败: {ex.Message}\n请联系管理员。", "系统错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    // [修复] 报工单输入框的回车/扫码确认，只应该触发"更新报工单"，不应该触发"创建新任务"
                    // 原来的逻辑会导致扫描报工单时，错误地再次使用当前物流盒号创建任务
                    
                    if (!string.IsNullOrWhiteSpace(viewModel.WorkOrderNo))
                    {
                        viewModel.UpdateWorkOrderNoForLatestTask(viewModel.WorkOrderNo);
                    }
                    else if (sender is TextBox tb && !string.IsNullOrWhiteSpace(tb.Text))
                    {
                         viewModel.UpdateWorkOrderNoForLatestTask(tb.Text);
                    }
                    
                    // 清空输入框（可选，取决于ViewModel是否清空了）
                    viewModel.WorkOrderNo = string.Empty; 
                    e.Handled = true;
                }
            }
        }


        private void InspectorName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                 // 送检人输入确认，ViewModel中已通过Binding更新
                 if (DataContext is ViewModels.TaskManagementViewModel viewModel)
                 {
                     // 手动触发更新最新任务逻辑（使用当前输入框的值）
                     viewModel.UpdateInspectorForLatestTask(viewModel.InspectorInputText);
                     
                     // 清空输入框（但ViewModel内部已经缓存了LastInspectorName用于后续任务）
                     viewModel.InspectorInputText = string.Empty;
                 }
                 e.Handled = true;
            }
        }
    }
}

