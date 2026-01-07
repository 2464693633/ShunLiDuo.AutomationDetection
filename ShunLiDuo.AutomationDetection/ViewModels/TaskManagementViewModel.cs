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
        private readonly IScannerCommunicationService _scannerService;
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

        public TaskManagementViewModel(
            IRuleService ruleService, 
            IDetectionRoomService detectionRoomService, 
            IS7CommunicationService s7Service,
            IScannerCommunicationService scannerService)
        {
            _ruleService = ruleService;
            _detectionRoomService = detectionRoomService;
            _s7Service = s7Service;
            _scannerService = scannerService;
            InitializeData();
            LoadRulesAsync();
            LoadDetectionRoomsAsync();
            
            // 初始化PLC连接状态
            UpdatePlcConnectionStatus();
            
            // 监听连接状态变化
            _s7Service.ConnectionStatusChanged += S7Service_ConnectionStatusChanged;
            
            // 监听扫码数据接收事件
            _scannerService.DataReceived += ScannerService_DataReceived;
            
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

        private async void ScannerService_DataReceived(object sender, Services.ScannerDataReceivedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[任务管理] 收到串口数据 - 检测室ID:{e.RoomId}, 检测室名称:{e.RoomName}, 扫码数据:{e.ScanData}");
            
            // 在UI线程上处理扫码数据
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 找到对应的检测室
                var roomBoxList = RoomBoxLists?.FirstOrDefault(rbl => rbl.Room?.Id == e.RoomId);
                if (roomBoxList == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[任务管理] 未找到对应的检测室 - 检测室ID:{e.RoomId}");
                    return;
                }

                // 更新扫码编码显示
                roomBoxList.LastScannedCode = e.ScanData;
                
                // 验证扫码编码是否匹配规则
                if (SelectedRuleItem == null)
                {
                    roomBoxList.ScanStatus = "未选择规则";
                    return;
                }

                // 解析规则中的物流盒编号
                var ruleLogisticsBoxNos = SelectedRuleItem.LogisticsBoxNos?.Split(',')
                    .Where(b => !string.IsNullOrWhiteSpace(b))
                    .Select(b => b.Trim())
                    .ToList() ?? new System.Collections.Generic.List<string>();

                // 检查扫码的编码是否在规则的物流盒列表中
                if (ruleLogisticsBoxNos.Contains(e.ScanData))
                {
                    // 匹配成功，添加到对应检测室的物流盒列表（串口扫码的数据才显示在检测室）
                    var boxCode = $"物流盒编码{e.ScanData}";
                    
                    // 添加到对应检测室的物流盒列表（这是串口扫码的数据，显示在检测室中）
                    if (!roomBoxList.Boxes.Contains(boxCode))
                    {
                        roomBoxList.Boxes.Add(boxCode);
                    }
                    
                    // 注意：串口扫码的数据不添加到 LogisticsBoxList（录入信息列表）
                    // LogisticsBoxList 只用于显示手动输入的数据
                    
                    roomBoxList.ScanStatus = "匹配成功";
                }
                else
                {
                    // 匹配失败
                    roomBoxList.ScanStatus = "匹配失败";
                }
            });
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
                    
                    // 如果检测室启用了扫码器，尝试打开连接
                    if (room.ScannerIsEnabled && !string.IsNullOrWhiteSpace(room.ScannerPortName))
                    {
                        System.Diagnostics.Debug.WriteLine($"[任务管理] 尝试打开串口连接 - 检测室ID:{room.Id}, 检测室名称:{room.RoomName}, 串口:{room.ScannerPortName}");
                        _ = Task.Run(async () =>
                        {
                            var result = await _scannerService.OpenConnectionAsync(room);
                            System.Diagnostics.Debug.WriteLine($"[任务管理] 串口连接结果 - 检测室ID:{room.Id}, 结果:{result}");
                        });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[任务管理] 检测室未启用扫码器或未配置串口 - 检测室ID:{room.Id}, 启用:{room.ScannerIsEnabled}, 串口:{room.ScannerPortName}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载检测室失败: {ex.Message}");
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
                    // 手动输入只添加到录入信息列表，不自动分配到检测室
                    // 只有通过串口扫码器接收的数据才会显示在检测室中
                    LogisticsBoxList.Add(formattedCode);
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
            set => SetProperty(ref _selectedRuleItem, value);
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

    }
}

