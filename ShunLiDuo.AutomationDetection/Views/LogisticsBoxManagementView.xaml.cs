using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.ViewModels;
using ShunLiDuo.AutomationDetection.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class LogisticsBoxManagementView : UserControl
    {
        private readonly ILogisticsBoxService _logisticsBoxService;
        private readonly LogisticsBoxManagementViewModel _viewModel;

        public LogisticsBoxManagementView(
            ILogisticsBoxService logisticsBoxService,
            IAccountService accountService,
            ICurrentUserService currentUserService)
        {
            InitializeComponent();
            _logisticsBoxService = logisticsBoxService;
            _viewModel = new LogisticsBoxManagementViewModel(logisticsBoxService, accountService, currentUserService);
            DataContext = _viewModel;
            
            // 订阅ViewModel的事件
            _viewModel.EditRequested += (s, e) => Edit_Click(null, null);
            _viewModel.DeleteRequested += (s, e) => Delete_Click(null, null);
            _viewModel.ViewRequested += (s, e) => View_Click(null, null);
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            var boxItem = _viewModel.SelectedItem;
            if (boxItem == null)
            {
                MessageBox.Show("请选择要编辑的物流盒", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var box = await _logisticsBoxService.GetBoxByIdAsync(boxItem.Id);
            if (box == null)
            {
                MessageBox.Show("物流盒不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new AddLogisticsBoxDialog(box);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                var updatedBox = new LogisticsBoxItem
                {
                    Id = box.Id,
                    BoxNo = dialog.BoxNo ?? string.Empty,
                    BoxName = dialog.BoxName ?? string.Empty,
                    Remark = dialog.Remark ?? string.Empty,
                    IsSelected = false
                };

                var success = await _logisticsBoxService.UpdateBoxAsync(updatedBox);
                if (success)
                {
                    MessageBox.Show("物流盒更新成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    _viewModel.LoadBoxesAsync();
                }
                else
                {
                    MessageBox.Show("物流盒更新失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            var boxItem = _viewModel.SelectedItem;
            if (boxItem == null)
            {
                MessageBox.Show("请选择要删除的物流盒", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"确定要删除物流盒 '{boxItem.BoxName}' 吗？", "确认删除", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                var success = await _logisticsBoxService.DeleteBoxAsync(boxItem.Id);
                if (success)
                {
                    MessageBox.Show("物流盒删除成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    _viewModel.LoadBoxesAsync();
                }
                else
                {
                    MessageBox.Show("物流盒删除失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void View_Click(object sender, RoutedEventArgs e)
        {
            var boxItem = _viewModel.SelectedItem;
            if (boxItem == null)
            {
                MessageBox.Show("请选择要查看的物流盒", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var box = await _logisticsBoxService.GetBoxByIdAsync(boxItem.Id);
            if (box == null)
            {
                MessageBox.Show("物流盒不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var details = new List<ViewDetailDialog.DetailItem>
            {
                new ViewDetailDialog.DetailItem { Label = "物流盒编号", Value = box.BoxNo ?? "" },
                new ViewDetailDialog.DetailItem { Label = "物流盒名称", Value = box.BoxName ?? "" },
                new ViewDetailDialog.DetailItem { Label = "备注", Value = box.Remark ?? "" }
            };

            var dialog = new ViewDetailDialog("物流盒详情", details);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

    }
}

