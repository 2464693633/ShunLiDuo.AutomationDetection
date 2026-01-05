using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class TaskManagementViewModel : BindableBase
    {
        private readonly IRuleService _ruleService;
        private readonly IDetectionRoomService _detectionRoomService;
        private readonly IS7CommunicationService _s7Service;
        private string _logisticsBoxCode;
        private string _logisticsBoxInputInfo;
        private RuleItem _selectedRuleItem;
        private ObservableCollection<Models.TaskItem> _tasks;
        private ObservableCollection<RuleItem> _rules;
        private ObservableCollection<Models.DetectionRoomItem> _detectionRooms;
        private ObservableCollection<string> _logisticsBoxList;
        private ObservableCollection<Models.RoomBoxList> _roomBoxLists;
        private bool _isPlcConnected;
        private DispatcherTimer _plcStatusTimer;

        public bool IsPlcConnected
        {
            get => _isPlcConnected;
            set => SetProperty(ref _isPlcConnected, value);
        }

        public TaskManagementViewModel(IRuleService ruleService, IDetectionRoomService detectionRoomService, IS7CommunicationService s7Service)
        {
            _ruleService = ruleService;
            _detectionRoomService = detectionRoomService;
            _s7Service = s7Service;
            InitializeData();
            ExecuteCommand = new DelegateCommand(OnExecute);
            LoadRulesAsync();
            LoadDetectionRoomsAsync();
            
            // 初始化PLC连接状态
            UpdatePlcConnectionStatus();
            
            // 监听连接状态变化
            _s7Service.ConnectionStatusChanged += S7Service_ConnectionStatusChanged;
            
            // 启动定时器定期检查连接状态（每秒检查一次）
            _plcStatusTimer = new DispatcherTimer();
            _plcStatusTimer.Interval = System.TimeSpan.FromSeconds(1);
            _plcStatusTimer.Tick += (s, e) => UpdatePlcConnectionStatus();
            _plcStatusTimer.Start();
        }
        
        // 公开刷新方法，供视图调用
        public async System.Threading.Tasks.Task RefreshDetectionRoomsAsync()
        {
            await LoadDetectionRoomsAsync();
        }
        
        // 公开刷新规则方法，供视图调用
        public async System.Threading.Tasks.Task RefreshRulesAsync()
        {
            await LoadRulesAsync();
        }
        
        // 刷新所有数据
        public async System.Threading.Tasks.Task RefreshAllDataAsync()
        {
            await LoadRulesAsync();
            await LoadDetectionRoomsAsync();
        }

        private void S7Service_ConnectionStatusChanged(object sender, bool isConnected)
        {
            // 直接更新，BindableBase的SetProperty已经处理了线程安全
            IsPlcConnected = isConnected;
        }

        private void UpdatePlcConnectionStatus()
        {
            // 直接更新，BindableBase的SetProperty已经处理了线程安全
            var currentStatus = _s7Service.IsConnected;
            // 强制更新，即使值相同也触发通知（确保UI刷新）
            IsPlcConnected = currentStatus;
        }

        private async void InitializeData()
        {
            Tasks = new ObservableCollection<Models.TaskItem>
            {
                new Models.TaskItem { Id = 1, InspectorName = "张三" },
                new Models.TaskItem { Id = 2, InspectorName = "李四" },
                new Models.TaskItem { Id = 3 },
                new Models.TaskItem { Id = 4 }
            };

            RoomBoxLists = new ObservableCollection<Models.RoomBoxList>();
            LogisticsBoxList = new ObservableCollection<string>();
            Rules = new ObservableCollection<RuleItem>();
            DetectionRooms = new ObservableCollection<Models.DetectionRoomItem>();
        }

        private async System.Threading.Tasks.Task LoadRulesAsync()
        {
            try
            {
                var rules = await _ruleService.GetAllRulesAsync();
                var currentSelectedRuleId = SelectedRuleItem?.Id ?? 0;
                
                Rules.Clear();
                foreach (var rule in rules)
                {
                    Rules.Add(rule);
                }
                
                // 尝试恢复之前选中的规则，如果没有则选择第一个
                if (currentSelectedRuleId > 0)
                {
                    var previousRule = Rules.FirstOrDefault(r => r.Id == currentSelectedRuleId);
                    if (previousRule != null)
                    {
                        SelectedRuleItem = previousRule;
                    }
                    else if (Rules.Count > 0)
                    {
                        SelectedRuleItem = Rules[0];
                    }
                }
                else if (Rules.Count > 0)
                {
                    SelectedRuleItem = Rules[0];
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载规则失败: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadDetectionRoomsAsync()
        {
            try
            {
                var rooms = await _detectionRoomService.GetAllRoomsAsync();
                
                // 检查检测室列表是否有变化
                bool hasChanged = false;
                if (DetectionRooms.Count != rooms.Count)
                {
                    hasChanged = true;
                }
                else
                {
                    for (int i = 0; i < rooms.Count; i++)
                    {
                        if (i >= DetectionRooms.Count || 
                            DetectionRooms[i].Id != rooms[i].Id ||
                            DetectionRooms[i].RoomName != rooms[i].RoomName)
                        {
                            hasChanged = true;
                            break;
                        }
                    }
                }
                
                if (!hasChanged)
                {
                    return; // 没有变化，不需要更新
                }
                
                // 保存现有的物流盒分配
                var existingBoxesMap = new System.Collections.Generic.Dictionary<int, ObservableCollection<string>>();
                foreach (var roomBoxList in RoomBoxLists)
                {
                    if (roomBoxList.Room != null)
                    {
                        existingBoxesMap[roomBoxList.Room.Id] = new ObservableCollection<string>(roomBoxList.Boxes);
                    }
                }
                
                // 更新检测室列表
                DetectionRooms.Clear();
                foreach (var room in rooms)
                {
                    DetectionRooms.Add(room);
                }
                
                // 更新RoomBoxLists
                RoomBoxLists.Clear();
                foreach (var room in rooms)
                {
                    var roomBoxList = new Models.RoomBoxList
                    {
                        Room = room
                    };
                    
                    // 恢复之前的物流盒分配（如果存在）
                    if (existingBoxesMap.ContainsKey(room.Id))
                    {
                        roomBoxList.Boxes = existingBoxesMap[room.Id];
                    }
                    else
                    {
                        roomBoxList.Boxes = new ObservableCollection<string>();
                    }
                    
                    RoomBoxLists.Add(roomBoxList);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载检测室失败: {ex.Message}");
            }
        }

        private void OnExecute()
        {
            // 执行检测归属逻辑 - 根据选定的规则自动分配
            if (SelectedRuleItem == null)
            {
                System.Windows.MessageBox.Show("请先选择调度规则", "提示", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // 清空所有检测室的物流盒
            foreach (var roomBoxList in RoomBoxLists)
            {
                roomBoxList.Boxes.Clear();
            }

            // 重新分配所有物流盒
            foreach (var boxCode in LogisticsBoxList.ToList())
            {
                AutoAssignToRoom(boxCode);
            }
        }

        public string LogisticsBoxCode
        {
            get => _logisticsBoxCode;
            set => SetProperty(ref _logisticsBoxCode, value);
        }

        public void AddLogisticsBoxCode(string code)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var formattedCode = code.StartsWith("物流盒编码") ? code : $"物流盒编码{code}";
                if (!LogisticsBoxList.Contains(formattedCode))
                {
                    LogisticsBoxList.Add(formattedCode);
                    // 自动根据规则分配
                    AutoAssignToRoom(formattedCode);
                }
                LogisticsBoxCode = string.Empty;
            }
        }

        private void AutoAssignToRoom(string boxCode)
        {
            if (SelectedRuleItem == null || RoomBoxLists == null || RoomBoxLists.Count == 0) return;

            // 提取物流盒编号（去掉"物流盒编码"前缀）
            var boxNo = boxCode.Replace("物流盒编码", "").Trim();
            
            // 解析规则中的检测室名称（DetectionRooms存储的是RoomName）和物流盒编号
            var ruleDetectionRoomNames = SelectedRuleItem.DetectionRooms?.Split(',')
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .ToList() ?? new System.Collections.Generic.List<string>();

            var ruleLogisticsBoxNos = SelectedRuleItem.LogisticsBoxNos?.Split(',')
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Select(b => b.Trim())
                .ToList() ?? new System.Collections.Generic.List<string>();

            // 检查该物流盒是否在规则的物流盒列表中
            if (ruleLogisticsBoxNos.Contains(boxNo))
            {
                // 根据规则中的检测室名称匹配到实际的检测室
                // DetectionRooms存储的是RoomName，所以优先匹配RoomName
                foreach (var ruleRoomName in ruleDetectionRoomNames)
                {
                    // 查找匹配的检测室（优先精确匹配RoomName，也支持RoomNo匹配）
                    var matchedRoomBoxList = RoomBoxLists.FirstOrDefault(rbl => 
                        rbl.Room != null && (
                            rbl.Room.RoomName == ruleRoomName || 
                            rbl.Room.RoomNo == ruleRoomName ||
                            rbl.Room.RoomName.Contains(ruleRoomName) ||
                            ruleRoomName.Contains(rbl.Room.RoomName)));
                    
                    if (matchedRoomBoxList != null && !matchedRoomBoxList.Boxes.Contains(boxCode))
                    {
                        matchedRoomBoxList.Boxes.Add(boxCode);
                        break; // 找到一个匹配的检测室后就不再继续查找
                    }
                }
            }
        }

        public string LogisticsBoxInputInfo
        {
            get => _logisticsBoxInputInfo;
            set => SetProperty(ref _logisticsBoxInputInfo, value);
        }

        public ObservableCollection<string> LogisticsBoxList
        {
            get => _logisticsBoxList;
            set => SetProperty(ref _logisticsBoxList, value);
        }

        public ObservableCollection<RuleItem> Rules
        {
            get => _rules;
            set => SetProperty(ref _rules, value);
        }

        public ObservableCollection<Models.DetectionRoomItem> DetectionRooms
        {
            get => _detectionRooms;
            set => SetProperty(ref _detectionRooms, value);
        }

        public RuleItem SelectedRuleItem
        {
            get => _selectedRuleItem;
            set
            {
                if (SetProperty(ref _selectedRuleItem, value))
                {
                    // 当规则改变时，重新分配所有物流盒
                    if (value != null && LogisticsBoxList != null && LogisticsBoxList.Count > 0)
                    {
                        // 清空所有检测室
                        foreach (var roomBoxList in RoomBoxLists)
                        {
                            roomBoxList.Boxes.Clear();
                        }
                        
                        // 重新分配所有物流盒
                        foreach (var boxCode in LogisticsBoxList.ToList())
                        {
                            AutoAssignToRoom(boxCode);
                        }
                    }
                }
            }
        }

        public ObservableCollection<Models.TaskItem> Tasks
        {
            get => _tasks;
            set => SetProperty(ref _tasks, value);
        }

        public ObservableCollection<Models.RoomBoxList> RoomBoxLists
        {
            get => _roomBoxLists;
            set => SetProperty(ref _roomBoxLists, value);
        }

        public DelegateCommand ExecuteCommand { get; private set; }
    }
}

