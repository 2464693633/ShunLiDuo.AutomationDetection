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
        private ObservableCollection<DetectionRoomItem> _detectionRooms;
        private ObservableCollection<LogisticsBoxItem> _logisticsBoxes;
        private DetectionRoomItem _selectedDetectionRoom;
        private LogisticsBoxItem _selectedLogisticsBox;

        public AddRuleDialogViewModel()
        {
            InitializeData();
        }

        private void InitializeData()
        {
            // 初始化检测室列表
            DetectionRooms = new ObservableCollection<DetectionRoomItem>();
            
            // 初始化物流盒列表
            LogisticsBoxes = new ObservableCollection<LogisticsBoxItem>();
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

        public ObservableCollection<DetectionRoomItem> DetectionRooms
        {
            get => _detectionRooms;
            set => SetProperty(ref _detectionRooms, value);
        }

        public ObservableCollection<LogisticsBoxItem> LogisticsBoxes
        {
            get => _logisticsBoxes;
            set => SetProperty(ref _logisticsBoxes, value);
        }

        public DetectionRoomItem SelectedDetectionRoom
        {
            get => _selectedDetectionRoom;
            set
            {
                SetProperty(ref _selectedDetectionRoom, value);
                Validate();
            }
        }

        public LogisticsBoxItem SelectedLogisticsBox
        {
            get => _selectedLogisticsBox;
            set
            {
                SetProperty(ref _selectedLogisticsBox, value);
                Validate();
            }
        }

        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            set => SetProperty(ref _isValid, value);
        }

        private void Validate()
        {
            IsValid = !string.IsNullOrWhiteSpace(RuleNo) 
                   && !string.IsNullOrWhiteSpace(RuleName)
                   && SelectedDetectionRoom != null
                   && SelectedLogisticsBox != null;
        }

        public void LoadDetectionRooms(ObservableCollection<DetectionRoomItem> rooms)
        {
            DetectionRooms = rooms ?? new ObservableCollection<DetectionRoomItem>();
            Validate();
        }

        public void LoadLogisticsBoxes(ObservableCollection<LogisticsBoxItem> boxes)
        {
            LogisticsBoxes = boxes ?? new ObservableCollection<LogisticsBoxItem>();
            Validate();
        }

        public string GetSelectedDetectionRooms()
        {
            return SelectedDetectionRoom?.RoomName ?? string.Empty;
        }

        public string GetSelectedLogisticsBoxNos()
        {
            return SelectedLogisticsBox?.BoxNo ?? string.Empty;
        }
    }
}

