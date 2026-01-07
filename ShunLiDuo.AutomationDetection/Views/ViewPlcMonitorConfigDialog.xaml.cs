using System.Windows;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class ViewPlcMonitorConfigDialog : Window
    {
        private readonly Models.PlcMonitorConfigItem _config;

        public ViewPlcMonitorConfigDialog(Models.PlcMonitorConfigItem config)
        {
            InitializeComponent();
            _config = config;
            LoadConfigData();
        }

        private void LoadConfigData()
        {
            RoomNameTextBlock.Text = _config.RoomName ?? string.Empty;
            
            Cylinder1NameTextBlock.Text = _config.Cylinder1Name ?? "未配置";
            Cylinder1ExtendAddressTextBlock.Text = _config.Cylinder1ExtendAddress ?? "未配置";
            Cylinder1RetractAddressTextBlock.Text = _config.Cylinder1RetractAddress ?? "未配置";
            Cylinder1ExtendFeedbackAddressTextBlock.Text = _config.Cylinder1ExtendFeedbackAddress ?? "未配置";
            Cylinder1RetractFeedbackAddressTextBlock.Text = _config.Cylinder1RetractFeedbackAddress ?? "未配置";
            Cylinder1DataTypeTextBlock.Text = _config.Cylinder1DataType ?? "Bool";
            
            Cylinder2NameTextBlock.Text = _config.Cylinder2Name ?? "未配置";
            Cylinder2ExtendAddressTextBlock.Text = _config.Cylinder2ExtendAddress ?? "未配置";
            Cylinder2RetractAddressTextBlock.Text = _config.Cylinder2RetractAddress ?? "未配置";
            Cylinder2ExtendFeedbackAddressTextBlock.Text = _config.Cylinder2ExtendFeedbackAddress ?? "未配置";
            Cylinder2RetractFeedbackAddressTextBlock.Text = _config.Cylinder2RetractFeedbackAddress ?? "未配置";
            Cylinder2DataTypeTextBlock.Text = _config.Cylinder2DataType ?? "Bool";
            
            SensorNameTextBlock.Text = _config.SensorName ?? "未配置";
            SensorAddressTextBlock.Text = _config.SensorAddress ?? "未配置";
            SensorDataTypeTextBlock.Text = _config.SensorDataType ?? "Bool";
            
            RemarkTextBlock.Text = _config.Remark ?? string.Empty;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

