using System.Windows.Controls;
using System.Windows.Input;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class DeviceAlarmView : UserControl
    {
        public DeviceAlarmView()
        {
            InitializeComponent();
        }

        private void HandleAlarm_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is AlarmRecord alarm)
            {
                if (DataContext is ViewModels.DeviceAlarmViewModel viewModel)
                {
                    viewModel.HandleCommand.Execute(alarm);
                }
            }
        }

        private void DeleteAlarm_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is AlarmRecord alarm)
            {
                if (DataContext is ViewModels.DeviceAlarmViewModel viewModel)
                {
                    viewModel.DeleteCommand.Execute(alarm);
                }
            }
        }
    }
}

