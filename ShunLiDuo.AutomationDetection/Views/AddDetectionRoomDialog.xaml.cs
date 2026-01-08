using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Media;
using ShunLiDuo.AutomationDetection.ViewModels;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class AddDetectionRoomDialog : Window
    {
        public AddDetectionRoomDialogViewModel ViewModel { get; private set; }
        public bool IsEditMode { get; private set; }
        public int RoomId { get; private set; }

        public AddDetectionRoomDialog(Models.DetectionRoomItem room = null)
        {
            InitializeComponent();
            IsEditMode = room != null;
            ViewModel = new AddDetectionRoomDialogViewModel(room);
            DataContext = ViewModel;
            
            if (IsEditMode)
            {
                Title = "编辑检测室";
                RoomId = room.Id;
                Loaded += (s, e) => TitleTextBlock.Text = "编辑检测室";
            }
            else
            {
                Title = "新增检测室";
                Loaded += (s, e) => TitleTextBlock.Text = "新增检测室";
            }
        }

        public string RoomNo => ViewModel.RoomNo;
        public string RoomName => ViewModel.RoomName;
        public string Remark => ViewModel.Remark;
        
        // 串口配置属性
        public string ScannerPortName => ViewModel.ScannerPortName;
        public int ScannerBaudRate => ViewModel.ScannerBaudRate;
        public int ScannerDataBits => ViewModel.ScannerDataBits;
        public int ScannerStopBits => ViewModel.ScannerStopBits;
        public string ScannerParity => ViewModel.ScannerParity;
        public bool ScannerIsEnabled => ViewModel.ScannerIsEnabled;
        
        // PLC配置属性 - 气缸1
        public string Cylinder1ExtendAddress => ViewModel.Cylinder1ExtendAddress;
        public string Cylinder1RetractAddress => ViewModel.Cylinder1RetractAddress;
        public string Cylinder1ExtendFeedbackAddress => ViewModel.Cylinder1ExtendFeedbackAddress;
        public string Cylinder1RetractFeedbackAddress => ViewModel.Cylinder1RetractFeedbackAddress;
        public string Cylinder1DataType => ViewModel.Cylinder1DataType;
        
        // PLC配置属性 - 气缸2
        public string Cylinder2ExtendAddress => ViewModel.Cylinder2ExtendAddress;
        public string Cylinder2RetractAddress => ViewModel.Cylinder2RetractAddress;
        public string Cylinder2ExtendFeedbackAddress => ViewModel.Cylinder2ExtendFeedbackAddress;
        public string Cylinder2RetractFeedbackAddress => ViewModel.Cylinder2RetractFeedbackAddress;
        public string Cylinder2DataType => ViewModel.Cylinder2DataType;
        
        // PLC配置属性 - 传感器
        public string SensorAddress => ViewModel.SensorAddress;
        public string SensorDataType => ViewModel.SensorDataType;
        
        // 反馈报警延时时间设置
        public int PushCylinderRetractTimeout => ViewModel.PushCylinderRetractTimeout;
        public int PushCylinderExtendTimeout => ViewModel.PushCylinderExtendTimeout;
        public int BlockingCylinderRetractTimeout => ViewModel.BlockingCylinderRetractTimeout;
        public int BlockingCylinderExtendTimeout => ViewModel.BlockingCylinderExtendTimeout;
        public int SensorDetectTimeout => ViewModel.SensorDetectTimeout;
        public int PassageDelayTime => ViewModel.PassageDelayTime;
        public int SensorConfirmDelayTime => ViewModel.SensorConfirmDelayTime;

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            // 在关闭前，确保所有绑定值都已更新到ViewModel
            // 移除焦点以触发当前编辑的TextBox的绑定更新
            var focusedElement = Keyboard.FocusedElement;
            if (focusedElement != null)
            {
                Keyboard.ClearFocus();
            }
            
            // 遍历所有TextBox并强制更新绑定
            UpdateAllBindings(this);
            
            // 添加调试日志，检查 ViewModel 的值
            System.Diagnostics.Debug.WriteLine($"[对话框] 确认按钮点击 - 推箱气缸收缩超时: {ViewModel.PushCylinderRetractTimeout}");
            System.Diagnostics.Debug.WriteLine($"[对话框] 确认按钮点击 - 推箱气缸伸出超时: {ViewModel.PushCylinderExtendTimeout}");
            System.Diagnostics.Debug.WriteLine($"[对话框] 确认按钮点击 - 阻挡气缸收缩超时: {ViewModel.BlockingCylinderRetractTimeout}");
            System.Diagnostics.Debug.WriteLine($"[对话框] 确认按钮点击 - 阻挡气缸伸出超时: {ViewModel.BlockingCylinderExtendTimeout}");
            
            DialogResult = true;
            Close();
        }
        
        private void UpdateAllBindings(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is TextBox textBox)
                {
                    var binding = BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty);
                    if (binding != null)
                    {
                        // 检查绑定路径，如果是超时时间相关的，添加调试日志
                        var bindingPath = binding.ParentBinding?.Path?.Path ?? "";
                        if (bindingPath.Contains("Timeout") || bindingPath.Contains("Delay"))
                        {
                            System.Diagnostics.Debug.WriteLine($"[绑定更新] TextBox 路径: {bindingPath}, 当前Text值: {textBox.Text}");
                        }
                        
                        // 强制更新绑定
                        binding.UpdateSource();
                        
                        // 检查是否有验证错误
                        if (binding.HasError)
                        {
                            System.Diagnostics.Debug.WriteLine($"[绑定更新] 绑定错误 - 路径: {bindingPath}, 错误: {binding.ValidationError?.ErrorContent}");
                        }
                    }
                }
                
                UpdateAllBindings(child);
            }
        }
    }
}

