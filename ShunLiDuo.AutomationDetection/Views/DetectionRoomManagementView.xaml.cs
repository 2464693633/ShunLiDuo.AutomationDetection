using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.ViewModels;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class DetectionRoomManagementView : UserControl
    {
        private readonly IDetectionRoomService _detectionRoomService;
        private readonly DetectionRoomManagementViewModel _viewModel;

        public DetectionRoomManagementView(IDetectionRoomService detectionRoomService)
        {
            InitializeComponent();
            _detectionRoomService = detectionRoomService;
            _viewModel = new DetectionRoomManagementViewModel(detectionRoomService);
            DataContext = _viewModel;
        }

        private async void Edit_Click(object sender, MouseButtonEventArgs e)
        {
            var roomItem = GetSelectedRoomItem(sender);
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
                    IsSelected = false
                };

                var success = await _detectionRoomService.UpdateRoomAsync(updatedRoom);
                if (success)
                {
                    MessageBox.Show("检测室更新成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    _viewModel.LoadRoomsAsync();
                }
                else
                {
                    MessageBox.Show("检测室更新失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void Delete_Click(object sender, MouseButtonEventArgs e)
        {
            var roomItem = GetSelectedRoomItem(sender);
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

        private async void View_Click(object sender, MouseButtonEventArgs e)
        {
            var roomItem = GetSelectedRoomItem(sender);
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
                new ViewDetailDialog.DetailItem { Label = "备注", Value = room.Remark ?? "" }
            };

            var dialog = new ViewDetailDialog("检测室详情", details);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private DetectionRoomItem GetSelectedRoomItem(object sender)
        {
            var textBlock = sender as TextBlock;
            if (textBlock == null) return null;

            var dataGridRow = FindParent<DataGridRow>(textBlock);
            if (dataGridRow == null) return null;

            return dataGridRow.Item as DetectionRoomItem;
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;
            else
                return FindParent<T>(parentObject);
        }
    }
}
