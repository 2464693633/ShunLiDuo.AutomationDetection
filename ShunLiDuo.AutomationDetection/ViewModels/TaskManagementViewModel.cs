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
using ShunLiDuo.AutomationDetection.Views;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    /// <summary>
    /// 条码类型枚举
    /// </summary>
    public enum BarcodeType
    {
        LogisticsBox,   // 物流盒编码 (H开头+数字)
        WorkOrder,      // 报工单编号 (其他格式)
        Inspector,      // 送检人 (纯数字)
        Unknown         // 未知类型
    }

    public class TaskManagementViewModel : BindableBase
    {
        public Prism.Commands.DelegateCommand InitializeStateCommand { get; private set; }
        public Prism.Commands.DelegateCommand<Models.TaskItem> DeleteTaskCommand { get; private set; }
        public Prism.Commands.DelegateCommand UnifiedInputConfirmCommand { get; private set; }
        private readonly IRuleService _ruleService;
        private readonly IDetectionRoomService _detectionRoomService;
        private readonly IS7CommunicationService _s7Service;
        private readonly IScannerCommunicationService _scannerService;
        private readonly IDetectionLogService _detectionLogService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAlarmRecordService _alarmRecordService;
        private readonly ICommunicationConfigService _configService;
        private readonly IMesDatabaseService _mesDatabaseService; // [修复] 补上字段声明
        private string _logisticsBoxCode;
        private string _workOrderNo;
        // private string _currentInspectorName; // 已移除，改用下面的分离模式



        private string _lastInspectorName; // 记录最后一次录入的送检人（逻辑值）
        private string _inspectorInputText; // 界面输入框的绑定值

        public string InspectorInputText
        {
            get => _inspectorInputText;
            set 
            {
                SetProperty(ref _inspectorInputText, value);
                // 如果输入不为空，则更新缓存的送检人信息
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _lastInspectorName = value;
                }
            }
        }
        private string _unifiedInputText; // [新增] 统一扫码输入

        public string UnifiedInputText
        {
            get => _unifiedInputText;
            set => SetProperty(ref _unifiedInputText, value);
        }

        private RuleItem _selectedRuleItem;
        private ObservableCollection<Models.TaskItem> _tasks;
        private ObservableCollection<RuleItem> _rules;
        private ObservableCollection<Models.DetectionRoomItem> _detectionRooms;
        private ObservableCollection<string> _logisticsBoxList;
        private ObservableCollection<Models.RoomBoxList> _roomBoxLists;
        private bool _isPlcConnected;
        private DispatcherTimer _plcStatusTimer;
        private readonly object _lockObject = new object(); // 并发控制锁
        private readonly Dictionary<int, bool> _roomControlLock = new Dictionary<int, bool>(); // 防止同一检测室并发控制
        private System.Threading.CancellationTokenSource _loadingCurveCts; // 上料弯道控制循环取消令牌
        private System.Threading.CancellationTokenSource _unloadingCurveCts; // 下料弯道控制循环取消令牌
        private WorkMode _workMode = WorkMode.Standard; // 工作模式，默认标准模式

        public bool IsPlcConnected
        {
            get => _isPlcConnected;
            set => SetProperty(ref _isPlcConnected, value);
        }


        public TaskManagementViewModel(
            IRuleService ruleService, 
            IDetectionRoomService detectionRoomService, 
            IS7CommunicationService s7Service,
            IScannerCommunicationService scannerService,
            IDetectionLogService detectionLogService,
            ICurrentUserService currentUserService,
            IAlarmRecordService alarmRecordService,
            ICommunicationConfigService configService,
            IMesDatabaseService mesDatabaseService) // [新增] 注入参数
        {
            try
            {
                _ruleService = ruleService;
                _detectionRoomService = detectionRoomService;
                _s7Service = s7Service;
                _scannerService = scannerService;
                _detectionLogService = detectionLogService;
                _currentUserService = currentUserService;
                _alarmRecordService = alarmRecordService;
                _configService = configService;
                _mesDatabaseService = mesDatabaseService; // [新增] 赋值
                
                // 初始化命令
                InitializeStateCommand = new Prism.Commands.DelegateCommand(ExecuteInitializeState);
                DeleteTaskCommand = new Prism.Commands.DelegateCommand<Models.TaskItem>(ExecuteDeleteTask);
                UnifiedInputConfirmCommand = new Prism.Commands.DelegateCommand(ExecuteUnifiedInputConfirm); // [新增] 统一输入命令
                
                // [新增] 异步加载工作模式配置
                LoadWorkModeConfigAsync();
                
                InitializeData();
                LoadRulesAsync();
                LoadDetectionRoomsAsync();
                InitializeGlobalScannersAsync();
                
                // 初始化PLC连接状态
                UpdatePlcConnectionStatus();
                
                // 监听连接状态变化
                _s7Service.ConnectionStatusChanged += S7Service_ConnectionStatusChanged;
                
                // 监听扫码数据接收事件
                _scannerService.DataReceived += ScannerService_DataReceived;
                
                // 监听扫码器连接状态变化事件
                _scannerService.ConnectionStatusChanged += ScannerService_ConnectionStatusChanged;
                
                // 启动定时器定期检查连接状态（每秒检查一次）
                _plcStatusTimer = new DispatcherTimer();
                _plcStatusTimer.Interval = System.TimeSpan.FromSeconds(1);
                _plcStatusTimer.Tick += (s, e) => 
                {
                    UpdatePlcConnectionStatus();
                    // UpdateScannerConnectionStatus(); // 移除轮询，避免与后台连接线程发生锁竞争导致UI卡死
                };
                _plcStatusTimer.Start();
                
                // 初始更新一次状态
                UpdateScannerConnectionStatus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[严重错误] TaskManagementViewModel 初始化失败: {ex.Message}");
                // 使用 Prism 的 Dialog Service 或者直接 MessageBox 提示（ViewModel 中尽量避免 UI，但此处为调试关键）
                // 暂时不弹出 MessageBox 避免此时 UI 线程问题，改用更显眼的 Log
                // 实际在 View 初始化的 try-catch 中更好
            }
        }
        
        /// <summary>
        /// 加载工作模式配置
        /// </summary>
        private async void LoadWorkModeConfigAsync()
        {
            try
            {
                var config = await _configService.GetConfigAsync();
                _workMode = config.Mode;
                
                System.Diagnostics.Debug.WriteLine($"[工作模式] 当前模式: {(_workMode == WorkMode.Standard ? "标准模式" : "简易模式")}");
                
                // 简易模式下不启动PLC连接和气缸控制
                if (_workMode == WorkMode.Simple)
                {
                    System.Diagnostics.Debug.WriteLine("[工作模式] 简易模式已启用，不启动PLC连接和气缸控制循环");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[工作模式] 加载配置失败: {ex.Message}，使用默认标准模式");
                _workMode = WorkMode.Standard;
            }
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
        
        /// <summary>
        /// 检测条码类型
        /// </summary>
        /// <param name="scanData">扫描的条码数据</param>
        /// <returns>条码类型</returns>
        private BarcodeType DetectBarcodeType(string scanData)
        {
            if (string.IsNullOrWhiteSpace(scanData))
                return BarcodeType.Unknown;
            
            // H开头 + 后续全是数字 = 物流盒编码
            if (scanData.StartsWith("H", StringComparison.OrdinalIgnoreCase) && 
                scanData.Length > 1 && 
                scanData.Substring(1).All(char.IsDigit))
            {
                return BarcodeType.LogisticsBox;
            }

            // 纯数字 = 送检人
            if (scanData.All(char.IsDigit))
            {
                return BarcodeType.Inspector;
            }
            
            // 其他 = 报工单编号
            return BarcodeType.WorkOrder;
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
            
            // 弯道控制循环管理
            if (isConnected)
            {
                StartLoadingCurveControlLoop();
                StartUnloadingCurveControlLoop();
            }
            else
            {
                StopLoadingCurveControlLoop();
                StopUnloadingCurveControlLoop();
            }
        }

        private void ScannerService_ConnectionStatusChanged(object sender, Services.ScannerConnectionStatusChangedEventArgs e)
        {
            // 确保在UI线程上更新
            if (System.Windows.Application.Current?.Dispatcher != null)
            {
                if (System.Windows.Application.Current.Dispatcher.CheckAccess())
                {
                    UpdateScannerConnectionStatusFromEvent(e);
                }
                else
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => UpdateScannerConnectionStatusFromEvent(e));
                }
            }
            else
            {
                UpdateScannerConnectionStatusFromEvent(e);
            }
        }
        
        private void UpdateScannerConnectionStatusFromEvent(Services.ScannerConnectionStatusChangedEventArgs e)
        {
            // 立即更新对应检测室的连接状态
            if (RoomBoxLists != null && e != null)
            {
                var roomBoxList = RoomBoxLists.FirstOrDefault(rbl => rbl.Room?.Id == e.RoomId);
                if (roomBoxList != null)
                {
                    roomBoxList.IsScannerConnected = e.IsConnected;
                }
            }
        }

        private void UpdatePlcConnectionStatus()
        {
            // 直接更新，BindableBase的SetProperty已经处理了线程安全
            var currentStatus = _s7Service.IsConnected;
            // 强制更新，即使值相同也触发通知（确保UI刷新）
            IsPlcConnected = currentStatus;
            
            // 确保循环在启动时正确运行
            if (currentStatus)
            {
                StartLoadingCurveControlLoop();
                StartUnloadingCurveControlLoop();
            }
        }

        private void UpdateScannerConnectionStatus()
        {
            // DispatcherTimer已经在UI线程上运行，直接更新即可
            if (RoomBoxLists == null)
            {
                return;
            }
            
            foreach (var roomBoxList in RoomBoxLists)
            {
                if (roomBoxList?.Room == null)
                {
                    continue;
                }
                
                bool isConnected = _scannerService.IsConnected(roomBoxList.Room.Id);
                bool oldValue = roomBoxList.IsScannerConnected;
                
                // 只有当值不同时才更新，避免不必要的通知
                if (oldValue != isConnected)
                {
                    roomBoxList.IsScannerConnected = isConnected;
                }
            }
        }
        
        // 公开方法：手动刷新扫码器连接状态（供外部调用）
        public void RefreshScannerConnectionStatus()
        {
            UpdateScannerConnectionStatus();
        }

        private async void ScannerService_DataReceived(object sender, Services.ScannerDataReceivedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[任务管理] 收到串口数据 - 检测室ID:{e.RoomId}, 检测室名称:{e.RoomName}, 扫码数据:{e.ScanData}");
            
            // 在UI线程上处理扫码数据
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                // --- 全局上料扫码枪 (ID: -1) ---
                if (e.RoomId == -1)
                {
                    // 自动识别条码类型
                    var barcodeType = DetectBarcodeType(e.ScanData);
                    
                    switch (barcodeType)
                    {
                        case BarcodeType.LogisticsBox:
                            // 物流盒编码：创建任务
                            System.Diagnostics.Debug.WriteLine($"[扫码识别] 物流盒编码: {e.ScanData}");
                            AddLogisticsBoxCode(e.ScanData);
                            break;
                            
                        case BarcodeType.WorkOrder:
                            // 报工单编号：更新最新任务的报工单编号，并更新UI暂存
                            System.Diagnostics.Debug.WriteLine($"[扫码识别] 报工单编号: {e.ScanData}");
                            WorkOrderNo = e.ScanData; // 【新增】暂存到属性，用于创建新任务
                            UpdateWorkOrderNoForLatestTask(e.ScanData); // 尝试更新已有任务
                            break;

                        case BarcodeType.Inspector:
                            // 送检人信息：录入并更新UI和缓存
                            System.Diagnostics.Debug.WriteLine($"[扫码识别] 送检人: {e.ScanData}");
                            InspectorInputText = e.ScanData; // 这会自动更新 _lastInspectorName
                            
                            // 尝试更新已存在的最新任务（支持先扫盒后扫人）
                            UpdateInspectorForLatestTask(e.ScanData);
                            break;
                            
                        case BarcodeType.Unknown:
                            System.Diagnostics.Debug.WriteLine($"[扫码识别] 未知编码格式: {e.ScanData}");
                            System.Windows.MessageBox.Show(
                                $"未知编码格式：{e.ScanData}\n\n物流盒编码格式：H + 数字（如 H0001）\n送检人：纯数字\n报工单编号：其他格式", 
                                "警告", 
                                System.Windows.MessageBoxButton.OK, 
                                System.Windows.MessageBoxImage.Warning);
                            break;
                    }
                    return;
                }

                // --- 全局下料扫码枪 (ID: -2) ---
                if (e.RoomId == -2)
                {
                    // 下料扫码：标记任务完成
                    var boxNo = e.ScanData.Trim();
                    // [修改] 如果扫描的数据带有前缀，去除它
                    if (boxNo.StartsWith("物流盒编码", StringComparison.OrdinalIgnoreCase))
                    {
                        boxNo = boxNo.Substring(5).Trim();
                    }
                    
                    // 查找对应的任务（现在 LogisticsBoxCode 只存储编号）
                    var task = Tasks?.Where(t => string.Equals(t.LogisticsBoxCode, boxNo, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(t => t.Id)
                        .FirstOrDefault();
                        
                    if (task == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[下料扫码] 未找到对应任务 - 物流盒:{boxNo}");
                        return;
                    }
                    
                    // 只有当任务分配了检测室，才更新状态
                    if (task.AssignedRoomId.HasValue) 
                    {
                        // 检查当前状态，如果已经是检测完成则忽略
                        string currentStatus = GetRoomStatusByRoomId(task, task.AssignedRoomId.Value);
                        if (currentStatus != "检测完成")
                        {
                            // 更新状态为"检测完成"
                            SetRoomStatusByRoomId(task, task.AssignedRoomId.Value, "检测完成");
                            task.EndTime = DateTime.Now;
                            
                            // 更新检测室列表显示（如果该物流盒还在某个检测室的列表中，将其移除）
                            var unloadingRoomBoxList = RoomBoxLists?.FirstOrDefault(rbl => rbl.Room?.Id == task.AssignedRoomId.Value);
                            if (unloadingRoomBoxList != null)
                            {
                                unloadingRoomBoxList.ScanStatus = "下料完成";
                                if (unloadingRoomBoxList.Boxes.Contains(boxNo))
                                {
                                    unloadingRoomBoxList.Boxes.Remove(boxNo);
                                }
                            }
                            
                            // [新增] 更新 MES 数据库：下料扫码，flag=D
                            var workOrderNoLocal = task.WorkOrderNo;
                            if (!string.IsNullOrWhiteSpace(workOrderNoLocal))
                            {
                                _ = Task.Run(async () =>
                                {
                                    var result = await _mesDatabaseService.UpdateUnloadingScanAsync(workOrderNoLocal);
                                    System.Diagnostics.Debug.WriteLine($"[下料扫码] MES更新 flag=D: 送检单={workOrderNoLocal}, 结果={result}");
                                });
                            }
                            
                            // 异步更新日志
                            _ = Task.Run(async () =>
                            {
                                var logs = await _detectionLogService.GetLogsByBoxCodeAsync(boxNo);
                                var latestLog = logs?.OrderByDescending(l => l.CreateTime).FirstOrDefault();
                                if (latestLog != null)
                                {
                                    latestLog.Status = "检测完成";
                                    latestLog.EndTime = DateTime.Now;
                                    await _detectionLogService.UpdateLogAsync(latestLog);
                                }
                            });
                            
                            System.Diagnostics.Debug.WriteLine($"[下料扫码] 物流盒 {boxNo} 检测流程结束");
                        }
                    }
                    return;
                }

                // --- 检测室扫码枪 (ID > 0) ---
                // 找到对应的检测室
                var roomBoxList = RoomBoxLists?.FirstOrDefault(rbl => rbl.Room?.Id == e.RoomId);
                if (roomBoxList == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[任务管理] 未找到对应的检测室 - 检测室ID:{e.RoomId}");
                    return;
                }

                // 更新扫码编码显示
                roomBoxList.LastScannedCode = e.ScanData;
                
                // 提取物流盒编号（去掉"物流盒编码"前缀，如果有的话）
                var boxNoLocal = e.ScanData.Trim();
                // 处理可能带有"物流盒编码"前缀的情况
                if (boxNoLocal.StartsWith("物流盒编码", StringComparison.OrdinalIgnoreCase))
                {
                    boxNoLocal = boxNoLocal.Substring(5).Trim(); // "物流盒编码"是5个字符
                }
                
                System.Diagnostics.Debug.WriteLine($"[检测室扫码] 检测室ID:{e.RoomId}, 扫码原始数据:'{e.ScanData}', 物流盒编号:'{boxNoLocal}'");

                // [修改] 直接查找已分配到此检测室的任务
                // LogisticsBoxCode 现在只存储编号（如 H00657），不存储前缀
                var taskLocal = Tasks?.Where(t => 
                        string.Equals(t.LogisticsBoxCode, boxNoLocal, StringComparison.OrdinalIgnoreCase) &&
                        t.AssignedRoomId == e.RoomId) // 只匹配分配到此检测室的任务
                    .OrderByDescending(t => t.Id)
                    .FirstOrDefault();
                    
                if (taskLocal == null)
                {
                    // 显示未匹配原因
                    var anyTask = Tasks?.FirstOrDefault(t => 
                        string.Equals(t.LogisticsBoxCode, boxNoLocal, StringComparison.OrdinalIgnoreCase));
                    
                    if (anyTask != null)
                    {
                        // 有该物流盒的任务，但不是分配到此检测室的
                        roomBoxList.ScanStatus = $"此物流盒分配到检测室{anyTask.AssignedRoomId}";
                        System.Diagnostics.Debug.WriteLine($"[检测室扫码] 物流盒 {boxNoLocal} 分配到检测室{anyTask.AssignedRoomId}，但扫码在检测室{e.RoomId}");
                    }
                    else
                    {
                        // 没有找到该物流盒的任务
                        roomBoxList.ScanStatus = "未找到匹配物流盒";
                        System.Diagnostics.Debug.WriteLine($"[检测室扫码] 未找到物流盒 {boxNoLocal} 的任务");
                    }
                    
                    // 执行不匹配流程（放行动作）
                    _ = HandleUnmatchedScanAsync(e.RoomId, e.RoomName);
                    return;
                }

                // 检查该任务是否分配到此检测室
                if (taskLocal.AssignedRoomId != e.RoomId)
                {
                    roomBoxList.ScanStatus = "任务不在此检测室";
                    // 执行不匹配流程（放行动作）
                    _ = HandleUnmatchedScanAsync(e.RoomId, e.RoomName);
                    return;
                }

                // 更新扫码次数
                if (!taskLocal.RoomScanCounts.ContainsKey(e.RoomId))
                {
                    taskLocal.RoomScanCounts[e.RoomId] = 0;
                }
                taskLocal.RoomScanCounts[e.RoomId]++;

                int scanCount = taskLocal.RoomScanCounts[e.RoomId];

                // 判断是首次扫码还是二次扫码
                if (scanCount == 1)
                {
                    // 首次扫码：状态改为"检测中"
                    SetRoomStatusByRoomId(taskLocal, e.RoomId, "检测中");
                    taskLocal.StartTime = DateTime.Now;
                    roomBoxList.ScanStatus = "匹配成功-检测中";
                    
                    // 将物流盒添加到检测室的 Boxes 列表（只有通过检测室扫码器扫码的才添加）
                    if (!roomBoxList.Boxes.Contains(boxNoLocal))
                    {
                        roomBoxList.Boxes.Add(boxNoLocal);
                    }
                    
                    // 异步执行匹配流程（不阻塞UI）
                    _ = HandleMatchedScanAsync(e.RoomId, e.RoomName, e.ScanData);
                    
                    // 异步更新日志（不阻塞UI）
                    _ = Task.Run(async () =>
                    {
                        var logs = await _detectionLogService.GetLogsByBoxCodeAsync(boxNoLocal);
                        var latestLog = logs?.OrderByDescending(l => l.CreateTime).FirstOrDefault();
                        if (latestLog != null)
                        {
                            latestLog.Status = "检测中";
                            latestLog.StartTime = DateTime.Now;
                            await _detectionLogService.UpdateLogAsync(latestLog);
                        }
                    });
                }
                else if (scanCount >= 2) // 二次及以上扫码
                {
                    // 二次扫码：只放行，不改变任务状态（状态保持"检测中"），真正完成由下料扫码枪触发
                    // 不需要更新Task状态为"检测完成"
                    // 不需要更新Log状态为"检测完成"
                    
                    roomBoxList.ScanStatus = "二次扫码-放行";
                    
                    // 从检测室的 Boxes 列表中移除（因为它即将离开检测室）
                    if (roomBoxList.Boxes.Contains(boxNoLocal))
                    {
                        roomBoxList.Boxes.Remove(boxNoLocal);
                    }
                    
                    // 执行放行动作（让物流盒离开检测室）
                    _ = HandleUnmatchedScanAsync(e.RoomId, e.RoomName);
                    
                    System.Diagnostics.Debug.WriteLine($"[任务管理] 物流盒 {boxNoLocal} 二次扫码放行 (检测室: {e.RoomName})");
                }
            });
        }

        private async System.Threading.Tasks.Task InitializeGlobalScannersAsync()
        {
            try
            {
                var config = await _configService.GetConfigAsync();
                
                // 上料扫码枪
                if (config.LoadingScannerIsEnabled && !string.IsNullOrWhiteSpace(config.LoadingScannerPort))
                {
                    var scanner = new Models.DetectionRoomItem
                    {
                        Id = -1,
                        RoomName = "上料扫码枪",
                        ScannerPortName = config.LoadingScannerPort,
                        ScannerBaudRate = config.LoadingScannerBaudRate,
                        ScannerDataBits = config.LoadingScannerDataBits,
                        ScannerStopBits = config.LoadingScannerStopBits,
                        ScannerParity = config.LoadingScannerParity,
                        ScannerIsEnabled = true
                    };
                    System.Diagnostics.Debug.WriteLine($"[任务管理] 正在连接上料扫码枪: {scanner.ScannerPortName}");
                    await _scannerService.OpenConnectionAsync(scanner);
                }

                // 下料扫码枪
                if (config.UnloadingScannerIsEnabled && !string.IsNullOrWhiteSpace(config.UnloadingScannerPort))
                {
                    var scanner = new Models.DetectionRoomItem
                    {
                        Id = -2,
                        RoomName = "下料扫码枪",
                        ScannerPortName = config.UnloadingScannerPort,
                        ScannerBaudRate = config.UnloadingScannerBaudRate,
                        ScannerDataBits = config.UnloadingScannerDataBits,
                        ScannerStopBits = config.UnloadingScannerStopBits,
                        ScannerParity = config.UnloadingScannerParity,
                        ScannerIsEnabled = true
                    };
                    System.Diagnostics.Debug.WriteLine($"[任务管理] 正在连接下料扫码枪: {scanner.ScannerPortName}");
                    await _scannerService.OpenConnectionAsync(scanner);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化全局扫码枪失败: {ex.Message}");
            }
        }

        private async void InitializeData()
        {
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
                        Room = room,
                        IsScannerConnected = _scannerService.IsConnected(room.Id) // 初始化串口连接状态
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

        public string WorkOrderNo
        {
            get => _workOrderNo;
            set => SetProperty(ref _workOrderNo, value);
        }

        private DateTime _lastProcessTime;
        private string _lastProcessedCode;
        // [新增] 全局静态去重缓存 (Key=FormattedCode, Value=ProcessTime)
        // 使用静态变量确保跨实例（虽然ViewModel通常单例）与生命周期的持久性
        private static Dictionary<string, DateTime> _processedCodesCache = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        public void AddLogisticsBoxCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return;
            }
            
            // [修复] 统一去除空格/回车换行，确保防抖比较准确
            code = code?.Trim();
            if (string.IsNullOrWhiteSpace(code)) return;

            // 提取物流盒编号（去掉"物流盒编码"前缀）
            // 使用忽略大小写的替换
            var boxNo = System.Text.RegularExpressions.Regex.Replace(code, "物流盒编码", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
            
            // [修改] 现在只存储编号，不存储前缀
            System.Diagnostics.Debug.WriteLine($"[尝试创建任务] 输入: '{code}', 编号: '{boxNo}', 当前任务数: {Tasks?.Count ?? 0}");

            // [强力去重] 使用全局静态缓存防止短时间内的重复处理 (现在用 boxNo 作为 key)
            lock (_lockObject)
            {
                // 清理过期缓存(超过5秒的)
                var now = DateTime.Now;
                var expiredKeys = _processedCodesCache.Where(kv => (now - kv.Value).TotalSeconds > 5).Select(kv => kv.Key).ToList();
                foreach (var key in expiredKeys) _processedCodesCache.Remove(key);

                // 检查当前码是否在缓存中且未过期 (2秒防抖)
                if (_processedCodesCache.TryGetValue(boxNo.ToUpperInvariant(), out DateTime lastTime))
                {
                    if ((now - lastTime).TotalSeconds < 2)
                    {
                        System.Diagnostics.Debug.WriteLine($"[强力防抖] 忽略短期重复扫码 (Global Cache): {boxNo}");
                        return;
                    }
                }
                
                // 更新缓存
                _processedCodesCache[boxNo.ToUpperInvariant()] = now;
            }

            // [业务层去重] 检查任务列表 (现在用 boxNo 比较)
            if (Tasks != null)
            {
                var existingTask = Tasks.FirstOrDefault(t => 
                    string.Equals(t.LogisticsBoxCode, boxNo, StringComparison.OrdinalIgnoreCase) && 
                    t.AssignedRoomId == null); // 待分配状态

                if (existingTask != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[去重] 任务列表中已存在待分配的物流盒 {boxNo} (ID={existingTask.Id})，忽略本次扫码");
                    return;
                }
            }

            // [修改] 扫物流盒逻辑：不再进行本地规则匹配，仅创建初始任务
            // 此时检测室未知 (AssignedRoomId = null)，状态为 "待扫送检单"

            // 检查录入列表中是否已存在
            if (LogisticsBoxList.Contains(boxNo))
            {
                // 暂时允许重复扫，不做严格限制
            }

            // 创建任务项
            var taskId = (int)(DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;
            var newTask = new Models.TaskItem
            {
                Id = taskId,
                LogisticsBoxCode = boxNo, // [修改] 只存储编号，不存储前缀
                WorkOrderNo = "", // 此时还没有送检单
                AssignedRoomId = null, // [关键] 检测室待定
                InspectorName = !string.IsNullOrWhiteSpace(_lastInspectorName) ? _lastInspectorName : (_currentUserService?.CurrentUser?.EmployeeNo ?? ""), 
                StartTime = DateTime.Now,
                RoomScanCounts = new Dictionary<int, int>()
            };

            // 添加到任务列表
            if (Tasks == null)
            {
                Tasks = new ObservableCollection<Models.TaskItem>();
            }
            Tasks.Insert(0, newTask); // 插入到最前面

            // 添加到录入信息列表
            if (!LogisticsBoxList.Contains(boxNo))
            {
                LogisticsBoxList.Add(boxNo);
            }


            // 异步记录日志（初始日志，无检测室，状态为待分配）
            // 异步记录日志（初始日志，无检测室，状态为待分配）
            // [修正] 上面的代码因为是直接new的对象，无法获取ID。
            // 正确的做法如下：
            _ = Task.Run(async () =>
            {
                var logItem = new DetectionLogItem
                {
                    LogisticsBoxCode = boxNo,
                    WorkOrderNo = "",
                    InspectorName = newTask.InspectorName ?? "", // [新增] 保存送检人
                    RoomId = null,
                    RoomName = "待分配",
                    Status = "等待扫送检单",
                    CreateTime = DateTime.Now
                };
                
                var success = await _detectionLogService.AddLogAsync(logItem);
                if (success && logItem.Id > 0)
                {
                    // 回填 ID 到 UI 模型
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        newTask.LogId = logItem.Id;
                        System.Diagnostics.Debug.WriteLine($"[日志] 物流盒 {boxNo} 初始日志已创建, LogId={newTask.LogId}");
                    });
                }
            });

            LogisticsBoxCode = string.Empty;
            WorkOrderNo = string.Empty;
        }

        /// <summary>
        /// 更新最新任务的报工单编号（用于在报工单编号输入框中按回车时更新已存在的任务）
        /// </summary>
        public void UpdateWorkOrderNoForLatestTask(string workOrderNo)
        {
            if (string.IsNullOrWhiteSpace(workOrderNo))
            {
                return;
            }

            // [注意] 移除了之前的强制同步配置刷新，因为会导致UI死锁
            // 且现在简易模式和标准模式逻辑一致（都查询MES），无需特殊处理

            // [修复] 查找最新的待更新任务：
            // 1. 优先查找 AssignedRoomId = null 且无送检单的任务（刚创建的任务）
            // 2. 或者查找已分配但未完成且无送检单的任务
            var latestTask = Tasks?.OrderByDescending(t => t.Id)
                .FirstOrDefault(t =>
                {
                    // 条件1：还没有送检单
                    if (!string.IsNullOrWhiteSpace(t.WorkOrderNo)) return false;
                    
                    // 条件2：要么是待分配状态（AssignedRoomId == null），要么是已分配但未完成
                    if (t.AssignedRoomId == null)
                    {
                        return true; // 待分配状态，可以扫送检单
                    }
                    else
                    {
                        // 已分配检测室，检查是否未完成
                        string status = GetRoomStatusByRoomId(t, t.AssignedRoomId.Value);
                        return status != "检测完成";
                    }
                });

            if (latestTask != null)
            {
                latestTask.WorkOrderNo = workOrderNo;
                System.Diagnostics.Debug.WriteLine($"[送检单扫码] 任务 {latestTask.LogisticsBoxCode} 更新送检单: {workOrderNo}");

                // [修复] 无论MES查询是否成功，都必须先保存报工单信息到本地数据库
                // 启动异步任务更新数据库
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // [竞态条件修复]：如果 LogId 还没回来（说明数据库插入还在进行中），稍微等一下
                        int retryCount = 0;
                        while (latestTask.LogId == 0 && retryCount < 10)
                        {
                            await Task.Delay(100); // 等待 100ms
                            retryCount++;
                        }

                        if (latestTask.LogId > 0)
                        {
                             // 1. 如果有 LogId，直接精准更新
                             await _detectionLogService.UpdateWorkOrderNoAsync(latestTask.LogId, latestTask.WorkOrderNo);
                             System.Diagnostics.Debug.WriteLine($"[数据库] 已保存送检单 {latestTask.WorkOrderNo} (By LogId={latestTask.LogId})");
                        }
                        else
                        {
                            // 2. 降级方案：如果没有 LogId（超时或其他原因），回退到按物流盒编码查询
                            System.Diagnostics.Debug.WriteLine($"[数据库] LogId 获取超时，降级为按物流盒编码更新");
                            var boxNo = latestTask.LogisticsBoxCode; // 现在只存储编号
                            if (!string.IsNullOrWhiteSpace(boxNo))
                            {
                                var logs = await _detectionLogService.GetLogsByBoxCodeAsync(boxNo);
                                var latestLog = logs?.OrderByDescending(l => l.CreateTime).FirstOrDefault();
                                if (latestLog != null)
                                {
                                    await _detectionLogService.UpdateWorkOrderNoAsync(latestLog.Id, latestTask.WorkOrderNo);
                                    System.Diagnostics.Debug.WriteLine($"[数据库] 已保存送检单 {latestTask.WorkOrderNo}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[数据库] 保存送检单失败: {ex.Message}");
                    }
                });

                // [新增] 标准模式：异步查询 MES 数据库并分配检测室
                _ = Task.Run(async () =>
                {
                    try 
                    {
                        // 1. 查询检测室编号
                        string roomNo = await _mesDatabaseService.GetRoomNumberByWorkOrderAsync(workOrderNo);
                        
                        // 回到 UI 线程更新
                        await App.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            if (!string.IsNullOrWhiteSpace(roomNo))
                            {
                                System.Diagnostics.Debug.WriteLine($"[MES查询成功] 送检单 {workOrderNo} -> 检测室 {roomNo}");
                                
                                // 2. 匹配本地检测室
                                var matchedRoom = DetectionRooms?.FirstOrDefault(r => r.RoomName.Contains(roomNo) || r.RoomNo == roomNo);
                                
                                if (matchedRoom != null)
                                {
                                    // 3. 更新任务
                                    latestTask.AssignedRoomId = matchedRoom.Id;
                                    SetRoomStatusByRoomId(latestTask, matchedRoom.Id, "未检测");
                                    
                                    // 4. 更新数据库日志（仅更新检测室和状态，WorkOrderNo前面已经更新过了）
                                    // 为了保险起见，再次确保WorkOrderNo一致性也无妨，或者只更新RoomInfo
                                    await Task.Run(async () => 
                                    {
                                        var boxNo = latestTask.LogisticsBoxCode; // 现在只存储编号，不需要去除前缀
                                        if (!string.IsNullOrWhiteSpace(boxNo))
                                        {
                                            var logs = await _detectionLogService.GetLogsByBoxCodeAsync(boxNo);
                                            var latestLog = logs?.OrderByDescending(l => l.CreateTime).FirstOrDefault();
                                            if (latestLog != null)
                                            {
                                                await _detectionLogService.UpdateRoomInfoAsync(latestLog.Id, matchedRoom.Id, matchedRoom.RoomName, "未检测");
                                            }
                                        }
                                    });
                                    
                                    // [新增] 更新 MES 数据库：上料扫码，写入 hz_code 和 flag=B
                                    var boxCodeForMes = latestTask.LogisticsBoxCode;
                                    if (!string.IsNullOrWhiteSpace(boxCodeForMes))
                                    {
                                        _ = Task.Run(async () =>
                                        {
                                            var result = await _mesDatabaseService.UpdateLoadingScanAsync(workOrderNo, boxCodeForMes);
                                            System.Diagnostics.Debug.WriteLine($"[上料扫码] MES更新 hz_code={boxCodeForMes}, flag=B: 送检单={workOrderNo}, 结果={result}");
                                        });
                                    }
                                }
                                else
                                {
                                    CustomMessageBox.ShowWarning($"MES返回检测室 '{roomNo}'，但本地未找到匹配配置！");
                                }
                            }
                            else
                            {
                                CustomMessageBox.ShowWarning($"MES未找到送检单 {workOrderNo} 对应的检测室信息！");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                         System.Diagnostics.Debug.WriteLine($"[分配异常] {ex.Message}");
                    }
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[送检单扫码失败] 未找到可更新的任务");
            }
        }

    /// <summary>
    /// 更新最新任务的送检人信息（用于在送检人输入框中按回车或扫码时更新）
    /// </summary>
    public void UpdateInspectorForLatestTask(string inspectorName)
    {
        if (string.IsNullOrWhiteSpace(inspectorName))
        {
            return;
        }

        // 查找最新的未完成任务（按ID降序，取第一个）
        var latestTask = Tasks?.OrderByDescending(t => t.Id)
            .FirstOrDefault(t => 
            {
                // [修改] 检查任务是否可以更新送检人：
                // 1. 未分配检测室的任务（刚创建，等待送检单扫码）
                if (t.AssignedRoomId == null)
                {
                    return true; // 可以更新送检人
                }
                
                // 2. 已分配检测室但未完成的任务
                string status = GetRoomStatusByRoomId(t, t.AssignedRoomId.Value);
                return status != "检测完成";
            });

        
        if (latestTask != null)
        {
            System.Diagnostics.Debug.WriteLine($"[送检人更新] 更新任务 ID={latestTask.Id}, 物流盒={latestTask.LogisticsBoxCode}, 送检人: {inspectorName}");
            latestTask.InspectorName = inspectorName;
            
            // [修复] 简易模式和标准模式都立即更新数据库，采用 LogId 精准更新策略
            _ = Task.Run(async () =>
            {
                try
                {
                    // [竞态条件修复]：如果 LogId 还没回来，稍微等待
                    int retryCount = 0;
                    while (latestTask.LogId == 0 && retryCount < 10)
                    {
                        await Task.Delay(100);
                        retryCount++;
                    }

                    if (latestTask.LogId > 0)
                    {
                        // 1. LogId 存在，精准更新
                        await _detectionLogService.UpdateInspectorNameAsync(latestTask.LogId, latestTask.InspectorName);
                        System.Diagnostics.Debug.WriteLine($"[送检人更新] 已保存送检人 {inspectorName} 到数据库 (By LogId={latestTask.LogId})");
                    }
                    else 
                    {
                        // 2. 降级方案
                        System.Diagnostics.Debug.WriteLine($"[送检人更新] LogId 获取超时，降级为按物流盒编码更新");
                        var boxNo = latestTask.LogisticsBoxCode; // 现在只存储编号
                        if (!string.IsNullOrWhiteSpace(boxNo))
                        {
                            var logs = await _detectionLogService.GetLogsByBoxCodeAsync(boxNo);
                            var latestLog = logs?.OrderByDescending(l => l.CreateTime).FirstOrDefault();
                            if (latestLog != null)
                            {
                                await _detectionLogService.UpdateInspectorNameAsync(latestLog.Id, latestTask.InspectorName);
                                System.Diagnostics.Debug.WriteLine($"[送检人更新] 已保存送检人 {inspectorName} 到数据库");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[送检人更新] 保存失败: {ex.Message}");
                }
            });
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[送检人更新] 警告: 未找到可更新的任务! 当前任务数: {Tasks?.Count ?? 0}");
            if (Tasks != null && Tasks.Any())
            {
                var latest = Tasks.OrderByDescending(t => t.Id).FirstOrDefault();
                System.Diagnostics.Debug.WriteLine($"[送检人更新] 最新任务: ID={latest?.Id}, AssignedRoomId={latest?.AssignedRoomId}, 物流盒={latest?.LogisticsBoxCode}");
            }
        }
    }

    /// <summary>
    /// 执行统一扫码输入确认逻辑
    /// </summary>
    private void ExecuteUnifiedInputConfirm()
    {
        if (string.IsNullOrWhiteSpace(UnifiedInputText))
            return;

        string input = UnifiedInputText.Trim();
        
        try
        {
            // 1. 识别物流盒编码：以 H 开头 + 数字
            if (input.StartsWith("H", StringComparison.OrdinalIgnoreCase) && input.Length > 1)
            {
                System.Diagnostics.Debug.WriteLine($"[统一扫码]以此为物流盒编码处理: {input}");
                AddLogisticsBoxCode(input);
            }
            // 2. 识别报工单编码：以 ZJ 开头
            else if (input.StartsWith("ZJ", StringComparison.OrdinalIgnoreCase) && input.Length > 2)
            {
                System.Diagnostics.Debug.WriteLine($"[统一扫码]以此为报工单编码处理: {input}");
                UpdateWorkOrderNoForLatestTask(input);
            }
            // 3. 识别送检人（员工号）：纯数字
            else if (long.TryParse(input, out _)) 
            {
                System.Diagnostics.Debug.WriteLine($"[统一扫码]以此为员工号处理: {input}");
                UpdateInspectorForLatestTask(input);
                
                // [新增] 简易模式：扫描送检人后直接完成任务
                if (_workMode == WorkMode.Simple)
                {
                    CompleteSimpleModeTask();
                }
            }
            else
            {
                // 未知格式，或者可能是其他类型的条码
                System.Diagnostics.Debug.WriteLine($"[统一扫码] 未知格式: {input}");
                System.Windows.MessageBox.Show($"无法识别的条码格式：{input}\n\n支持格式：\n1. H开头 (物流盒)\n2. ZJ开头 (报工单)\n3. 纯数字 (员工号)", "格式错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[统一扫码] 处理异常: {ex.Message}");
            // ShowError($"处理扫码异常: {ex.Message}");
        }
        finally
        {
            // 清空输入框，方便下一次扫码
            UnifiedInputText = string.Empty;
        }
    }

    /// <summary>
    /// 简易模式：完成任务（扫描送检人后直接标记为检测完成）
    /// </summary>
    private async void CompleteSimpleModeTask()
    {
        try
        {
            var latestTask = Tasks?.OrderByDescending(t => t.Id).FirstOrDefault();
            if (latestTask == null)
            {
                System.Diagnostics.Debug.WriteLine("[简易模式] 没有找到最新任务");
                return;
            }
            
            // 检查必要信息是否完整
            if (string.IsNullOrWhiteSpace(latestTask.WorkOrderNo))
            {
                System.Diagnostics.Debug.WriteLine("[简易模式] 送检单号为空，跳过自动完成");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(latestTask.InspectorName))
            {
                System.Diagnostics.Debug.WriteLine("[简易模式] 送检人为空，跳过自动完成");
                return;
            }
            
            // 标记为检测完成
            latestTask.EndTime = DateTime.Now;
            
            System.Diagnostics.Debug.WriteLine($"[简易模式] 任务自动完成：{latestTask.LogisticsBoxCode}, 报工单：{latestTask.WorkOrderNo}, 送检人：{latestTask.InspectorName}");
            
            // 更新数据库状态（包括WorkOrderNo和InspectorName）
            await Task.Run(async () =>
            {
                try
                {
                    var boxNo = latestTask.LogisticsBoxCode; // 现在只存储编号
                    if (!string.IsNullOrWhiteSpace(boxNo))
                    {
                        var logs = await _detectionLogService.GetLogsByBoxCodeAsync(boxNo);
                        var targetLog = logs?.OrderByDescending(l => l.CreateTime).FirstOrDefault();
                        
                        if (targetLog != null)
                        {
                            // [修复] 确保WorkOrderNo和InspectorName保存到数据库
                            targetLog.WorkOrderNo = latestTask.WorkOrderNo;
                            targetLog.InspectorName = latestTask.InspectorName;
                            targetLog.Status = "检测完成";
                            targetLog.EndTime = DateTime.Now;
                            await _detectionLogService.UpdateLogAsync(targetLog);
                            System.Diagnostics.Debug.WriteLine($"[简易模式] 数据库已更新：报工单={targetLog.WorkOrderNo}, 送检人={targetLog.InspectorName}, 状态=检测完成");
                        }
                    }
                    
                    // [新增] 简易模式：更新 MES 数据库，直接写入 hz_code 和 flag='D'
                    if (!string.IsNullOrWhiteSpace(latestTask.WorkOrderNo) && !string.IsNullOrWhiteSpace(boxNo))
                    {
                        // 简易模式下，一次性写入物流盒编码和完成状态D
                        var result = await _mesDatabaseService.UpdateSimpleModeScanAsync(latestTask.WorkOrderNo, boxNo);
                        System.Diagnostics.Debug.WriteLine($"[简易模式] MES更新 hz_code={boxNo}, flag=D: 送检单={latestTask.WorkOrderNo}, 结果={result}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[简易模式] 更新数据库失败: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[简易模式] 完成任务异常: {ex.Message}");
        }
    }

        // 根据检测室ID获取状态
        private string GetRoomStatusByRoomId(Models.TaskItem task, int roomId)
        {
            if (roomId == 1) return task.Room1Status;
            if (roomId == 2) return task.Room2Status;
            if (roomId == 3) return task.Room3Status;
            if (roomId == 4) return task.Room4Status;
            if (roomId == 5) return task.Room5Status;
            return null;
        }

        // 根据检测室ID设置状态
        private void SetRoomStatusByRoomId(Models.TaskItem task, int roomId, string status)
        {
            if (roomId == 1) task.Room1Status = status;
            else if (roomId == 2) task.Room2Status = status;
            else if (roomId == 3) task.Room3Status = status;
            else if (roomId == 4) task.Room4Status = status;
            else if (roomId == 5) task.Room5Status = status;
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

                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 匹配流程：推箱任务开始 - 检测室: {roomName}");

                // 1. [新增] 推箱气缸预备 (收缩状态 = 准备推箱)
                // 用户指出：推箱气缸处于收缩状态就是要准备推箱动作了；伸出状态是放行。
                // 所以在放行盒子前，必须先确推箱气缸处于"收缩"状态 (Armed State)。
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 推箱气缸预备(收缩) - 检测室: {roomName}");
                bool pusherPrepared = await ControlCylinderAsync(
                    pushCylinderExtendAddress, 
                    pushCylinderRetractAddress, 
                    false,  // 收缩 (预备)
                    pushCylinderRetractFeedbackAddress, // 收缩反馈
                    true,   // 目标真
                    room.PushCylinderRetractTimeout
                );

                if (!pusherPrepared)
                {
                     System.Diagnostics.Debug.WriteLine($"[控制逻辑] 推箱气缸预备(收缩)失败 - 检测室: {roomName}");
                     _alarmRecordService.RecordAlarmAsync(
                        alarmTitle: "推箱气缸预备失败",
                        alarmMessage: $"检测室 {roomName} 推箱气缸无法收缩到预备位",
                        roomId: roomId,
                        roomName: roomName,
                        deviceName: "推箱气缸",
                        remark: $"物流盒: {scanData}"
                    );
                    return;
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
                        // 记录报警
                        _alarmRecordService.RecordAlarmAsync(
                            alarmTitle: "推箱气缸收缩失败",
                            alarmMessage: $"检测室 {roomName} 推箱气缸收缩失败，禁止放行（安全卡控）",
                            roomId: roomId,
                            roomName: roomName,
                            deviceName: "推箱气缸",
                            remark: "安全卡控验证失败，系统已阻止放行"
                        );
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

                // 2. 阻挡气缸收缩放行 (根据用户最新反馈恢复)
                // 用户明确指出：推箱气缸在阻挡气缸后面，必须先放行，盒子流过去后，再根据匹配结果推箱。
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 阻挡气缸开始收缩放行 - 检测室: {roomName}");
                bool blockingRetracted = false;

                if (room.EnableBlockingCylinderRetractFeedback)
                {
                    // 正常模式：等待反馈
                    blockingRetracted = await ControlCylinderAsync(
                        blockingCylinderExtendAddress, 
                        blockingCylinderRetractAddress, 
                        false,  // 收缩
                        blockingCylinderRetractFeedbackAddress,  // 反馈地址
                        true,   // 目标反馈值（收缩到位应为true）
                        room.BlockingCylinderRetractTimeout    // 使用配置的超时时间
                    );
                    
                    if (!blockingRetracted)
                    {
                        System.Diagnostics.Debug.WriteLine($"[控制逻辑] 阻挡气缸收缩超时（容错继续执行） - 检测室: {roomName}");
                        // 记录报警（仅记录，不终止流程）
                        _alarmRecordService.RecordAlarmAsync(
                            alarmTitle: "阻挡气缸收缩超时",
                            alarmMessage: $"检测室 {roomName} 阻挡气缸收缩操作超时，未收到反馈信号（匹配流程），流程继续执行",
                            roomId: roomId,
                            roomName: roomName,
                            deviceName: "阻挡气缸",
                            remark: $"超时时间: {room.BlockingCylinderRetractTimeout}ms, 物流盒: {scanData}, 容错模式：继续执行"
                        );
                        // [修改] 不再尝试恢复阻挡气缸，也不终止流程，允许继续执行后续的传感器检测和推箱动作
                        System.Diagnostics.Debug.WriteLine($"[控制逻辑] 容错模式：忽略阻挡气缸收缩超时，继续执行传感器检测 - 检测室: {roomName}");
                    }
                }
                else
                {
                    // 容错模式
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 容错模式：阻挡气缸收缩不等待反馈 - 检测室: {roomName}");
                    await _s7Service.WriteBoolAsync(blockingCylinderRetractAddress, true); // 持续输出模式现在由ControlCylinder处理，但在容错模式手动发
                    // 注意：ControlCylinderAsync 已经支持无反馈调用，建议统一使用 ControlCylinderAsync 即使无反馈?
                    // 保持原有逻辑结构以防变动太大，但需确保 Clear Opposite logic (Check ControlCylinderAsync impl).
                    // 手动发送必须小心互锁。还是调用 ControlCylinderAsync 安全。
                    
                    await ControlCylinderAsync(blockingCylinderExtendAddress, blockingCylinderRetractAddress, false, null, false, 500);
                    blockingRetracted = true;
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
                        // 记录报警
                        _alarmRecordService.RecordAlarmAsync(
                            alarmTitle: "推箱气缸伸出超时",
                            alarmMessage: $"检测室 {roomName} 推箱气缸伸出操作超时，未收到反馈信号",
                            roomId: roomId,
                            roomName: roomName,
                            deviceName: "推箱气缸",
                            remark: $"超时时间: {room.PushCylinderExtendTimeout}ms, 物流盒编码: {scanData}"
                        );
                        // 推箱超时也要恢复阻挡气缸
                        await ControlCylinderAsync(blockingCylinderExtendAddress, blockingCylinderRetractAddress, true, blockingCylinderExtendFeedbackAddress, true, room.BlockingCylinderExtendTimeout);
                        return;
                    }
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 推箱气缸已伸出到位，推箱完成 - 检测室: {roomName}");
                    
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 推箱气缸已伸出到位，推箱完成 - 检测室: {roomName}");
                    
                    // 4.5. [修改] 推箱动作完成后不需要立即收缩
                    // 用户指出：推箱伸出状态 = 放行。
                    // 因此推完箱后保持伸出状态是安全的（即处于放行状态），等待下一个任务指令。
                    // 下一个如果是匹配任务，会在开始时执行"预备收缩"。
                    // 下一个如果是不匹配，则保持伸出即可。
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 推箱完成，保持伸出状态(放行位) - 检测室: {roomName}");
                    
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
                        // 记录报警
                        _alarmRecordService.RecordAlarmAsync(
                            alarmTitle: "阻挡气缸伸出超时",
                            alarmMessage: $"检测室 {roomName} 阻挡气缸伸出操作超时，未收到反馈信号（匹配流程）",
                            roomId: roomId,
                            roomName: roomName,
                            deviceName: "阻挡气缸",
                            remark: $"超时时间: {room.BlockingCylinderExtendTimeout}ms, 物流盒编码: {scanData}"
                        );
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
                    // 记录报警
                    _alarmRecordService.RecordAlarmAsync(
                        alarmTitle: "传感器检测超时",
                        alarmMessage: $"检测室 {roomName} 传感器检测超时，未检测到物流盒",
                        roomId: roomId,
                        roomName: roomName,
                        deviceName: "传感器",
                        remark: $"超时时间: {room.SensorDetectTimeout}ms, 物流盒编码: {scanData}"
                    );
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
                // 记录报警
                _alarmRecordService.RecordAlarmAsync(
                    alarmTitle: "匹配流程处理异常",
                    alarmMessage: $"检测室 {roomName} 匹配流程处理失败：{ex.Message}",
                    roomId: roomId,
                    roomName: roomName,
                    remark: $"异常类型: {ex.GetType().Name}, 堆栈: {ex.StackTrace}"
                );
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
                        // 记录报警
                        _alarmRecordService.RecordAlarmAsync(
                            alarmTitle: "推箱气缸伸出超时",
                            alarmMessage: $"检测室 {roomName} 推箱气缸伸出操作超时，未收到反馈信号（不匹配流程）",
                            roomId: roomId,
                            roomName: roomName,
                            deviceName: "推箱气缸",
                            remark: $"超时时间: {room.PushCylinderExtendTimeout}ms"
                        );
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
                        // 记录报警
                        _alarmRecordService.RecordAlarmAsync(
                            alarmTitle: "推箱气缸伸出失败",
                            alarmMessage: $"检测室 {roomName} 推箱气缸伸出失败，禁止放行（安全卡控-不匹配流程）",
                            roomId: roomId,
                            roomName: roomName,
                            deviceName: "推箱气缸",
                            remark: "安全卡控验证失败，系统已阻止放行"
                        );
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
                System.Diagnostics.Debug.WriteLine($"[控制逻辑] 不匹配流程：阻挡气缸开始收缩放行 - 检测室: {roomName}");
                bool blockingRetracted = false;

                if (room.EnableBlockingCylinderRetractFeedback)
                {
                    // 正常模式：等待反馈
                    blockingRetracted = await ControlCylinderAsync(
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
                        // 记录报警
                        _alarmRecordService.RecordAlarmAsync(
                            alarmTitle: "阻挡气缸收缩超时",
                            alarmMessage: $"检测室 {roomName} 阻挡气缸收缩操作超时，未收到反馈信号（不匹配流程）",
                            roomId: roomId,
                            roomName: roomName,
                            deviceName: "阻挡气缸",
                            remark: $"超时时间: {room.BlockingCylinderRetractTimeout}ms"
                        );
                        // 尝试恢复阻挡气缸（等待反馈，确保恢复到安全状态）
                        await ControlCylinderAsync(blockingCylinderExtendAddress, blockingCylinderRetractAddress, true, blockingCylinderExtendFeedbackAddress, true, room.BlockingCylinderExtendTimeout);
                        return;
                    }
                }
                else
                {
                    // 容错模式：不等待反馈，发送信号后等待固定时间
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 容错模式：阻挡气缸收缩不等待反馈 - 检测室: {roomName}");
                    
                    // 发送收缩信号
                    await _s7Service.WriteBoolAsync(blockingCylinderRetractAddress, true);
                    await _s7Service.WriteBoolAsync(blockingCylinderExtendAddress, false);
                    
                    // 等待固定时间（假设气缸已经动作完成），例如500ms
                    await Task.Delay(500);
                    
                    // 清零信号
                    await _s7Service.WriteBoolAsync(blockingCylinderRetractAddress, false);
                    await _s7Service.WriteBoolAsync(blockingCylinderExtendAddress, false);
                    
                    blockingRetracted = true; // 假设已收缩到位
                    System.Diagnostics.Debug.WriteLine($"[控制逻辑] 容错模式：阻挡气缸收缩信号已发送，假设已收缩到位 - 检测室: {roomName}");
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
                    // 记录报警
                    _alarmRecordService.RecordAlarmAsync(
                        alarmTitle: "阻挡气缸伸出超时",
                        alarmMessage: $"检测室 {roomName} 阻挡气缸伸出操作超时，未收到反馈信号（不匹配流程恢复）",
                        roomId: roomId,
                        roomName: roomName,
                        deviceName: "阻挡气缸",
                        remark: $"超时时间: {room.BlockingCylinderExtendTimeout}ms"
                    );
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

        private async void ExecuteInitializeState()
        {
            if (System.Windows.MessageBox.Show("确定要初始化所有检测室状态吗？\n这将强制阻挡气缸伸出、推箱气缸收缩。\n请确保现场人员安全！", 
                "初始化确认", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var result = await InitializeAllRoomsAsync();
                
                if (result.failCount == 0)
                {
                    // 初始化全局弯道气缸状态 to 收缩
                    await InitGlobalCurveCylindersAsync();
                    
                    System.Windows.MessageBox.Show($"初始化成功！\n成功初始化 {result.successCount} 个检测室。\n上下料弯道气缸已复位。", "操作成功", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    // 即使部分失败，也尝试初始化全局气缸
                    await InitGlobalCurveCylindersAsync();
                    
                    string failDetails = string.Join(", ", result.failedRooms);
                    System.Windows.MessageBox.Show($"初始化完成，但有错误发生。\n成功: {result.successCount}\n失败: {result.failCount}\n失败列表: {failDetails}", "部分失败", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"初始化执行失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// 初始化全局弯道气缸状态（强制收缩）
        /// </summary>
        private async Task InitGlobalCurveCylindersAsync()
        {
            try
            {
                if (_s7Service.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine("[初始化] 开始初始化全局弯道气缸...");
                    
                    // 上料弯道收缩：Q8.0=False, Q8.1=True
                    // 先断开伸出信号
                    await _s7Service.WriteBoolAsync("Q8.0", false);
                    await Task.Delay(50);
                    // 再接通收缩信号
                    await _s7Service.WriteBoolAsync("Q8.1", true);
                    
                    // 下料弯道收缩：Q8.2=False, Q8.3=True
                    // 先断开伸出信号
                    await _s7Service.WriteBoolAsync("Q8.2", false);
                    await Task.Delay(50);
                    // 再接通收缩信号
                    await _s7Service.WriteBoolAsync("Q8.3", true);
                    
                    System.Diagnostics.Debug.WriteLine("[初始化] 全局弯道气缸初始化完成（已置为收缩状态）");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[初始化] 初始化全局弯道气缸失败: {ex.Message}");
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
                    // 记录报警
                    _alarmRecordService.RecordAlarmAsync(
                        alarmTitle: "检测室配置不完整",
                        alarmMessage: $"检测室 {roomName} 未配置PLC控制地址，无法执行初始化",
                        roomId: roomId,
                        roomName: roomName,
                        remark: "缺少PLC控制地址配置"
                    );
                    return false;
                }
                
                // 检查反馈地址是否配置（初始化必须要有反馈地址才能确认成功）
                if (string.IsNullOrWhiteSpace(room.Cylinder1ExtendFeedbackAddress) || 
                    string.IsNullOrWhiteSpace(room.Cylinder2RetractFeedbackAddress))
                {
                    System.Diagnostics.Debug.WriteLine($"[初始化] 检测室 {roomName} 未配置反馈地址，无法确认初始化是否成功");
                    // 记录报警
                    _alarmRecordService.RecordAlarmAsync(
                        alarmTitle: "检测室配置不完整",
                        alarmMessage: $"检测室 {roomName} 未配置反馈地址，无法确认初始化是否成功",
                        roomId: roomId,
                        roomName: roomName,
                        remark: $"缺少反馈地址配置，无法执行初始化"
                    );
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
                    // 记录报警
                    _alarmRecordService.RecordAlarmAsync(
                        alarmTitle: "检测室初始化失败-阻挡气缸",
                        alarmMessage: $"检测室 {roomName} 初始化失败：阻挡气缸伸出超时或未到位",
                        roomId: roomId,
                        roomName: roomName,
                        deviceName: "阻挡气缸",
                        remark: $"超时时间: {room.BlockingCylinderExtendTimeout}ms"
                    );
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
                    // 记录报警
                    _alarmRecordService.RecordAlarmAsync(
                        alarmTitle: "检测室初始化失败-推箱气缸",
                        alarmMessage: $"检测室 {roomName} 初始化失败：推箱气缸收缩超时或未到位",
                        roomId: roomId,
                        roomName: roomName,
                        deviceName: "推箱气缸",
                        remark: $"超时时间: {room.PushCylinderRetractTimeout}ms"
                    );
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
                // [修改] 允许单电控（单线圈）配置，即收缩地址可以为空
                // 只要目标地址存在即可执行
                string targetAddress = extend ? extendAddress : retractAddress;
                string oppositeAddress = extend ? retractAddress : extendAddress;
                
                if (string.IsNullOrWhiteSpace(targetAddress))
                    return false;

                // [安全保护] 1. 强制互锁：先关断对向信号 (Break-Before-Make)
                // 无论当前对向信号读取状态如何，都强制执行一次清零，确保双电控气缸不会同时受电
                try 
                {
                    if (!string.IsNullOrWhiteSpace(oppositeAddress))
                    {
                        // 直接写入False
                        await _s7Service.WriteBoolAsync(oppositeAddress, false);
                        
                        // [关键] 给予PLC和电磁阀反应时间 (50ms)
                        // 这能有效防止"信号重叠"导致的阀芯卡死或不动
                        await Task.Delay(50); 
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[安全保护] 互锁清零失败: {ex.Message} - 地址: {oppositeAddress}");
                    return false;
                }

                // [修改] 持续输出模式：移除初始化置0，直接根据需求置1
                // await _s7Service.WriteBoolAsync(targetAddress, false);
                // await Task.Delay(10);

                // 伸出/收缩操作
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
                            await _s7Service.WriteBoolAsync(retractAddress, false); // 持续压制对向信号
                            await Task.Delay(100); // 每100ms刷新一次
                        }
                        
                        // [修改] 持续输出模式：反馈到位后保持为1，不清理
                        // await _s7Service.WriteBoolAsync(extendAddress, false);
                        
                        if (!feedbackReceived)
                        {
                            System.Diagnostics.Debug.WriteLine($"[辅助方法] 保持信号超时 - 控制地址: {extendAddress}, 反馈地址: {feedbackAddress}");
                        }
                        
                        return feedbackReceived;
                    }
                    else
                    {
                        // [修改] 持续输出模式：无反馈也保持为1
                        // await _s7Service.WriteBoolAsync(extendAddress, false);
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
                            
                            // 保持信号为1
                            await _s7Service.WriteBoolAsync(retractAddress, true);
                            await _s7Service.WriteBoolAsync(extendAddress, false); // 持续压制对向信号
                            await Task.Delay(100);
                        }
                        
                        // [修改] 持续输出模式：反馈到位后保持为1
                        // await _s7Service.WriteBoolAsync(extendAddress, false); // 原代码这里写错成 extendAddress了，应为Retract，但反正要删除
                        // await _s7Service.WriteBoolAsync(retractAddress, false);
                        
                        if (!feedbackReceived)
                        {
                            System.Diagnostics.Debug.WriteLine($"[辅助方法] 保持信号超时 - 控制地址: {retractAddress}, 反馈地址: {feedbackAddress}");
                        }
                        
                        return feedbackReceived;
                    }
                    else
                    {
                        // [修改] 持续输出模式：无反馈也保持为1
                        // await _s7Service.WriteBoolAsync(extendAddress, false);
                        // await _s7Service.WriteBoolAsync(retractAddress, false);
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


        /// <summary>
        /// 启动上料弯道控制循环
        /// 逻辑：当防碰撞传感器(I14.5)为false时，如果上料弯道传感器(I0.5)为true，则执行推箱(Q8.0)动作
        /// </summary>
        private void StartLoadingCurveControlLoop()
        {
            // 防止重复启动
            StopLoadingCurveControlLoop();
            
            _loadingCurveCts = new System.Threading.CancellationTokenSource();
            var token = _loadingCurveCts.Token;

            Task.Run(async () => 
            {
                System.Diagnostics.Debug.WriteLine("[上料弯道] 控制循环已启动");
                bool controlExecuted = false; // 用于标记是否刚刚执行过控制，防止同一个信号重复触发

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (_s7Service.IsConnected)
                        {
                            // 1. 优先检查防碰撞传感器 I14.5 (True = 有阻挡/危险，不执行)
                            bool isInterceptorActive = await _s7Service.ReadBoolAsync("I14.5");
                            
                            if (isInterceptorActive)
                            {
                                // 如果防碰撞激活，重置执行标记，允许下次安全时再次触发
                                // System.Diagnostics.Debug.WriteLine("[上料弯道] 防碰撞传感器激活(I14.5)，禁止动作");
                            }
                            else
                            {
                                // 2. 检查上料弯道传感器 I0.5
                                bool isLoadingSensorActive = await _s7Service.ReadBoolAsync("I0.5");
                                
                                if (isLoadingSensorActive)
                                {
                                    if (!controlExecuted)
                                    {
                                        System.Diagnostics.Debug.WriteLine("[上料弯道] 检测到物体(I0.5)，且防碰撞安全(I14.5=False)，执行推箱");
                                        
                                        // [修复] 使用脉冲触发模式，避免双线圈同时通电
                                        try
                                        {
                                            // 3. 执行动作：脉冲触发模式
                                            // 伸出：发送短脉冲到Q8.0
                                            System.Diagnostics.Debug.WriteLine("[上料弯道] 发送伸出脉冲(Q8.0)");
                                            await _s7Service.WriteBoolAsync("Q8.0", true);
                                            await Task.Delay(1000); // [修复] 不使用token，确保脉冲完整执行
                                            await _s7Service.WriteBoolAsync("Q8.0", false); // 立即清零
                                            System.Diagnostics.Debug.WriteLine("[上料弯道] Q8.0 已清零");
                                            
                                            // 等待气缸伸出完成（3000ms）
                                            await Task.Delay(3000); 
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[上料弯道] 推箱伸出异常: {ex.Message}");
                                            // [关键] 异常时也要确保清零
                                            try { await _s7Service.WriteBoolAsync("Q8.0", false); } catch { }
                                        }
                                        finally
                                        {
                                            // [关键] 无论伸出是否成功，都必须执行收缩以恢复原位
                                            try
                                            {
                                                // 额外保险：再次确保Q8.0清零
                                                await _s7Service.WriteBoolAsync("Q8.0", false);
                                                
                                                // ===== 第一次收缩脉冲 =====
                                                System.Diagnostics.Debug.WriteLine("[上料弯道] 发送第1次收缩脉冲(Q8.1)");
                                                await _s7Service.WriteBoolAsync("Q8.1", true);
                                                await Task.Delay(1000); // 脉冲宽度 1000ms
                                                await _s7Service.WriteBoolAsync("Q8.1", false); // 清零
                                                System.Diagnostics.Debug.WriteLine("[上料弯道] Q8.1 第1次已清零");
                                                
                                                // 等待第一次收缩
                                                await Task.Delay(2000);
                                                
                                                // ===== 第二次强制收缩脉冲（保险）=====
                                                System.Diagnostics.Debug.WriteLine("[上料弯道] 发送第2次强制收缩脉冲(Q8.1)");
                                                await _s7Service.WriteBoolAsync("Q8.1", true);
                                                await Task.Delay(1000); // 脉冲宽度 1000ms
                                                await _s7Service.WriteBoolAsync("Q8.1", false); // 清零
                                                System.Diagnostics.Debug.WriteLine("[上料弯道] Q8.1 第2次已清零");
                                                
                                                // 等待气缸收缩完成
                                                await Task.Delay(2000);
                                                
                                                System.Diagnostics.Debug.WriteLine("[上料弯道] 推箱动作完成(双重收缩)");
                                            }
                                            catch (Exception retractEx)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"[上料弯道] 收缩动作异常: {retractEx.Message}");
                                                // 异常时也要确保清零
                                                try { await _s7Service.WriteBoolAsync("Q8.1", false); } catch { }
                                            }
                                        }
                                        
                                        controlExecuted = true;
                                        
                                        // 冷却时间，防止连击
                                        await Task.Delay(500, token); 
                                    }
                                }
                                else
                                {
                                    // 传感器信号消失，重置标记，准备下一次触发
                                    controlExecuted = false;
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[上料弯道] 控制循环异常: {ex.Message}");
                        await Task.Delay(1000);
                    }
                    
                    // 循环检测间隔
                    await Task.Delay(50, token);
                }
                System.Diagnostics.Debug.WriteLine("[上料弯道] 控制循环已停止");
            }, token);
        }

        /// <summary>
        /// 停止上料弯道控制循环
        /// </summary>
        private void StopLoadingCurveControlLoop()
        {
            try
            {
                if (_loadingCurveCts != null)
                {
                    _loadingCurveCts.Cancel();
                    _loadingCurveCts = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[上料弯道] 停止循环失败: {ex.Message}");
            }
        }


        /// <summary>
        /// 启动下料弯道控制循环
        /// 逻辑：当下料弯道传感器(I1.5)为true时，执行推箱(Q8.2)动作，随后收缩(Q8.3)
        /// </summary>
        private void StartUnloadingCurveControlLoop()
        {
            // 防止重复启动
            StopUnloadingCurveControlLoop();
            
            _unloadingCurveCts = new System.Threading.CancellationTokenSource();
            var token = _unloadingCurveCts.Token;

            Task.Run(async () => 
            {
                System.Diagnostics.Debug.WriteLine("[下料弯道] 控制循环已启动");
                bool controlExecuted = false; 

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (_s7Service.IsConnected)
                        {
                            // 检查下料弯道传感器 I1.5
                            bool isUnloadingSensorActive = await _s7Service.ReadBoolAsync("I1.5");
                            
                            if (isUnloadingSensorActive)
                            {
                                if (!controlExecuted)
                                {
                                    System.Diagnostics.Debug.WriteLine("[下料弯道] 检测到物体(I1.5)，执行推箱");
                                    
                                    // [修复] 使用脉冲触发模式，避免双线圈同时通电
                                    try
                                    {
                                        // 执行动作：脉冲触发模式
                                        // 伸出：发送短脉冲到Q8.2
                                        System.Diagnostics.Debug.WriteLine("[下料弯道] 发送伸出脉冲(Q8.2)");
                                        await _s7Service.WriteBoolAsync("Q8.2", true);
                                        await Task.Delay(1000); // [修复] 不使用token，确保脉冲完整执行
                                        await _s7Service.WriteBoolAsync("Q8.2", false); // 立即清零
                                        System.Diagnostics.Debug.WriteLine("[下料弯道] Q8.2 已清零");
                                        
                                        // 等待气缸伸出完成（3000ms）
                                        await Task.Delay(3000); 
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[下料弯道] 推箱伸出异常: {ex.Message}");
                                        // [关键] 异常时也要确保清零
                                        try { await _s7Service.WriteBoolAsync("Q8.2", false); } catch { }
                                    }
                                    finally
                                    {
                                        // [关键] 无论伸出是否成功，都必须执行收缩以恢复原位
                                        try
                                        {
                                            // 额外保险：再次确保Q8.2清零
                                            await _s7Service.WriteBoolAsync("Q8.2", false);
                                            
                                            // ===== 第一次收缩脉冲 =====
                                            System.Diagnostics.Debug.WriteLine("[下料弯道] 发送第1次收缩脉冲(Q8.3)");
                                            await _s7Service.WriteBoolAsync("Q8.3", true);
                                            await Task.Delay(1000); // 脉冲宽度 1000ms
                                            await _s7Service.WriteBoolAsync("Q8.3", false); // 清零
                                            System.Diagnostics.Debug.WriteLine("[下料弯道] Q8.3 第1次已清零");
                                            
                                            // 等待第一次收缩
                                            await Task.Delay(2000);
                                            
                                            // ===== 第二次强制收缩脉冲（保险）=====
                                            System.Diagnostics.Debug.WriteLine("[下料弯道] 发送第2次强制收缩脉冲(Q8.3)");
                                            await _s7Service.WriteBoolAsync("Q8.3", true);
                                            await Task.Delay(1000); // 脉冲宽度 1000ms
                                            await _s7Service.WriteBoolAsync("Q8.3", false); // 清零
                                            System.Diagnostics.Debug.WriteLine("[下料弯道] Q8.3 第2次已清零");
                                            
                                            // 等待气缸收缩完成
                                            await Task.Delay(2000);
                                            
                                            System.Diagnostics.Debug.WriteLine("[下料弯道] 推箱动作完成(双重收缩)");
                                        }
                                        catch (Exception retractEx)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[下料弯道] 收缩动作异常: {retractEx.Message}");
                                            // 异常时也要确保清零
                                            try { await _s7Service.WriteBoolAsync("Q8.3", false); } catch { }
                                        }
                                    }
                                    
                                    controlExecuted = true;
                                    
                                    // 冷却时间
                                    await Task.Delay(500, token); 
                                }
                            }
                            else
                            {
                                controlExecuted = false;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[下料弯道] 控制循环异常: {ex.Message}");
                        await Task.Delay(1000);
                    }
                    
                    await Task.Delay(50, token);
                }
                System.Diagnostics.Debug.WriteLine("[下料弯道] 控制循环已停止");
            }, token);
        }

        /// <summary>
        /// 停止下料弯道控制循环
        /// </summary>
        private void StopUnloadingCurveControlLoop()
        {
            try
            {
                if (_unloadingCurveCts != null)
                {
                    _unloadingCurveCts.Cancel();
                    _unloadingCurveCts = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[下料弯道] 停止循环失败: {ex.Message}");
            }
        }

        private async void ExecuteDeleteTask(Models.TaskItem item)
    {
        if (item == null) return;
        
        var result = System.Windows.MessageBox.Show($"确定要删除任务 {item.LogisticsBoxCode} 吗？\n注意：此操作不可恢复。", "确认删除", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            try
            {
                // [修复] TaskItem.Id是时间戳生成的临时ID，与数据库自增ID不同
                // 因此需要通过物流盒编号查找DetectionLog记录
                var boxNo = item.LogisticsBoxCode?.Replace("物流盒编码", "").Trim();
                if (string.IsNullOrWhiteSpace(boxNo))
                {
                    System.Windows.MessageBox.Show("无法获取物流盒编号，删除失败。", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                
                // 查找对应的DetectionLog记录
                var logs = await _detectionLogService.GetLogsByBoxCodeAsync(boxNo);
                var targetLog = logs?.OrderByDescending(l => l.CreateTime).FirstOrDefault();
                
                if (targetLog == null)
                {
                    System.Windows.MessageBox.Show("未找到对应的检测日志记录。", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    // 即使数据库中没有记录，也允许从UI列表中删除
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => 
                    {
                        Tasks.Remove(item);
                    });
                    return;
                }
                
                // 删除数据库记录
                bool success = await _detectionLogService.DeleteLogAsync(targetLog.Id);
                if (success)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => 
                    {
                        Tasks.Remove(item);
                    });
                    System.Diagnostics.Debug.WriteLine($"[任务删除] 成功删除: 物流盒={boxNo}, LogId={targetLog.Id}");
                }
                else
                {
                     System.Windows.MessageBox.Show("删除失败，可能是数据库操作异常。", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"删除过程中发生错误: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[任务删除] 异常: {ex}");
            }
        }
    }
    }
}
