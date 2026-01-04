using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class TaskManagementView : UserControl
    {
        public TaskManagementView()
        {
            InitializeComponent();
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

