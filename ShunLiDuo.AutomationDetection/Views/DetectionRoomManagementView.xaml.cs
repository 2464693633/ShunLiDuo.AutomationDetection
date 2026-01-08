using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.ViewModels;
using ShunLiDuo.AutomationDetection.Models;
using Prism.Commands;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class DetectionRoomManagementView : UserControl
    {
        private readonly IDetectionRoomService _detectionRoomService;
        private readonly DetectionRoomManagementViewModel _viewModel;

        public DetectionRoomManagementView(
            IDetectionRoomService detectionRoomService,
            IAccountService accountService,
            ICurrentUserService currentUserService,
            IScannerCommunicationService scannerService)
        {
            InitializeComponent();
            _detectionRoomService = detectionRoomService;
            _viewModel = new DetectionRoomManagementViewModel(detectionRoomService, accountService, currentUserService, scannerService);
            DataContext = _viewModel;
            
            // 订阅ViewModel的事件
            _viewModel.EditRequested += (s, e) => Edit_Click(null, null);
            _viewModel.DeleteRequested += (s, e) => Delete_Click(null, null);
            _viewModel.ViewRequested += (s, e) => View_Click(null, null);
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            var roomItem = _viewModel.SelectedItem;
            if (roomItem == null)
            {
                MessageBox.Show("请选择要编辑的检测室", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var room = await _detectionRoomService.GetRoomByIdAsync(roomItem.Id);
            if (room == null)
            {
                MessageBox.Show("检测室不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new AddDetectionRoomDialog(room);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                var updatedRoom = new DetectionRoomItem
                {
                    Id = room.Id,
                    RoomNo = dialog.ViewModel.RoomNo ?? string.Empty,
                    RoomName = dialog.ViewModel.RoomName ?? string.Empty,
                    Remark = dialog.ViewModel.Remark ?? string.Empty,
                    ScannerPortName = dialog.ViewModel.ScannerPortName ?? string.Empty,
                    ScannerBaudRate = dialog.ViewModel.ScannerBaudRate,
                    ScannerDataBits = dialog.ViewModel.ScannerDataBits,
                    ScannerStopBits = dialog.ViewModel.ScannerStopBits,
                    ScannerParity = dialog.ViewModel.ScannerParity ?? "None",
                    ScannerIsEnabled = dialog.ViewModel.ScannerIsEnabled,
                    // PLC配置 - 气缸1
                    Cylinder1ExtendAddress = dialog.ViewModel.Cylinder1ExtendAddress ?? string.Empty,
                    Cylinder1RetractAddress = dialog.ViewModel.Cylinder1RetractAddress ?? string.Empty,
                    Cylinder1ExtendFeedbackAddress = dialog.ViewModel.Cylinder1ExtendFeedbackAddress ?? string.Empty,
                    Cylinder1RetractFeedbackAddress = dialog.ViewModel.Cylinder1RetractFeedbackAddress ?? string.Empty,
                    Cylinder1DataType = dialog.ViewModel.Cylinder1DataType ?? "Bool",
                    // PLC配置 - 气缸2
                    Cylinder2ExtendAddress = dialog.ViewModel.Cylinder2ExtendAddress ?? string.Empty,
                    Cylinder2RetractAddress = dialog.ViewModel.Cylinder2RetractAddress ?? string.Empty,
                    Cylinder2ExtendFeedbackAddress = dialog.ViewModel.Cylinder2ExtendFeedbackAddress ?? string.Empty,
                    Cylinder2RetractFeedbackAddress = dialog.ViewModel.Cylinder2RetractFeedbackAddress ?? string.Empty,
                    Cylinder2DataType = dialog.ViewModel.Cylinder2DataType ?? "Bool",
                    // PLC配置 - 传感器
                    SensorAddress = dialog.ViewModel.SensorAddress ?? string.Empty,
                    SensorDataType = dialog.ViewModel.SensorDataType ?? "Bool",
                    // 反馈报警延时时间设置（直接使用ViewModel的值，不进行默认值替换）
                    // 注意：直接从 ViewModel 读取，确保获取最新的值
                    PushCylinderRetractTimeout = dialog.ViewModel.PushCylinderRetractTimeout,
                    PushCylinderExtendTimeout = dialog.ViewModel.PushCylinderExtendTimeout,
                    BlockingCylinderRetractTimeout = dialog.ViewModel.BlockingCylinderRetractTimeout,
                    BlockingCylinderExtendTimeout = dialog.ViewModel.BlockingCylinderExtendTimeout,
                    SensorDetectTimeout = dialog.ViewModel.SensorDetectTimeout,
                    PassageDelayTime = dialog.ViewModel.PassageDelayTime,
                    SensorConfirmDelayTime = dialog.ViewModel.SensorConfirmDelayTime,
                    IsSelected = false
                };

                // 添加调试日志，检查保存前的值
                System.Diagnostics.Debug.WriteLine($"[保存] 准备保存 - 推箱气缸收缩超时: {updatedRoom.PushCylinderRetractTimeout}");
                System.Diagnostics.Debug.WriteLine($"[保存] 准备保存 - 推箱气缸伸出超时: {updatedRoom.PushCylinderExtendTimeout}");
                System.Diagnostics.Debug.WriteLine($"[保存] 准备保存 - 阻挡气缸收缩超时: {updatedRoom.BlockingCylinderRetractTimeout}");
                System.Diagnostics.Debug.WriteLine($"[保存] 准备保存 - 阻挡气缸伸出超时: {updatedRoom.BlockingCylinderExtendTimeout}");
                
                var success = await _detectionRoomService.UpdateRoomAsync(updatedRoom);
                if (success)
                {
                    MessageBox.Show("检测室更新成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    _viewModel.LoadRoomsAsync();
                    
                    // 验证保存后的值
                    var savedRoom = await _detectionRoomService.GetRoomByIdAsync(room.Id);
                    if (savedRoom != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[保存] 保存后验证 - 推箱气缸收缩超时: {savedRoom.PushCylinderRetractTimeout}");
                        System.Diagnostics.Debug.WriteLine($"[保存] 保存后验证 - 推箱气缸伸出超时: {savedRoom.PushCylinderExtendTimeout}");
                        System.Diagnostics.Debug.WriteLine($"[保存] 保存后验证 - 阻挡气缸收缩超时: {savedRoom.BlockingCylinderRetractTimeout}");
                        System.Diagnostics.Debug.WriteLine($"[保存] 保存后验证 - 阻挡气缸伸出超时: {savedRoom.BlockingCylinderExtendTimeout}");
                    }
                }
                else
                {
                    MessageBox.Show("检测室更新失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            var roomItem = _viewModel.SelectedItem;
            if (roomItem == null)
            {
                MessageBox.Show("请选择要删除的检测室", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"确定要删除检测室 '{roomItem.RoomName}' 吗？", "确认删除", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                var success = await _detectionRoomService.DeleteRoomAsync(roomItem.Id);
                if (success)
                {
                    MessageBox.Show("检测室删除成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    _viewModel.LoadRoomsAsync();
                }
                else
                {
                    MessageBox.Show("检测室删除失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void View_Click(object sender, RoutedEventArgs e)
        {
            var roomItem = _viewModel.SelectedItem;
            if (roomItem == null)
            {
                MessageBox.Show("请选择要查看的检测室", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var room = await _detectionRoomService.GetRoomByIdAsync(roomItem.Id);
            if (room == null)
            {
                MessageBox.Show("检测室不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var details = new List<ViewDetailDialog.DetailItem>
            {
                new ViewDetailDialog.DetailItem { Label = "检测室编号", Value = room.RoomNo ?? "" },
                new ViewDetailDialog.DetailItem { Label = "检测室名称", Value = room.RoomName ?? "" },
                new ViewDetailDialog.DetailItem { Label = "备注", Value = room.Remark ?? "" },
                new ViewDetailDialog.DetailItem { Label = "", Value = "--- 扫码器配置 ---" },
                new ViewDetailDialog.DetailItem { Label = "串口号", Value = room.ScannerPortName ?? "" },
                new ViewDetailDialog.DetailItem { Label = "波特率", Value = room.ScannerBaudRate.ToString() },
                new ViewDetailDialog.DetailItem { Label = "数据位", Value = room.ScannerDataBits.ToString() },
                new ViewDetailDialog.DetailItem { Label = "停止位", Value = room.ScannerStopBits.ToString() },
                new ViewDetailDialog.DetailItem { Label = "校验位", Value = room.ScannerParity ?? "" },
                new ViewDetailDialog.DetailItem { Label = "启用扫码器", Value = room.ScannerIsEnabled ? "是" : "否" },
                new ViewDetailDialog.DetailItem { Label = "", Value = "--- PLC配置 - 气缸1（阻挡气缸）---" },
                new ViewDetailDialog.DetailItem { Label = "伸出控制地址", Value = room.Cylinder1ExtendAddress ?? "" },
                new ViewDetailDialog.DetailItem { Label = "收缩控制地址", Value = room.Cylinder1RetractAddress ?? "" },
                new ViewDetailDialog.DetailItem { Label = "伸出反馈地址", Value = room.Cylinder1ExtendFeedbackAddress ?? "" },
                new ViewDetailDialog.DetailItem { Label = "收缩反馈地址", Value = room.Cylinder1RetractFeedbackAddress ?? "" },
                new ViewDetailDialog.DetailItem { Label = "", Value = "--- PLC配置 - 气缸2（推箱气缸）---" },
                new ViewDetailDialog.DetailItem { Label = "伸出控制地址", Value = room.Cylinder2ExtendAddress ?? "" },
                new ViewDetailDialog.DetailItem { Label = "收缩控制地址", Value = room.Cylinder2RetractAddress ?? "" },
                new ViewDetailDialog.DetailItem { Label = "伸出反馈地址", Value = room.Cylinder2ExtendFeedbackAddress ?? "" },
                new ViewDetailDialog.DetailItem { Label = "收缩反馈地址", Value = room.Cylinder2RetractFeedbackAddress ?? "" },
                new ViewDetailDialog.DetailItem { Label = "", Value = "--- PLC配置 - 传感器 ---" },
                new ViewDetailDialog.DetailItem { Label = "传感器地址", Value = room.SensorAddress ?? "" }
            };

            var dialog = new ViewDetailDialog("检测室详情", details);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

    }
}
