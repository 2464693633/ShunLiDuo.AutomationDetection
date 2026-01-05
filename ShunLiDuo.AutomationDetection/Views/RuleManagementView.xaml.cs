using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.ViewModels;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class RuleManagementView : UserControl
    {
        private readonly IRuleService _ruleService;
        private readonly IDetectionRoomService _detectionRoomService;
        private readonly ILogisticsBoxService _logisticsBoxService;
        private readonly RuleManagementViewModel _viewModel;

        public RuleManagementView(IRuleService ruleService, IDetectionRoomService detectionRoomService, ILogisticsBoxService logisticsBoxService)
        {
            InitializeComponent();
            _ruleService = ruleService;
            _detectionRoomService = detectionRoomService;
            _logisticsBoxService = logisticsBoxService;
            _viewModel = new RuleManagementViewModel(ruleService, detectionRoomService, logisticsBoxService);
            DataContext = _viewModel;
        }

        private async void Edit_Click(object sender, MouseButtonEventArgs e)
        {
            var ruleItem = GetSelectedRuleItem(sender);
            if (ruleItem == null)
            {
                MessageBox.Show("请选择要编辑的规则", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rule = await _ruleService.GetRuleByIdAsync(ruleItem.Id);
            if (rule == null)
            {
                MessageBox.Show("规则不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new AddRuleDialog(rule);
            dialog.Owner = Application.Current.MainWindow;
            
            // 从数据库加载检测室和物流盒数据
            try
            {
                var rooms = await _detectionRoomService.GetAllRoomsAsync();
                var boxes = await _logisticsBoxService.GetAllBoxesAsync();
                
                dialog.ViewModel.LoadDetectionRooms(new ObservableCollection<DetectionRoomItem>(rooms));
                dialog.ViewModel.LoadLogisticsBoxes(new ObservableCollection<LogisticsBoxItem>(boxes));
                
                // 注意：编辑模式下的选中状态已经在AddRuleDialogViewModel的构造函数中处理了
                // 这里不需要再手动设置，因为LoadDetectionRooms和LoadLogisticsBoxes会恢复选中状态
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"加载数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (dialog.ShowDialog() == true)
            {
                var updatedRule = new RuleItem
                {
                    Id = rule.Id,
                    RuleNo = rule.RuleNo,
                    RuleName = dialog.ViewModel.RuleName ?? string.Empty,
                    DetectionRooms = dialog.ViewModel.GetSelectedDetectionRooms() ?? string.Empty,
                    LogisticsBoxNos = dialog.ViewModel.GetSelectedLogisticsBoxNos() ?? string.Empty,
                    Remark = dialog.ViewModel.Remark ?? string.Empty
                };

                var success = await _ruleService.UpdateRuleAsync(updatedRule);
                if (success)
                {
                    MessageBox.Show("规则更新成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    _viewModel.LoadRulesAsync();
                }
                else
                {
                    MessageBox.Show("规则更新失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void Delete_Click(object sender, MouseButtonEventArgs e)
        {
            var ruleItem = GetSelectedRuleItem(sender);
            if (ruleItem == null)
            {
                MessageBox.Show("请选择要删除的规则", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"确定要删除规则 '{ruleItem.RuleName}' 吗？", "确认删除", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                var success = await _ruleService.DeleteRuleAsync(ruleItem.Id);
                if (success)
                {
                    MessageBox.Show("规则删除成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    _viewModel.LoadRulesAsync();
                }
                else
                {
                    MessageBox.Show("规则删除失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void View_Click(object sender, MouseButtonEventArgs e)
        {
            var ruleItem = GetSelectedRuleItem(sender);
            if (ruleItem == null)
            {
                MessageBox.Show("请选择要查看的规则", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rule = await _ruleService.GetRuleByIdAsync(ruleItem.Id);
            if (rule == null)
            {
                MessageBox.Show("规则不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var details = new List<ViewDetailDialog.DetailItem>
            {
                new ViewDetailDialog.DetailItem { Label = "规则编号", Value = rule.RuleNo ?? "" },
                new ViewDetailDialog.DetailItem { Label = "规则名称", Value = rule.RuleName ?? "" },
                new ViewDetailDialog.DetailItem { Label = "检测室", Value = rule.DetectionRooms ?? "" },
                new ViewDetailDialog.DetailItem { Label = "物流盒编号", Value = rule.LogisticsBoxNos ?? "" },
                new ViewDetailDialog.DetailItem { Label = "备注", Value = rule.Remark ?? "" }
            };

            var dialog = new ViewDetailDialog("规则详情", details);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private RuleItem GetSelectedRuleItem(object sender)
        {
            var textBlock = sender as TextBlock;
            if (textBlock == null) return null;

            var dataGridRow = FindParent<DataGridRow>(textBlock);
            if (dataGridRow == null) return null;

            return dataGridRow.Item as RuleItem;
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
