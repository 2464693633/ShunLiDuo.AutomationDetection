using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class DetectionRoomSelectItem : BindableBase
    {
        private bool _isSelected;
        public DetectionRoomItem Room { get; set; }
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        public string RoomName => Room?.RoomName ?? string.Empty;
    }

    public class LogisticsBoxSelectItem : BindableBase
    {
        private bool _isSelected;
        public LogisticsBoxItem Box { get; set; }
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        public string BoxNo => Box?.BoxNo ?? string.Empty;
    }

    public class AddRuleDialogViewModel : BindableBase
    {
        private string _ruleNo;
        private string _ruleName;
        private string _remark;
        private ObservableCollection<DetectionRoomSelectItem> _detectionRoomSelectItems;
        private ObservableCollection<LogisticsBoxSelectItem> _logisticsBoxSelectItems;

        public AddRuleDialogViewModel()
        {
            InitializeData();
        }

        private System.Collections.Generic.List<string> _pendingSelectedRoomNames = new System.Collections.Generic.List<string>();
        private System.Collections.Generic.List<string> _pendingSelectedBoxNos = new System.Collections.Generic.List<string>();

        public AddRuleDialogViewModel(RuleItem rule) : this()
        {
            if (rule != null)
            {
                RuleNo = rule.RuleNo ?? string.Empty;
                RuleName = rule.RuleName ?? string.Empty;
                Remark = rule.Remark ?? string.Empty;
                
                // 保存选中的检测室和物流盒名称，在LoadDetectionRooms和LoadLogisticsBoxes时使用
                _pendingSelectedRoomNames = !string.IsNullOrWhiteSpace(rule.DetectionRooms) 
                    ? rule.DetectionRooms.Split(',')
                        .Select(r => r.Trim())
                        .Where(r => !string.IsNullOrWhiteSpace(r))
                        .ToList()
                    : new System.Collections.Generic.List<string>();
                
                _pendingSelectedBoxNos = !string.IsNullOrWhiteSpace(rule.LogisticsBoxNos)
                    ? rule.LogisticsBoxNos.Split(',')
                        .Select(b => b.Trim())
                        .Where(b => !string.IsNullOrWhiteSpace(b))
                        .ToList()
                    : new System.Collections.Generic.List<string>();
            }
        }

        private void InitializeData()
        {
            // 初始化检测室选择项列表
            DetectionRoomSelectItems = new ObservableCollection<DetectionRoomSelectItem>();
            
            // 初始化物流盒选择项列表
            LogisticsBoxSelectItems = new ObservableCollection<LogisticsBoxSelectItem>();
        }

        public string RuleNo
        {
            get => _ruleNo;
            set
            {
                SetProperty(ref _ruleNo, value);
                Validate();
            }
        }

        public string RuleName
        {
            get => _ruleName;
            set
            {
                SetProperty(ref _ruleName, value);
                Validate();
            }
        }

        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        public ObservableCollection<DetectionRoomSelectItem> DetectionRoomSelectItems
        {
            get => _detectionRoomSelectItems;
            set => SetProperty(ref _detectionRoomSelectItems, value);
        }

        public ObservableCollection<LogisticsBoxSelectItem> LogisticsBoxSelectItems
        {
            get => _logisticsBoxSelectItems;
            set => SetProperty(ref _logisticsBoxSelectItems, value);
        }

        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            set => SetProperty(ref _isValid, value);
        }

        private void Validate()
        {
            var hasSelectedRooms = DetectionRoomSelectItems != null && 
                                   DetectionRoomSelectItems.Any(item => item.IsSelected);
            var hasSelectedBoxes = LogisticsBoxSelectItems != null && 
                                   LogisticsBoxSelectItems.Any(item => item.IsSelected);
            
            IsValid = !string.IsNullOrWhiteSpace(RuleNo) 
                   && !string.IsNullOrWhiteSpace(RuleName)
                   && hasSelectedRooms
                   && hasSelectedBoxes;
        }

        public void LoadDetectionRooms(ObservableCollection<DetectionRoomItem> rooms)
        {
            DetectionRoomSelectItems.Clear();
            if (rooms != null)
            {
                foreach (var room in rooms)
                {
                    var item = new DetectionRoomSelectItem
                    {
                        Room = room,
                        IsSelected = _pendingSelectedRoomNames.Contains(room.RoomName)
                    };
                    // 订阅选择变化事件
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(DetectionRoomSelectItem.IsSelected))
                        {
                            Validate();
                        }
                    };
                    DetectionRoomSelectItems.Add(item);
                }
            }
            _pendingSelectedRoomNames.Clear(); // 清除待处理的选中项
            Validate();
        }

        public void LoadLogisticsBoxes(ObservableCollection<LogisticsBoxItem> boxes)
        {
            LogisticsBoxSelectItems.Clear();
            if (boxes != null)
            {
                foreach (var box in boxes)
                {
                    var item = new LogisticsBoxSelectItem
                    {
                        Box = box,
                        IsSelected = _pendingSelectedBoxNos.Contains(box.BoxNo)
                    };
                    // 订阅选择变化事件
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(LogisticsBoxSelectItem.IsSelected))
                        {
                            Validate();
                        }
                    };
                    LogisticsBoxSelectItems.Add(item);
                }
            }
            _pendingSelectedBoxNos.Clear(); // 清除待处理的选中项
            Validate();
        }

        public string GetSelectedDetectionRooms()
        {
            if (DetectionRoomSelectItems == null)
                return string.Empty;
            
            var selectedRooms = DetectionRoomSelectItems
                .Where(item => item.IsSelected)
                .Select(item => item.RoomName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();
            
            return string.Join(",", selectedRooms);
        }

        public string GetSelectedLogisticsBoxNos()
        {
            if (LogisticsBoxSelectItems == null)
                return string.Empty;
            
            var selectedBoxes = LogisticsBoxSelectItems
                .Where(item => item.IsSelected)
                .Select(item => item.BoxNo)
                .Where(no => !string.IsNullOrWhiteSpace(no))
                .ToList();
            
            return string.Join(",", selectedBoxes);
        }
    }
}

