using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.Views;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class DetectionLogViewModel : BindableBase
    {
        private readonly IDetectionLogService _detectionLogService;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private string _selectedRoom;
        private string _searchKeyword;
        private ObservableCollection<DetectionLogItem> _detectionLogs;
        private ObservableCollection<string> _roomNames;
        private int _totalCount;
        private bool _isLoading;

        public DetectionLogViewModel(IDetectionLogService detectionLogService)
        {
            _detectionLogService = detectionLogService;
            DetectionLogs = new ObservableCollection<DetectionLogItem>();
            RoomNames = new ObservableCollection<string>();
            QueryCommand = new DelegateCommand(OnQuery, () => !_isLoading);
            ExportCommand = new DelegateCommand(OnExport, () => !_isLoading);
            RefreshCommand = new DelegateCommand(OnRefresh, () => !_isLoading);
            
            // 初始化时加载数据
            LoadLogsAsync();
        }

        private async void LoadLogsAsync()
        {
            IsLoading = true;
            try
            {
                var logs = await _detectionLogService.GetAllLogsAsync();
                DetectionLogs.Clear();
                foreach (var log in logs)
                {
                    DetectionLogs.Add(log);
                }
                TotalCount = DetectionLogs.Count;
                
                // 更新检测室名称列表
                UpdateRoomNames();
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"加载检测历史记录失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateRoomNames()
        {
            var rooms = DetectionLogs
                .Where(log => !string.IsNullOrWhiteSpace(log.RoomName))
                .Select(log => log.RoomName)
                .Distinct()
                .OrderBy(r => r)
                .ToList();
            
            RoomNames.Clear();
            RoomNames.Add("全部");
            foreach (var room in rooms)
            {
                RoomNames.Add(room);
            }
        }

        private async void OnQuery()
        {
            IsLoading = true;
            try
            {
                var logs = await _detectionLogService.GetAllLogsAsync();
                
                // 应用筛选条件
                var filteredLogs = logs.AsEnumerable();
                
                if (StartTime.HasValue)
                {
                    filteredLogs = filteredLogs.Where(log => 
                        log.CreateTime >= StartTime.Value.Date);
                }
                
                if (EndTime.HasValue)
                {
                    filteredLogs = filteredLogs.Where(log => 
                        log.CreateTime <= EndTime.Value.Date.AddDays(1).AddSeconds(-1));
                }
                
                if (!string.IsNullOrWhiteSpace(SelectedRoom) && SelectedRoom != "全部")
                {
                    filteredLogs = filteredLogs.Where(log => log.RoomName == SelectedRoom);
                }
                
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    filteredLogs = filteredLogs.Where(log => 
                        (log.LogisticsBoxCode != null && log.LogisticsBoxCode.Contains(SearchKeyword)) ||
                        (log.RoomName != null && log.RoomName.Contains(SearchKeyword)));
                }
                
                DetectionLogs.Clear();
                foreach (var log in filteredLogs.OrderByDescending(l => l.CreateTime))
                {
                    DetectionLogs.Add(log);
                }
                TotalCount = DetectionLogs.Count;
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"查询检测历史记录失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnExport()
        {
            CustomMessageBox.ShowInformation("导出功能待实现");
        }

        private void OnRefresh()
        {
            LoadLogsAsync();
        }

        public DateTime? StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public DateTime? EndTime
        {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        public string SelectedRoom
        {
            get => _selectedRoom;
            set => SetProperty(ref _selectedRoom, value);
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        public ObservableCollection<DetectionLogItem> DetectionLogs
        {
            get => _detectionLogs;
            set => SetProperty(ref _detectionLogs, value);
        }

        public ObservableCollection<string> RoomNames
        {
            get => _roomNames;
            set => SetProperty(ref _roomNames, value);
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
                QueryCommand?.RaiseCanExecuteChanged();
                ExportCommand?.RaiseCanExecuteChanged();
                RefreshCommand?.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand QueryCommand { get; private set; }
        public DelegateCommand ExportCommand { get; private set; }
        public DelegateCommand RefreshCommand { get; private set; }
    }
}

