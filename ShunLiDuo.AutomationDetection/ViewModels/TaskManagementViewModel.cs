using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Collections.Generic;
using System;
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
        private bool _isInitializing;
        private DispatcherTimer _plcStatusTimer;
        private readonly object _lockObject = new object(); // 并发控制锁
        private readonly Dictionary<int, bool> _roomControlLock = new Dictionary<int, bool>(); // 防止同一检测室并发控制

        public bool IsPlcConnected
        {
            get => _isPlcConnected;
            set
            {
                SetProperty(ref _isPlcConnected, value);
                InitializeCommand?.RaiseCanExecuteChanged();
            }
        }

        public bool IsInitializing
        {
            get => _isInitializing;
            set
            {
                SetProperty(ref _isInitializing, value);
                InitializeCommand?.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand InitializeCommand { get; private set; }

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
            
            // 初始化命令
            InitializeCommand = new DelegateCommand(OnInitialize, () => !_isInitializing && IsPlcConnected);
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
                    // 匹配成功
                    var boxCode = $"物流盒编码{e.ScanData}";
                    if (!roomBoxList.Boxes.Contains(boxCode))
                    {
                        roomBoxList.Boxes.Add(boxCode);
                    }
                    roomBoxList.ScanStatus = "匹配成功";
                    
                    // 异步执行匹配流程（不阻塞UI）
                    _ = HandleMatchedScanAsync(e.RoomId, e.RoomName, e.ScanData);
                }
                else
                {
                    // 匹配失败
                    roomBoxList.ScanStatus = "匹配失败";
                    
                    // 异步执行不匹配流程（不阻塞UI）
                    _ = HandleUnmatchedScanAsync(e.RoomId, e.RoomName);
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
                
                // 初始化所有检测室的初始状态
                _ = InitializeAllRoomsAsync(); // 异步执行，不阻塞
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

        /// <summary>
        /// 处理匹配的扫码流程
        /// </summary>
        private async Task HandleMatchedScanAsync(int roomId, string roomName, string scanData)
        {
            // 防止同一检测室并发控制（原子操作）
            lock (_lockObject)
            {
                if (_roomControlLock.ContainsKey(roomId) && _roomControlLock[roomId])
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 检测室 {roomName} 正在执行控制流程，忽略本次请求");
                    return;
                }
                _roomControlLock[roomId] = true;
            }

            try
            {
                // 检查PLC连接状态
                if (!_s7Service.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] PLC未连接，无法执行控制 - 检测室: {roomName}");
                    return;
                }

                // 从检测室获取PLC配置
                var room = DetectionRooms.FirstOrDefault(r => r.Id == roomId);
                if (room == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 检测室 {roomName} 不存在");
                    return;
                }

                // 检查PLC配置是否完整
                if (string.IsNullOrWhiteSpace(room.Cylinder1ExtendAddress) || 
                    string.IsNullOrWhiteSpace(room.Cylinder2ExtendAddress) || 
                    string.IsNullOrWhiteSpace(room.SensorAddress))
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 检测室 {roomName} 未配置PLC监控参数");
                    return;
                }

                // 气缸1=阻挡气缸，气缸2=推箱气缸
                string blockingCylinderExtendAddress = room.Cylinder1ExtendAddress;       // 阻挡气缸伸出控制地址
                string blockingCylinderRetractAddress = room.Cylinder1RetractAddress;     // 阻挡气缸收缩控制地址
                string blockingCylinderExtendFeedbackAddress = room.Cylinder1ExtendFeedbackAddress; // 阻挡气缸伸出反馈地址
                string blockingCylinderRetractFeedbackAddress = room.Cylinder1RetractFeedbackAddress; // 阻挡气缸收缩反馈地址
                string pushCylinderExtendAddress = room.Cylinder2ExtendAddress;           // 推箱气缸伸出控制地址
                string pushCylinderRetractAddress = room.Cylinder2RetractAddress;         // 推箱气缸收缩控制地址
                string pushCylinderExtendFeedbackAddress = room.Cylinder2ExtendFeedbackAddress; // 推箱气缸伸出反馈地址
                string pushCylinderRetractFeedbackAddress = room.Cylinder2RetractFeedbackAddress; // 推箱气缸收缩反馈地址
                string sensorAddress = room.SensorAddress;                                // 传感器地址

                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 开始匹配流程 - 检测室: {roomName}, 编码: {scanData}");

                // 1. 检测推箱气缸位置，必须先将推箱气缸置于收缩状态
                bool pushCylinderExtended = await CheckCylinderPositionAsync(pushCylinderExtendFeedbackAddress);
                if (pushCylinderExtended)
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 推箱气缸在伸出状态，开始收缩 - 检测室: {roomName}");
                    bool retracted = await ControlCylinderAsync(
                        pushCylinderExtendAddress, 
                        pushCylinderRetractAddress, 
                        false,  // 收缩
                        pushCylinderRetractFeedbackAddress,  // 反馈地址
                        true,   // 目标反馈值（收缩到位应为true）
                        room.PushCylinderRetractTimeout    // 使用配置的超时时间
                    );
                    
                    if (!retracted)
                    {
                        System.Diagnostics.Debug.WriteLine($"[控制逻辑] 推箱气缸收缩超时 - 检测室: {roomName}");
                        // 尝试恢复阻挡气缸（如果已经收缩了）
                        try
                        {
                            var currentBlockingState = await CheckCylinderPositionAsync(blockingCylinderExtendFeedbackAddress);
                            if (!currentBlockingState)
                            {
                                await ControlCylinderAsync(blockingCylinderExtendAddress, blockingCylinderRetractAddress, true, blockingCylinderExtendFeedbackAddress, true, room.BlockingCylinderExtendTimeout);
                            }
                        }
                        catch { }
                        return;
                    }
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 推箱气缸已收缩到位 - 检测室: {roomName}");
                }

                // 1.5 卡控验证：确认推箱气缸已收缩到位（安全卡控）
                bool pushCylinderRetractedConfirmed = await CheckCylinderPositionAsync(pushCylinderRetractFeedbackAddress);
                if (!pushCylinderRetractedConfirmed)
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 安全卡控失败：推箱气缸未收缩到位，尝试收缩 - 检测室: {roomName}");
                    // 如果之前已经尝试收缩但失败了，这里会再次尝试
                    bool retracted = await ControlCylinderAsync(
                        pushCylinderExtendAddress, 
                        pushCylinderRetractAddress, 
                        false,  // 收缩
                        pushCylinderRetractFeedbackAddress,  // 反馈地址
                        true,   // 目标反馈值（收缩到位应为true）
                        room.PushCylinderRetractTimeout
                    );
                    if (!retracted)
                    {
                        System.Diagnostics.Debug.WriteLine($"[控制逻辑] 推箱气缸收缩失败，禁止放行 - 检测室: {roomName}");
                        // 尝试恢复阻挡气缸（如果已经收缩了）
                        try
                        {
                            var currentBlockingState = await CheckCylinderPositionAsync(blockingCylinderExtendFeedbackAddress);
                            if (!currentBlockingState)
                            {
                                await ControlCylinderAsync(blockingCylinderExtendAddress, blockingCylinderRetractAddress, true, blockingCylinderExtendFeedbackAddress, true, room.BlockingCylinderExtendTimeout);
                            }
                        }
                        catch { }
                        return;
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 安全卡控通过：推箱气缸已收缩到位，允许放行 - 检测室: {roomName}");

                // 2. 阻挡气缸收缩放行
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 阻挡气缸开始收缩放行 - 检测室: {roomName}");
                bool blockingRetracted = await ControlCylinderAsync(
                    blockingCylinderExtendAddress, 
                    blockingCylinderRetractAddress, 
                    false,  // 收缩
                    blockingCylinderRetractFeedbackAddress,  // 反馈地址
                    true,   // 目标反馈值（收缩到位应为true）
                    room.BlockingCylinderRetractTimeout    // 使用配置的超时时间
                );
                
                if (!blockingRetracted)
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 阻挡气缸收缩超时 - 检测室: {roomName}");
                    // 尝试恢复阻挡气缸（等待反馈，确保恢复到安全状态）
                    await ControlCylinderAsync(blockingCylinderExtendAddress, blockingCylinderRetractAddress, true, blockingCylinderExtendFeedbackAddress, true, room.BlockingCylinderExtendTimeout);
                    return;
                }
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 阻挡气缸已收缩到位 - 检测室: {roomName}");

                // 3. 等待传感器检测到物流盒到达推箱位置
                // 注意：传感器信号仅在此阶段有效（阻挡气缸放行后，推箱动作执行前）
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 开始等待传感器检测物流盒 - 检测室: {roomName}, 传感器地址: {sensorAddress}, 超时: {room.SensorDetectTimeout}ms");
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 传感器检测窗口已开启 - 检测室: {roomName}");
                bool sensorDetected = await WaitForSensorAsync(sensorAddress, true, room.SensorDetectTimeout, roomName);
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 传感器检测窗口已关闭 - 检测室: {roomName}, 检测结果: {(sensorDetected ? "成功" : "超时")}");
                
                if (sensorDetected)
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 传感器检测到物流盒 - 检测室: {roomName}");
                    
                    // 等待配置的延时时间，确保物流盒完全到位
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 等待{room.SensorConfirmDelayTime}ms确保物流盒完全到位 - 检测室: {roomName}");
                    await Task.Delay(room.SensorConfirmDelayTime);
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 物流盒到位确认完成，开始推箱动作 - 检测室: {roomName}");
                    
                    // 4. 执行推箱动作（伸出推箱气缸，等待反馈）
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 开始推箱动作 - 检测室: {roomName}");
                    bool pushExtended = await ControlCylinderAsync(
                        pushCylinderExtendAddress, 
                        pushCylinderRetractAddress, 
                        true,  // 伸出
                        pushCylinderExtendFeedbackAddress,  // 反馈地址
                        true,   // 目标反馈值（伸出到位应为true）
                        room.PushCylinderExtendTimeout    // 使用配置的超时时间
                    );
                    
                    if (!pushExtended)
                    {
                        System.Diagnostics.Debug.WriteLine($"[控制逻辑] 推箱气缸伸出超时 - 检测室: {roomName}");
                        // 推箱超时也要恢复阻挡气缸
                        await ControlCylinderAsync(blockingCylinderExtendAddress, blockingCylinderRetractAddress, true, blockingCylinderExtendFeedbackAddress, true, room.BlockingCylinderExtendTimeout);
                        return;
                    }
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 推箱气缸已伸出到位，推箱完成 - 检测室: {roomName}");
                    
                    // 5. 阻挡气缸恢复阻挡状态（伸出，等待反馈）
                    bool blockingExtended = await ControlCylinderAsync(
                        blockingCylinderExtendAddress, 
                        blockingCylinderRetractAddress, 
                        true,  // 伸出
                        blockingCylinderExtendFeedbackAddress,  // 反馈地址
                        true,   // 目标反馈值（伸出到位应为true）
                        room.BlockingCylinderExtendTimeout    // 使用配置的超时时间
                    );
                    
                    if (!blockingExtended)
                    {
                        System.Diagnostics.Debug.WriteLine($"[控制逻辑] 阻挡气缸伸出超时 - 检测室: {roomName}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[控制逻辑] 阻挡气缸已伸出到位 - 检测室: {roomName}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 匹配流程完成 - 检测室: {roomName}, 编码: {scanData}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 传感器检测超时，未检测到物流盒 - 检测室: {roomName}");
                    // 超时后也要恢复阻挡气缸
                    bool blockingExtended = await ControlCylinderAsync(
                        blockingCylinderExtendAddress, 
                        blockingCylinderRetractAddress, 
                        true,  // 伸出
                        blockingCylinderExtendFeedbackAddress,  // 反馈地址
                        true,   // 目标反馈值（伸出到位应为true）
                        room.BlockingCylinderExtendTimeout    // 使用配置的超时时间
                    );
                    
                    if (!blockingExtended)
                    {
                        System.Diagnostics.Debug.WriteLine($"[控制逻辑] 阻挡气缸伸出超时 - 检测室: {roomName}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[控制逻辑] 阻挡气缸已伸出到位 - 检测室: {roomName}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 处理匹配扫码失败 - 检测室: {roomName}, 错误: {ex.Message}");
                // 异常时恢复设备到安全状态
                await RestoreRoomToSafeStateAsync(roomId, roomName, true);
            }
            finally
            {
                lock (_lockObject)
                {
                    _roomControlLock[roomId] = false;
                }
            }
        }

        /// <summary>
        /// 处理不匹配的扫码流程
        /// </summary>
        private async Task HandleUnmatchedScanAsync(int roomId, string roomName)
        {
            // 防止同一检测室并发控制（原子操作）
            lock (_lockObject)
            {
                if (_roomControlLock.ContainsKey(roomId) && _roomControlLock[roomId])
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 检测室 {roomName} 正在执行控制流程，忽略本次请求");
                    return;
                }
                _roomControlLock[roomId] = true;
            }

            try
            {
                // 检查PLC连接状态
                if (!_s7Service.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] PLC未连接，无法执行控制 - 检测室: {roomName}");
                    return;
                }

                // 从检测室获取PLC配置
                var room = DetectionRooms.FirstOrDefault(r => r.Id == roomId);
                if (room == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 检测室 {roomName} 不存在");
                    return;
                }

                // 检查PLC配置是否完整
                // 注意：不匹配流程不需要传感器地址，所以不检查 SensorAddress
                if (string.IsNullOrWhiteSpace(room.Cylinder1ExtendAddress) || 
                    string.IsNullOrWhiteSpace(room.Cylinder2ExtendAddress))
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 检测室 {roomName} 未配置PLC监控参数");
                    return;
                }

                // 气缸1=阻挡气缸，气缸2=推箱气缸
                string blockingCylinderExtendAddress = room.Cylinder1ExtendAddress;
                string blockingCylinderRetractAddress = room.Cylinder1RetractAddress;
                string blockingCylinderExtendFeedbackAddress = room.Cylinder1ExtendFeedbackAddress; // 阻挡气缸伸出反馈地址
                string blockingCylinderRetractFeedbackAddress = room.Cylinder1RetractFeedbackAddress; // 阻挡气缸收缩反馈地址
                string pushCylinderExtendAddress = room.Cylinder2ExtendAddress;
                string pushCylinderRetractAddress = room.Cylinder2RetractAddress;
                string pushCylinderExtendFeedbackAddress = room.Cylinder2ExtendFeedbackAddress;
                string pushCylinderRetractFeedbackAddress = room.Cylinder2RetractFeedbackAddress;

                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 开始不匹配流程 - 检测室: {roomName}");

                // 1. 检测推箱气缸位置，必须先将推箱气缸处于伸出状态
                bool pushCylinderRetracted = await CheckCylinderPositionAsync(pushCylinderRetractFeedbackAddress);
                if (pushCylinderRetracted)
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 推箱气缸在收缩状态，开始伸出 - 检测室: {roomName}");
                    bool extended = await ControlCylinderAsync(
                        pushCylinderExtendAddress, 
                        pushCylinderRetractAddress, 
                        true,  // 伸出
                        pushCylinderExtendFeedbackAddress,  // 反馈地址
                        true,   // 目标反馈值（伸出到位应为true）
                        room.PushCylinderExtendTimeout    // 使用配置的超时时间
                    );
                    
                    if (!extended)
                    {
                        System.Diagnostics.Debug.WriteLine($"[控制逻辑] 推箱气缸伸出超时 - 检测室: {roomName}");
                        // 尝试恢复阻挡气缸（如果已经收缩了）
                        try
                        {
                            var currentBlockingState = await CheckCylinderPositionAsync(blockingCylinderExtendFeedbackAddress);
                            if (!currentBlockingState)
                            {
                                await ControlCylinderAsync(blockingCylinderExtendAddress, blockingCylinderRetractAddress, true, blockingCylinderExtendFeedbackAddress, true, room.BlockingCylinderExtendTimeout);
                            }
                        }
                        catch { }
                        return;
                    }
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 推箱气缸已伸出到位 - 检测室: {roomName}");
                }

                // 1.5 卡控验证：确认推箱气缸已伸出到位（安全卡控）
                bool pushCylinderExtendedConfirmed = await CheckCylinderPositionAsync(pushCylinderExtendFeedbackAddress);
                if (!pushCylinderExtendedConfirmed)
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 安全卡控失败：推箱气缸未伸出到位，尝试伸出 - 检测室: {roomName}");
                    // 如果之前已经尝试伸出但失败了，这里会再次尝试
                    bool extended = await ControlCylinderAsync(
                        pushCylinderExtendAddress, 
                        pushCylinderRetractAddress, 
                        true,  // 伸出
                        pushCylinderExtendFeedbackAddress,  // 反馈地址
                        true,   // 目标反馈值（伸出到位应为true）
                        room.PushCylinderExtendTimeout
                    );
                    if (!extended)
                    {
                        System.Diagnostics.Debug.WriteLine($"[控制逻辑] 推箱气缸伸出失败，禁止放行 - 检测室: {roomName}");
                        // 尝试恢复阻挡气缸（如果已经收缩了）
                        try
                        {
                            var currentBlockingState = await CheckCylinderPositionAsync(blockingCylinderExtendFeedbackAddress);
                            if (!currentBlockingState)
                            {
                                await ControlCylinderAsync(blockingCylinderExtendAddress, blockingCylinderRetractAddress, true, blockingCylinderExtendFeedbackAddress, true, room.BlockingCylinderExtendTimeout);
                            }
                        }
                        catch { }
                        return;
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 安全卡控通过：推箱气缸已伸出到位，允许放行 - 检测室: {roomName}");

                // 2. 阻挡气缸收缩放行
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 阻挡气缸开始收缩放行 - 检测室: {roomName}");
                bool blockingRetracted = await ControlCylinderAsync(
                    blockingCylinderExtendAddress, 
                    blockingCylinderRetractAddress, 
                    false,  // 收缩
                    blockingCylinderRetractFeedbackAddress,  // 反馈地址
                    true,   // 目标反馈值（收缩到位应为true）
                    room.BlockingCylinderRetractTimeout    // 使用配置的超时时间
                );
                
                if (!blockingRetracted)
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 阻挡气缸收缩超时 - 检测室: {roomName}");
                    // 尝试恢复阻挡气缸（等待反馈，确保恢复到安全状态）
                    await ControlCylinderAsync(blockingCylinderExtendAddress, blockingCylinderRetractAddress, true, blockingCylinderExtendFeedbackAddress, true, room.BlockingCylinderExtendTimeout);
                    return;
                }
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 阻挡气缸已收缩到位 - 检测室: {roomName}");

                // 3. 等待配置的放行时间
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 等待{room.PassageDelayTime}ms放行时间 - 检测室: {roomName}");
                await Task.Delay(room.PassageDelayTime);

                // 4. 阻挡气缸恢复阻挡状态（伸出）
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 阻挡气缸恢复阻挡状态 - 检测室: {roomName}");
                bool blockingExtended = await ControlCylinderAsync(
                    blockingCylinderExtendAddress, 
                    blockingCylinderRetractAddress, 
                    true,  // 伸出
                    blockingCylinderExtendFeedbackAddress,  // 反馈地址
                    true,   // 目标反馈值（伸出到位应为true）
                    room.BlockingCylinderExtendTimeout    // 使用配置的超时时间
                );
                
                if (!blockingExtended)
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 阻挡气缸伸出超时 - 检测室: {roomName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 阻挡气缸已伸出到位 - 检测室: {roomName}");
                }
                
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 不匹配流程完成 - 检测室: {roomName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 处理不匹配扫码失败 - 检测室: {roomName}, 错误: {ex.Message}");
            }
            finally
            {
                _roomControlLock[roomId] = false;
            }
        }

        /// <summary>
        /// 初始化所有检测室的初始状态（系统启动时调用）
        /// </summary>
        public async Task<(int successCount, int failCount, List<string> failedRooms)> InitializeAllRoomsAsync()
        {
            int successCount = 0;
            int failCount = 0;
            var failedRooms = new List<string>();
            
            try
            {
                if (!_s7Service.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine($"[初始化] PLC未连接，无法初始化检测室状态");
                    return (0, 0, new List<string> { "PLC未连接" });
                }

                var rooms = await _detectionRoomService.GetAllRoomsAsync();
                
                foreach (var room in rooms)
                {
                    try
                    {
                        // 检查该检测室是否正在执行控制流程
                        lock (_lockObject)
                        {
                            if (_roomControlLock.ContainsKey(room.Id) && _roomControlLock[room.Id])
                            {
                                System.Diagnostics.Debug.WriteLine($"[初始化] 检测室 {room.RoomName} 正在执行控制流程，跳过初始化");
                                continue;
                            }
                        }
                        
                        bool success = await InitializeRoomStateAsync(room.Id, room.RoomName);
                        if (success)
                        {
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                            failedRooms.Add(room.RoomName);
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        failedRooms.Add(room.RoomName);
                        System.Diagnostics.Debug.WriteLine($"[初始化] 初始化检测室 {room.RoomName} 失败: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[初始化] 所有检测室初始化完成 - 成功: {successCount}, 失败: {failCount}");
                return (successCount, failCount, failedRooms);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[初始化] 初始化检测室状态失败: {ex.Message}");
                return (successCount, failCount, failedRooms);
            }
        }

        /// <summary>
        /// 初始化按钮点击处理
        /// </summary>
        private async void OnInitialize()
        {
            IsInitializing = true;
            try
            {
                if (!_s7Service.IsConnected)
                {
                    System.Windows.MessageBox.Show("PLC未连接，无法执行初始化", "提示", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var (successCount, failCount, failedRooms) = await InitializeAllRoomsAsync();
                
                // 根据结果显示不同的提示消息
                if (failCount == 0)
                {
                    System.Windows.MessageBox.Show($"所有检测室初始化成功！\n成功: {successCount} 个", "初始化完成", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Information);
                }
                else if (successCount > 0)
                {
                    string failedRoomsText = string.Join("、", failedRooms);
                    System.Windows.MessageBox.Show($"初始化部分成功\n成功: {successCount} 个\n失败: {failCount} 个\n失败的检测室: {failedRoomsText}", "初始化结果", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Warning);
                }
                else
                {
                    string failedRoomsText = string.Join("、", failedRooms);
                    System.Windows.MessageBox.Show($"所有检测室初始化失败！\n失败的检测室: {failedRoomsText}\n\n请检查：\n1. PLC连接状态\n2. 反馈地址配置是否正确\n3. 气缸是否正常工作", "初始化失败", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"初始化失败: {ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsInitializing = false;
            }
        }

        /// <summary>
        /// 初始化单个检测室的初始状态
        /// </summary>
        /// <returns>返回true表示初始化成功（所有气缸都收到反馈），false表示失败</returns>
        private async Task<bool> InitializeRoomStateAsync(int roomId, string roomName)
        {
            try
            {
                // 从检测室获取PLC配置
                var room = DetectionRooms.FirstOrDefault(r => r.Id == roomId);
                if (room == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[初始化] 检测室 {roomName} 不存在");
                    return false;
                }

                // 检查PLC配置是否完整
                if (string.IsNullOrWhiteSpace(room.Cylinder1ExtendAddress) || 
                    string.IsNullOrWhiteSpace(room.Cylinder2ExtendAddress))
                {
                    System.Diagnostics.Debug.WriteLine($"[初始化] 检测室 {roomName} 未配置PLC控制地址，跳过初始化");
                    return false;
                }
                
                // 检查反馈地址是否配置（初始化必须要有反馈地址才能确认成功）
                if (string.IsNullOrWhiteSpace(room.Cylinder1ExtendFeedbackAddress) || 
                    string.IsNullOrWhiteSpace(room.Cylinder2RetractFeedbackAddress))
                {
                    System.Diagnostics.Debug.WriteLine($"[初始化] 检测室 {roomName} 未配置反馈地址，无法确认初始化是否成功");
                    return false;
                }

                // 气缸1=阻挡气缸（初始伸出），气缸2=推箱气缸（初始收缩）
                string blockingCylinderExtendAddress = room.Cylinder1ExtendAddress;
                string blockingCylinderRetractAddress = room.Cylinder1RetractAddress;
                string blockingCylinderExtendFeedbackAddress = room.Cylinder1ExtendFeedbackAddress;
                string pushCylinderExtendAddress = room.Cylinder2ExtendAddress;
                string pushCylinderRetractAddress = room.Cylinder2RetractAddress;
                string pushCylinderRetractFeedbackAddress = room.Cylinder2RetractFeedbackAddress;

                System.Diagnostics.Debug.WriteLine($"[初始化] 开始初始化检测室 {roomName} 的状态");

                // 1. 阻挡气缸初始为伸出状态（等待反馈到位，确保初始化成功）
                bool blockingExtended = await ControlCylinderAsync(
                    blockingCylinderExtendAddress, 
                    blockingCylinderRetractAddress, 
                    true,  // 伸出
                    blockingCylinderExtendFeedbackAddress,  // 反馈地址
                    true,   // 目标反馈值（伸出到位应为true）
                    room.BlockingCylinderExtendTimeout    // 使用配置的超时时间
                );
                
                if (blockingExtended)
                {
                    System.Diagnostics.Debug.WriteLine($"[初始化] 阻挡气缸已伸出到位 - 检测室: {roomName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[初始化] 阻挡气缸伸出超时或未到位 - 检测室: {roomName}");
                }

                // 2. 推箱气缸初始为收缩状态（等待反馈到位，确保初始化成功）
                bool pushRetracted = await ControlCylinderAsync(
                    pushCylinderExtendAddress, 
                    pushCylinderRetractAddress, 
                    false,  // 收缩
                    pushCylinderRetractFeedbackAddress,  // 反馈地址
                    true,   // 目标反馈值（收缩到位应为true）
                    room.PushCylinderRetractTimeout    // 使用配置的超时时间
                );
                
                if (pushRetracted)
                {
                    System.Diagnostics.Debug.WriteLine($"[初始化] 推箱气缸已收缩到位 - 检测室: {roomName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[初始化] 推箱气缸收缩超时或未到位 - 检测室: {roomName}");
                }

                // 只有两个气缸都收到反馈才算初始化成功
                bool success = blockingExtended && pushRetracted;
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"[初始化] 检测室 {roomName} 初始化成功（阻挡气缸=伸出到位，推箱气缸=收缩到位）");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[初始化] 检测室 {roomName} 初始化失败 - 阻挡气缸: {(blockingExtended ? "成功" : "失败")}, 推箱气缸: {(pushRetracted ? "成功" : "失败")}");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[初始化] 初始化检测室 {roomName} 状态失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 控制气缸动作（伸出或收缩），使用脉冲+保持信号策略
        /// 如果提供了反馈地址，会在反馈到位前持续保持信号为1，确保气缸动作完成
        /// 如果没有提供反馈地址，则只发送脉冲信号（写1后立即清零）
        /// </summary>
        private async Task<bool> ControlCylinderAsync(string extendAddress, string retractAddress, bool extend, string feedbackAddress = null, bool targetFeedbackValue = true, int timeoutMs = 5000)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(extendAddress) || string.IsNullOrWhiteSpace(retractAddress))
                    return false;

                // 先确保两个点都是0（安全状态）
                await _s7Service.WriteBoolAsync(extendAddress, false);
                await _s7Service.WriteBoolAsync(retractAddress, false);
                
                // 短暂延迟，确保清零完成
                await Task.Delay(10);

                if (extend)
                {
                    // 伸出：把 extendAddress 置1
                    await _s7Service.WriteBoolAsync(extendAddress, true);
                    
                    // 如果提供了反馈地址，持续保持信号直到反馈到位
                    if (!string.IsNullOrWhiteSpace(feedbackAddress) && timeoutMs > 0)
                    {
                        var startTime = DateTime.Now;
                        bool feedbackReceived = false;
                        
                        System.Diagnostics.Debug.WriteLine($"[辅助方法] 保持信号等待反馈 - 控制地址: {extendAddress}, 反馈地址: {feedbackAddress}, 目标值: {targetFeedbackValue}, 超时: {timeoutMs}ms");
                        
                        // 循环保持信号，每100ms检查一次反馈
                        while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
                        {
                            try
                            {
                                bool currentFeedback = await _s7Service.ReadBoolAsync(feedbackAddress);
                                if (currentFeedback == targetFeedbackValue)
                                {
                                    feedbackReceived = true;
                                    System.Diagnostics.Debug.WriteLine($"[辅助方法] 反馈到位 - 地址: {feedbackAddress}, 值: {currentFeedback}");
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[辅助方法] 读取反馈地址失败: {ex.Message}");
                            }
                            
                            // 保持信号为1（每100ms刷新一次）
                            await _s7Service.WriteBoolAsync(extendAddress, true);
                            await _s7Service.WriteBoolAsync(retractAddress, false); // 确保另一个方向为0
                            await Task.Delay(100); // 每100ms刷新一次
                        }
                        
                        // 反馈到位后，清零信号
                        await _s7Service.WriteBoolAsync(extendAddress, false);
                        await _s7Service.WriteBoolAsync(retractAddress, false);
                        
                        if (!feedbackReceived)
                        {
                            System.Diagnostics.Debug.WriteLine($"[辅助方法] 保持信号超时 - 控制地址: {extendAddress}, 反馈地址: {feedbackAddress}");
                        }
                        
                        return feedbackReceived;
                    }
                    else
                    {
                        // 如果没有反馈地址，使用原来的脉冲方式（立即清零）
                        await _s7Service.WriteBoolAsync(extendAddress, false);
                        await _s7Service.WriteBoolAsync(retractAddress, false);
                        return true;
                    }
                }
                else
                {
                    // 收缩：把 retractAddress 置1
                    await _s7Service.WriteBoolAsync(retractAddress, true);
                    
                    // 如果提供了反馈地址，持续保持信号直到反馈到位
                    if (!string.IsNullOrWhiteSpace(feedbackAddress) && timeoutMs > 0)
                    {
                        var startTime = DateTime.Now;
                        bool feedbackReceived = false;
                        
                        System.Diagnostics.Debug.WriteLine($"[辅助方法] 保持信号等待反馈 - 控制地址: {retractAddress}, 反馈地址: {feedbackAddress}, 目标值: {targetFeedbackValue}, 超时: {timeoutMs}ms");
                        
                        // 循环保持信号，每100ms检查一次反馈
                        while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
                        {
                            try
                            {
                                bool currentFeedback = await _s7Service.ReadBoolAsync(feedbackAddress);
                                if (currentFeedback == targetFeedbackValue)
                                {
                                    feedbackReceived = true;
                                    System.Diagnostics.Debug.WriteLine($"[辅助方法] 反馈到位 - 地址: {feedbackAddress}, 值: {currentFeedback}");
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[辅助方法] 读取反馈地址失败: {ex.Message}");
                            }
                            
                            // 保持信号为1（每100ms刷新一次）
                            await _s7Service.WriteBoolAsync(retractAddress, true);
                            await _s7Service.WriteBoolAsync(extendAddress, false); // 确保另一个方向为0
                            await Task.Delay(100); // 每100ms刷新一次
                        }
                        
                        // 反馈到位后，清零信号
                        await _s7Service.WriteBoolAsync(extendAddress, false);
                        await _s7Service.WriteBoolAsync(retractAddress, false);
                        
                        if (!feedbackReceived)
                        {
                            System.Diagnostics.Debug.WriteLine($"[辅助方法] 保持信号超时 - 控制地址: {retractAddress}, 反馈地址: {feedbackAddress}");
                        }
                        
                        return feedbackReceived;
                    }
                    else
                    {
                        // 如果没有反馈地址，使用原来的脉冲方式（立即清零）
                        await _s7Service.WriteBoolAsync(extendAddress, false);
                        await _s7Service.WriteBoolAsync(retractAddress, false);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[辅助方法] 控制气缸失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查气缸位置（通过反馈地址）
        /// </summary>
        /// <param name="feedbackAddress">反馈地址</param>
        /// <returns>返回true表示气缸在目标位置（根据使用场景：伸出反馈地址返回true=伸出到位，收缩反馈地址返回true=收缩到位）</returns>
        private async Task<bool> CheckCylinderPositionAsync(string feedbackAddress)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(feedbackAddress))
                    return false;
                return await _s7Service.ReadBoolAsync(feedbackAddress);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[辅助方法] 检查气缸位置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 等待气缸到位
        /// </summary>
        private async Task<bool> WaitForCylinderPositionAsync(string feedbackAddress, bool targetValue, int timeoutMs)
        {
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
            {
                try
                {
                    bool currentValue = await _s7Service.ReadBoolAsync(feedbackAddress);
                    if (currentValue == targetValue)
                        return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[辅助方法] 读取气缸反馈地址失败: {ex.Message}");
                }
                
                await Task.Delay(100); // 每100ms检查一次
            }
            return false;
        }

        /// <summary>
        /// 等待传感器检测（仅在匹配流程的特定时间窗口内有效）
        /// </summary>
        /// <param name="sensorAddress">传感器地址</param>
        /// <param name="targetValue">目标值</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="roomName">检测室名称（用于日志）</param>
        /// <returns>如果检测到目标值返回true，否则返回false</returns>
        private async Task<bool> WaitForSensorAsync(string sensorAddress, bool targetValue, int timeoutMs, string roomName = "")
        {
            var startTime = DateTime.Now;
            
            System.Diagnostics.Debug.WriteLine($"[辅助方法] 传感器检测开始 - 地址: {sensorAddress}, 目标值: {targetValue}, 超时: {timeoutMs}ms, 检测室: {roomName}");
            
            // 先读取一次初始状态
            bool initialValue = false;
            try
            {
                initialValue = await _s7Service.ReadBoolAsync(sensorAddress);
                System.Diagnostics.Debug.WriteLine($"[辅助方法] 传感器初始状态 - 地址: {sensorAddress}, 值: {initialValue}, 检测室: {roomName}");
                
                // 如果初始状态已经是目标值，立即返回true（认为物流盒已经到位）
                // 这是因为阻挡气缸放行后，物流盒可能已经到达传感器位置
                if (initialValue == targetValue)
                {
                    System.Diagnostics.Debug.WriteLine($"[辅助方法] 传感器初始状态已满足目标值，立即返回 - 地址: {sensorAddress}, 检测室: {roomName}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[辅助方法] 读取传感器初始状态失败 - 地址: {sensorAddress}, 错误: {ex.Message}, 检测室: {roomName}");
            }
            
            // 等待传感器从初始状态变为目标值
            while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
            {
                try
                {
                    bool currentValue = await _s7Service.ReadBoolAsync(sensorAddress);
                    if (currentValue == targetValue)
                    {
                        var elapsedMs = (DateTime.Now - startTime).TotalMilliseconds;
                        System.Diagnostics.Debug.WriteLine($"[辅助方法] 传感器检测成功 - 地址: {sensorAddress}, 值: {currentValue}, 耗时: {elapsedMs:F0}ms, 检测室: {roomName}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[辅助方法] 读取传感器地址失败 - 地址: {sensorAddress}, 错误: {ex.Message}, 检测室: {roomName}");
                    // 读取失败时继续循环，不立即返回false
                }
                
                await Task.Delay(100); // 每100ms检查一次
            }
            
            var totalElapsedMs = (DateTime.Now - startTime).TotalMilliseconds;
            System.Diagnostics.Debug.WriteLine($"[辅助方法] 传感器检测超时 - 地址: {sensorAddress}, 目标值: {targetValue}, 耗时: {totalElapsedMs:F0}ms, 检测室: {roomName}");
            return false;
        }

        /// <summary>
        /// 恢复检测室到安全状态（异常时调用）
        /// </summary>
        /// <param name="roomId">检测室ID</param>
        /// <param name="roomName">检测室名称</param>
        /// <param name="isMatchedFlow">是否为匹配流程（用于确定推箱气缸的目标状态）</param>
        private async Task RestoreRoomToSafeStateAsync(int roomId, string roomName, bool isMatchedFlow)
        {
            try
            {
                var room = DetectionRooms.FirstOrDefault(r => r.Id == roomId);
                if (room == null) return;

                if (string.IsNullOrWhiteSpace(room.Cylinder1ExtendAddress) || 
                    string.IsNullOrWhiteSpace(room.Cylinder2ExtendAddress))
                    return;

                string blockingCylinderExtendAddress = room.Cylinder1ExtendAddress;
                string blockingCylinderRetractAddress = room.Cylinder1RetractAddress;
                string blockingCylinderExtendFeedbackAddress = room.Cylinder1ExtendFeedbackAddress;

                System.Diagnostics.Debug.WriteLine($"[异常恢复] 开始恢复检测室 {roomName} 到安全状态");

                // 1. 恢复阻挡气缸到伸出状态（阻挡位置）
                try
                {
                    await ControlCylinderAsync(
                        blockingCylinderExtendAddress, 
                        blockingCylinderRetractAddress, 
                        true,  // 伸出
                        blockingCylinderExtendFeedbackAddress, 
                        true, 
                        room.BlockingCylinderExtendTimeout
                    );
                    System.Diagnostics.Debug.WriteLine($"[异常恢复] 阻挡气缸已恢复到阻挡状态 - 检测室: {roomName}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[异常恢复] 恢复阻挡气缸失败 - 检测室: {roomName}, 错误: {ex.Message}");
                }

                // 2. 推箱气缸根据流程类型恢复到合适状态
                // 匹配流程：保持收缩状态（为下一次匹配准备）
                // 不匹配流程：保持伸出状态（为下一次放行准备）
                // 这里不主动改变推箱气缸状态，因为它应该在流程完成时已经处于正确状态
                System.Diagnostics.Debug.WriteLine($"[异常恢复] 检测室 {roomName} 安全状态恢复完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[异常恢复] 恢复检测室安全状态失败 - 检测室: {roomName}, 错误: {ex.Message}");
            }
        }

    }
}

