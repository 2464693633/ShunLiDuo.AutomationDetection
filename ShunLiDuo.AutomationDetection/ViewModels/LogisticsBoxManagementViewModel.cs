using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Services;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class LogisticsBoxManagementViewModel : BindableBase
    {
        private string _searchKeyword;
        private ObservableCollection<LogisticsBoxItem> _logisticsBoxes;
        private int _totalCount;
        private bool _isLoading;
        private readonly ILogisticsBoxService _logisticsBoxService;

        public LogisticsBoxManagementViewModel(ILogisticsBoxService logisticsBoxService)
        {
            _logisticsBoxService = logisticsBoxService;
            LogisticsBoxes = new ObservableCollection<LogisticsBoxItem>();
            SearchCommand = new DelegateCommand(OnSearch, () => !IsLoading);
            AddCommand = new DelegateCommand(OnAdd, () => !IsLoading);
            LoadBoxesAsync();
        }

        public async void LoadBoxesAsync()
        {
            IsLoading = true;
            try
            {
                var boxes = await _logisticsBoxService.GetAllBoxesAsync();
                LogisticsBoxes.Clear();
                foreach (var box in boxes)
                {
                    LogisticsBoxes.Add(box);
                }
                TotalCount = LogisticsBoxes.Count;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"加载物流盒失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnSearch()
        {
            // 搜索逻辑
            LoadBoxesAsync();
        }

        private async void OnAdd()
        {
            var dialog = new Views.AddLogisticsBoxDialog();
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                try
                {
                    // 创建新的物流盒
                    var newBox = new LogisticsBoxItem
                    {
                        BoxNo = dialog.BoxNo ?? string.Empty,
                        BoxName = dialog.BoxName ?? string.Empty,
                        Remark = dialog.Remark ?? string.Empty,
                        IsSelected = false
                    };
                    
                    var success = await _logisticsBoxService.AddBoxAsync(newBox);
                    if (success)
                    {
                        MessageBox.Show("物流盒添加成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadBoxesAsync();
                    }
                    else
                    {
                        MessageBox.Show("物流盒添加失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"添加物流盒失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        public ObservableCollection<LogisticsBoxItem> LogisticsBoxes
        {
            get => _logisticsBoxes;
            set => SetProperty(ref _logisticsBoxes, value);
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

