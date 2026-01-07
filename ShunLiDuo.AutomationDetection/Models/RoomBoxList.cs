using System.Collections.ObjectModel;
using Prism.Mvvm;

namespace ShunLiDuo.AutomationDetection.Models
{
    public class RoomBoxList : BindableBase
    {
        private DetectionRoomItem _room;
        private ObservableCollection<string> _boxes;
        private string _lastScannedCode;
        private string _scanStatus; // "等待扫码", "匹配成功", "匹配失败"

        public RoomBoxList()
        {
            Boxes = new ObservableCollection<string>();
            ScanStatus = "等待扫码";
        }

        public DetectionRoomItem Room
        {
            get => _room;
            set => SetProperty(ref _room, value);
        }

        public ObservableCollection<string> Boxes
        {
            get => _boxes;
            set => SetProperty(ref _boxes, value);
        }

        public string LastScannedCode
        {
            get => _lastScannedCode;
            set => SetProperty(ref _lastScannedCode, value);
        }

        public string ScanStatus
        {
            get => _scanStatus;
            set => SetProperty(ref _scanStatus, value);
        }
    }
}

