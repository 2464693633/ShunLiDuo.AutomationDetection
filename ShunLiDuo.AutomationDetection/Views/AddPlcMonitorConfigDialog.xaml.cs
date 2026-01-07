using System;
using System.Linq;
using System.Windows;
using Prism.DryIoc;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Services;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class AddPlcMonitorConfigDialog : Window
    {
        private readonly IDetectionRoomService _detectionRoomService;
        private readonly IPlcMonitorConfigService _configService;
        private readonly PlcMonitorConfigItem _editConfig;

        public AddPlcMonitorConfigDialog(IDetectionRoomService detectionRoomService, IPlcMonitorConfigService configService, PlcMonitorConfigItem editConfig = null)
        {
            InitializeComponent();
            _detectionRoomService = detectionRoomService;
            _configService = configService;
            _editConfig = editConfig;

            LoadRooms();
            
            if (editConfig != null)
            {
                TitleTextBlock.Text = "编辑PLC监控配置";
                LoadConfigData(editConfig);
            }
            else
            {
                TitleTextBlock.Text = "新增PLC监控配置";
            }
        }

        private async void LoadRooms()
        {
            try
            {
                var rooms = await _detectionRoomService.GetAllRoomsAsync();
                RoomComboBox.ItemsSource = rooms;
                
                if (_editConfig != null && _editConfig.RoomId > 0)
                {
                    RoomComboBox.SelectedValue = _editConfig.RoomId;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载检测室失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadConfigData(PlcMonitorConfigItem config)
        {
            Cylinder1NameTextBox.Text = config.Cylinder1Name ?? string.Empty;
            Cylinder1ExtendAddressTextBox.Text = config.Cylinder1ExtendAddress ?? string.Empty;
            Cylinder1RetractAddressTextBox.Text = config.Cylinder1RetractAddress ?? string.Empty;
            Cylinder1ExtendFeedbackAddressTextBox.Text = config.Cylinder1ExtendFeedbackAddress ?? string.Empty;
            Cylinder1RetractFeedbackAddressTextBox.Text = config.Cylinder1RetractFeedbackAddress ?? string.Empty;
            SetComboBoxSelection(Cylinder1DataTypeComboBox, config.Cylinder1DataType ?? "Bool");
            
            Cylinder2NameTextBox.Text = config.Cylinder2Name ?? string.Empty;
            Cylinder2ExtendAddressTextBox.Text = config.Cylinder2ExtendAddress ?? string.Empty;
            Cylinder2RetractAddressTextBox.Text = config.Cylinder2RetractAddress ?? string.Empty;
            Cylinder2ExtendFeedbackAddressTextBox.Text = config.Cylinder2ExtendFeedbackAddress ?? string.Empty;
            Cylinder2RetractFeedbackAddressTextBox.Text = config.Cylinder2RetractFeedbackAddress ?? string.Empty;
            SetComboBoxSelection(Cylinder2DataTypeComboBox, config.Cylinder2DataType ?? "Bool");
            
            SensorNameTextBox.Text = config.SensorName ?? string.Empty;
            SensorAddressTextBox.Text = config.SensorAddress ?? string.Empty;
            SetComboBoxSelection(SensorDataTypeComboBox, config.SensorDataType ?? "Bool");
            
            RemarkTextBox.Text = config.Remark ?? string.Empty;
        }

        private void SetComboBoxSelection(System.Windows.Controls.ComboBox comboBox, string value)
        {
            foreach (System.Windows.Controls.ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private string GetComboBoxSelection(System.Windows.Controls.ComboBox comboBox)
        {
            if (comboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                return item.Content.ToString();
            }
            return "Bool";
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (RoomComboBox.SelectedValue == null)
            {
                MessageBox.Show("请选择检测室", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var config = new PlcMonitorConfigItem
                {
                    RoomId = (int)RoomComboBox.SelectedValue,
                    Cylinder1Name = Cylinder1NameTextBox.Text?.Trim(),
                    Cylinder1ExtendAddress = Cylinder1ExtendAddressTextBox.Text?.Trim(),
                    Cylinder1RetractAddress = Cylinder1RetractAddressTextBox.Text?.Trim(),
                    Cylinder1ExtendFeedbackAddress = Cylinder1ExtendFeedbackAddressTextBox.Text?.Trim(),
                    Cylinder1RetractFeedbackAddress = Cylinder1RetractFeedbackAddressTextBox.Text?.Trim(),
                    Cylinder1DataType = GetComboBoxSelection(Cylinder1DataTypeComboBox),
                    Cylinder2Name = Cylinder2NameTextBox.Text?.Trim(),
                    Cylinder2ExtendAddress = Cylinder2ExtendAddressTextBox.Text?.Trim(),
                    Cylinder2RetractAddress = Cylinder2RetractAddressTextBox.Text?.Trim(),
                    Cylinder2ExtendFeedbackAddress = Cylinder2ExtendFeedbackAddressTextBox.Text?.Trim(),
                    Cylinder2RetractFeedbackAddress = Cylinder2RetractFeedbackAddressTextBox.Text?.Trim(),
                    Cylinder2DataType = GetComboBoxSelection(Cylinder2DataTypeComboBox),
                    SensorName = SensorNameTextBox.Text?.Trim(),
                    SensorAddress = SensorAddressTextBox.Text?.Trim(),
                    SensorDataType = GetComboBoxSelection(SensorDataTypeComboBox),
                    Remark = RemarkTextBox.Text?.Trim(),
                    CreateTime = DateTime.Now
                };

                if (_editConfig != null)
                {
                    config.Id = _editConfig.Id;
                    config.CreateTime = _editConfig.CreateTime;
                    var success = await _configService.UpdateConfigAsync(config);
                    if (!success)
                    {
                        MessageBox.Show("更新配置失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    await _configService.CreateConfigAsync(config);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

