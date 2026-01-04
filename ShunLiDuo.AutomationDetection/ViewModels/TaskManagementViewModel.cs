using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class TaskManagementViewModel : BindableBase
    {
        private readonly IRuleService _ruleService;
        private readonly IDetectionRoomService _detectionRoomService;
        private string _logisticsBoxCode;
        private string _logisticsBoxInputInfo;
        private RuleItem _selectedRuleItem;
        private ObservableCollection<Models.TaskItem> _tasks;
        private ObservableCollection<RuleItem> _rules;
        private ObservableCollection<Models.DetectionRoomItem> _detectionRooms;
        private ObservableCollection<string> _logisticsBoxList;
        private ObservableCollection<string> _room1Boxes;
        private ObservableCollection<string> _room2Boxes;
        private ObservableCollection<string> _room3Boxes;
        private ObservableCollection<string> _room4Boxes;
        private ObservableCollection<string> _room5Boxes;

        public TaskManagementViewModel(IRuleService ruleService, IDetectionRoomService detectionRoomService)
        {
            _ruleService = ruleService;
            _detectionRoomService = detectionRoomService;
            InitializeData();
            ExecuteCommand = new DelegateCommand(OnExecute);
            LoadRulesAsync();
            LoadDetectionRoomsAsync();
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

            Room1Boxes = new ObservableCollection<string>();
            Room2Boxes = new ObservableCollection<string>();
            Room3Boxes = new ObservableCollection<string>();
            Room4Boxes = new ObservableCollection<string>();
            Room5Boxes = new ObservableCollection<string>();

            LogisticsBoxList = new ObservableCollection<string>();
            Rules = new ObservableCollection<RuleItem>();
            DetectionRooms = new ObservableCollection<Models.DetectionRoomItem>();
        }

        private async void LoadRulesAsync()
        {
            try
            {
                var rules = await _ruleService.GetAllRulesAsync();
                Rules.Clear();
                foreach (var rule in rules)
                {
                    Rules.Add(rule);
                }
                
                // 默认选择第一个规则
                if (Rules.Count > 0)
                {
                    SelectedRuleItem = Rules[0];
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"加载规则失败: {ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void LoadDetectionRoomsAsync()
        {
            try
            {
                var rooms = await _detectionRoomService.GetAllRoomsAsync();
                DetectionRooms.Clear();
                foreach (var room in rooms)
                {
                    DetectionRooms.Add(room);
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"加载检测室失败: {ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
            Room1Boxes.Clear();
            Room2Boxes.Clear();
            Room3Boxes.Clear();
            Room4Boxes.Clear();
            Room5Boxes.Clear();

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
            if (SelectedRuleItem == null || DetectionRooms == null || DetectionRooms.Count == 0) return;

            // 提取物流盒编号（去掉"物流盒编码"前缀）
            var boxNo = boxCode.Replace("物流盒编码", "").Trim();
            
            // 解析规则中的检测室编号和物流盒编号
            var ruleDetectionRoomNos = SelectedRuleItem.DetectionRooms?.Split(',')
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
                // 根据规则中的检测室编号匹配到实际的检测室
                foreach (var ruleRoomNo in ruleDetectionRoomNos)
                {
                    // 查找匹配的检测室
                    var matchedRoom = DetectionRooms.FirstOrDefault(r => 
                        r.RoomNo == ruleRoomNo || 
                        r.RoomName == ruleRoomNo ||
                        r.RoomNo.Contains(ruleRoomNo) ||
                        ruleRoomNo.Contains(r.RoomNo));
                    
                    if (matchedRoom != null)
                    {
                        // 根据检测室编号分配到对应的检测室列表
                        // 假设检测室编号格式为 "1", "2", "3" 等，或包含数字
                        var roomNo = matchedRoom.RoomNo;
                        if (roomNo.Contains("1") || matchedRoom.RoomName.Contains("1"))
                        {
                            if (!Room1Boxes.Contains(boxCode))
                                Room1Boxes.Add(boxCode);
                        }
                        else if (roomNo.Contains("2") || matchedRoom.RoomName.Contains("2"))
                        {
                            if (!Room2Boxes.Contains(boxCode))
                                Room2Boxes.Add(boxCode);
                        }
                        else if (roomNo.Contains("3") || matchedRoom.RoomName.Contains("3"))
                        {
                            if (!Room3Boxes.Contains(boxCode))
                                Room3Boxes.Add(boxCode);
                        }
                        else if (roomNo.Contains("4") || matchedRoom.RoomName.Contains("4"))
                        {
                            if (!Room4Boxes.Contains(boxCode))
                                Room4Boxes.Add(boxCode);
                        }
                        else if (roomNo.Contains("5") || matchedRoom.RoomName.Contains("5"))
                        {
                            if (!Room5Boxes.Contains(boxCode))
                                Room5Boxes.Add(boxCode);
                        }
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
                        Room1Boxes.Clear();
                        Room2Boxes.Clear();
                        Room3Boxes.Clear();
                        Room4Boxes.Clear();
                        Room5Boxes.Clear();
                        
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

        public ObservableCollection<string> Room1Boxes
        {
            get => _room1Boxes;
            set => SetProperty(ref _room1Boxes, value);
        }

        public ObservableCollection<string> Room2Boxes
        {
            get => _room2Boxes;
            set => SetProperty(ref _room2Boxes, value);
        }

        public ObservableCollection<string> Room3Boxes
        {
            get => _room3Boxes;
            set => SetProperty(ref _room3Boxes, value);
        }

        public ObservableCollection<string> Room4Boxes
        {
            get => _room4Boxes;
            set => SetProperty(ref _room4Boxes, value);
        }

        public ObservableCollection<string> Room5Boxes
        {
            get => _room5Boxes;
            set => SetProperty(ref _room5Boxes, value);
        }

        public DelegateCommand ExecuteCommand { get; private set; }
    }
}

