using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.ViewModels;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Views;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class RuleManagementView : UserControl
    {
        private readonly IRuleService _ruleService;
        private readonly IDetectionRoomService _detectionRoomService;
        private readonly ILogisticsBoxService _logisticsBoxService;
        private readonly RuleManagementViewModel _viewModel;

        public RuleManagementView(
            IRuleService ruleService, 
            IDetectionRoomService detectionRoomService, 
            ILogisticsBoxService logisticsBoxService,
            IAccountService accountService,
            ICurrentUserService currentUserService)
        {
            InitializeComponent();
            _ruleService = ruleService;
            _detectionRoomService = detectionRoomService;
            _logisticsBoxService = logisticsBoxService;
            _viewModel = new RuleManagementViewModel(ruleService, detectionRoomService, logisticsBoxService, accountService, currentUserService);
            DataContext = _viewModel;
            
            // 订阅ViewModel的事件
            _viewModel.EditRequested += (s, e) => Edit_Click(null, null);
            _viewModel.DeleteRequested += (s, e) => Delete_Click(null, null);
            _viewModel.ViewRequested += (s, e) => View_Click(null, null);
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            var ruleItem = _viewModel.SelectedItem;
            if (ruleItem == null)
            {
                CustomMessageBox.ShowWarning("请选择要编辑的规则");
                return;
            }

            var rule = await _ruleService.GetRuleByIdAsync(ruleItem.Id);
            if (rule == null)
            {
                CustomMessageBox.ShowError("规则不存在");
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
                CustomMessageBox.ShowError($"加载数据失败: {ex.Message}");
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
                    CustomMessageBox.ShowInformation("规则更新成功");
                    _viewModel.LoadRulesAsync();
                }
                else
                {
                    CustomMessageBox.ShowError("规则更新失败");
                }
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            var ruleItem = _viewModel.SelectedItem;
            if (ruleItem == null)
            {
                CustomMessageBox.ShowWarning("请选择要删除的规则");
                return;
            }

            var result = CustomMessageBox.ShowQuestion($"确定要删除规则 '{ruleItem.RuleName}' 吗？", "确认删除");
            
            if (result == CustomMessageBoxResult.Yes)
            {
                var success = await _ruleService.DeleteRuleAsync(ruleItem.Id);
                if (success)
                {
                    CustomMessageBox.ShowInformation("规则删除成功");
                    _viewModel.LoadRulesAsync();
                }
                else
                {
                    CustomMessageBox.ShowError("规则删除失败");
                }
            }
        }

        private async void View_Click(object sender, RoutedEventArgs e)
        {
            var ruleItem = _viewModel.SelectedItem;
            if (ruleItem == null)
            {
                CustomMessageBox.ShowWarning("请选择要查看的规则");
                return;
            }

            var rule = await _ruleService.GetRuleByIdAsync(ruleItem.Id);
            if (rule == null)
            {
                CustomMessageBox.ShowError("规则不存在");
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

    }
}
