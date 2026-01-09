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
    public class DeviceAlarmViewModel : BindableBase
    {
        private readonly IAlarmRecordService _alarmRecordService;
        private readonly IDetectionRoomService _detectionRoomService;
        private readonly ICurrentUserService _currentUserService;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private string _selectedRoom;
        private string _searchKeyword;
        private ObservableCollection<AlarmRecord> _alarmRecords;
        private ObservableCollection<string> _roomNames;
        private int _totalCount;
        private bool _isLoading;

        public DeviceAlarmViewModel(IAlarmRecordService alarmRecordService, IDetectionRoomService detectionRoomService, ICurrentUserService currentUserService)
        {
            _alarmRecordService = alarmRecordService;
            _detectionRoomService = detectionRoomService;
            _currentUserService = currentUserService;
            AlarmRecords = new ObservableCollection<AlarmRecord>();
            RoomNames = new ObservableCollection<string> { "全部" };
            
            QueryCommand = new DelegateCommand(OnQuery, () => !_isLoading);
            RefreshCommand = new DelegateCommand(OnRefresh, () => !_isLoading);
            HandleCommand = new DelegateCommand<AlarmRecord>(OnHandle, alarm => alarm != null && alarm.Status == "未处理");
            DeleteCommand = new DelegateCommand<AlarmRecord>(OnDelete, alarm => alarm != null);
            
            // 初始化时加载数据
            LoadAlarmsAsync();
            LoadRoomNamesAsync();
        }

        private async void LoadAlarmsAsync()
        {
            IsLoading = true;
            try
            {
                var alarms = await _alarmRecordService.GetAllAlarmsAsync();
                AlarmRecords.Clear();
                foreach (var alarm in alarms.OrderByDescending(a => a.CreateTime))
                {
                    AlarmRecords.Add(alarm);
                }
                TotalCount = AlarmRecords.Count;
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"加载报警记录失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void LoadRoomNamesAsync()
        {
            try
            {
                var rooms = await _detectionRoomService.GetAllRoomsAsync();
                RoomNames.Clear();
                RoomNames.Add("全部");
                foreach (var room in rooms.OrderBy(r => r.Id))
                {
                    RoomNames.Add(room.RoomName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载检测室列表失败: {ex.Message}");
            }
        }

        private async void OnQuery()
        {
            IsLoading = true;
            try
            {
                var alarms = await _alarmRecordService.GetAllAlarmsAsync();
                
                // 应用筛选条件
                var filteredAlarms = alarms.AsEnumerable();
                
                if (StartTime.HasValue)
                {
                    filteredAlarms = filteredAlarms.Where(alarm => 
                        alarm.CreateTime >= StartTime.Value.Date);
                }
                
                if (EndTime.HasValue)
                {
                    filteredAlarms = filteredAlarms.Where(alarm => 
                        alarm.CreateTime <= EndTime.Value.Date.AddDays(1).AddSeconds(-1));
                }
                
                if (!string.IsNullOrWhiteSpace(SelectedRoom) && SelectedRoom != "全部")
                {
                    filteredAlarms = filteredAlarms.Where(alarm => alarm.RoomName == SelectedRoom);
                }
                
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    filteredAlarms = filteredAlarms.Where(alarm => 
                        (alarm.AlarmTitle != null && alarm.AlarmTitle.Contains(SearchKeyword)) ||
                        (alarm.AlarmMessage != null && alarm.AlarmMessage.Contains(SearchKeyword)) ||
                        (alarm.AlarmCode != null && alarm.AlarmCode.Contains(SearchKeyword)));
                }
                
                AlarmRecords.Clear();
                foreach (var alarm in filteredAlarms.OrderByDescending(a => a.CreateTime))
                {
                    AlarmRecords.Add(alarm);
                }
                TotalCount = AlarmRecords.Count;
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"查询报警记录失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnRefresh()
        {
            LoadAlarmsAsync();
        }

        private async void OnHandle(AlarmRecord alarm)
        {
            if (alarm == null || alarm.Status == "已处理")
                return;

            try
            {
                var handler = _currentUserService?.CurrentUser?.Name ?? "系统";

                var success = await _alarmRecordService.HandleAlarmAsync(alarm.Id, handler, "");
                
                if (success)
                {
                    alarm.Status = "已处理";
                    alarm.HandleTime = DateTime.Now;
                    alarm.Handler = handler;
                    RaisePropertyChanged(nameof(AlarmRecords));
                    CustomMessageBox.ShowInformation("报警已处理");
                }
                else
                {
                    CustomMessageBox.ShowError("处理报警失败");
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"处理报警失败: {ex.Message}");
            }
        }

        private async void OnDelete(AlarmRecord alarm)
        {
            if (alarm == null)
                return;

            var result = CustomMessageBox.ShowQuestion("确定要删除这条报警记录吗？", "确认删除");
            
            if (result != CustomMessageBoxResult.Yes)
                return;

            try
            {
                var success = await _alarmRecordService.DeleteAlarmAsync(alarm.Id);
                
                if (success)
                {
                    AlarmRecords.Remove(alarm);
                    TotalCount = AlarmRecords.Count;
                    CustomMessageBox.ShowInformation("删除成功");
                }
                else
                {
                    CustomMessageBox.ShowError("删除失败");
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"删除失败: {ex.Message}");
            }
        }

        // 属性
        public ObservableCollection<AlarmRecord> AlarmRecords
        {
            get => _alarmRecords;
            set => SetProperty(ref _alarmRecords, value);
        }

        public ObservableCollection<string> RoomNames
        {
            get => _roomNames;
            set => SetProperty(ref _roomNames, value);
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
                QueryCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
            }
        }

        // 命令
        public DelegateCommand QueryCommand { get; private set; }
        public DelegateCommand RefreshCommand { get; private set; }
        public DelegateCommand<AlarmRecord> HandleCommand { get; private set; }
        public DelegateCommand<AlarmRecord> DeleteCommand { get; private set; }
    }
}
