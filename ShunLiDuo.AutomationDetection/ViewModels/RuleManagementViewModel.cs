using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Services;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class RuleManagementViewModel : BindableBase
    {
        private string _searchKeyword;
        private ObservableCollection<RuleItem> _rules;
        private int _totalCount;
        private bool _isLoading;
        private readonly IRuleService _ruleService;
        private readonly IDetectionRoomService _detectionRoomService;
        private readonly ILogisticsBoxService _logisticsBoxService;

        public RuleManagementViewModel(IRuleService ruleService, IDetectionRoomService detectionRoomService, ILogisticsBoxService logisticsBoxService)
        {
            _ruleService = ruleService;
            _detectionRoomService = detectionRoomService;
            _logisticsBoxService = logisticsBoxService;
            Rules = new ObservableCollection<RuleItem>();
            SearchCommand = new DelegateCommand(OnSearch, () => !IsLoading);
            AddCommand = new DelegateCommand(OnAdd, () => !IsLoading);
            LoadRulesAsync();
        }

        private async void LoadRulesAsync()
        {
            IsLoading = true;
            try
            {
                var rules = await _ruleService.GetAllRulesAsync();
                Rules.Clear();
                foreach (var rule in rules)
                {
                    Rules.Add(rule);
                }
                TotalCount = Rules.Count;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"加载规则失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnSearch()
        {
            IsLoading = true;
            try
            {
                var rules = await _ruleService.SearchRulesAsync(SearchKeyword ?? string.Empty);
                Rules.Clear();
                foreach (var rule in rules)
                {
                    Rules.Add(rule);
                }
                TotalCount = Rules.Count;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"搜索规则失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnAdd()
        {
            var dialog = new Views.AddRuleDialog();
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            // 从数据库加载检测室和物流盒数据
            try
            {
                var rooms = await _detectionRoomService.GetAllRoomsAsync();
                var boxes = await _logisticsBoxService.GetAllBoxesAsync();
                
                dialog.ViewModel.LoadDetectionRooms(new ObservableCollection<Models.DetectionRoomItem>(rooms));
                dialog.ViewModel.LoadLogisticsBoxes(new ObservableCollection<Models.LogisticsBoxItem>(boxes));
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"加载数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                try
                {
                    var newRule = new RuleItem
                    {
                        RuleNo = dialog.RuleNo ?? string.Empty,
                        RuleName = dialog.RuleName ?? string.Empty,
                        DetectionRooms = dialog.SelectedDetectionRooms ?? string.Empty,
                        LogisticsBoxNos = dialog.SelectedLogisticsBoxNos ?? string.Empty,
                        Remark = dialog.Remark ?? string.Empty,
                        IsSelected = false
                    };

                    var success = await _ruleService.AddRuleAsync(newRule);
                    if (success)
                    {
                        MessageBox.Show("规则添加成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadRulesAsync();
                    }
                    else
                    {
                        MessageBox.Show("规则添加失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"添加规则失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        public ObservableCollection<RuleItem> Rules
        {
            get => _rules;
            set => SetProperty(ref _rules, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                SearchCommand.RaiseCanExecuteChanged();
                AddCommand.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand SearchCommand { get; private set; }
        public DelegateCommand AddCommand { get; private set; }
    }
}

