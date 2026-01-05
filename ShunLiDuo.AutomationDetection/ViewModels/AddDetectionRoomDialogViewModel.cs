using Prism.Mvvm;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class AddDetectionRoomDialogViewModel : BindableBase
    {
        private string _roomNo;
        private string _roomName;
        private string _remark;

        public AddDetectionRoomDialogViewModel(Models.DetectionRoomItem room = null)
        {
            if (room != null)
            {
                RoomNo = room.RoomNo ?? string.Empty;
                RoomName = room.RoomName ?? string.Empty;
                Remark = room.Remark ?? string.Empty;
            }
        }

        public string RoomNo
        {
            get => _roomNo;
            set => SetProperty(ref _roomNo, value);
        }

        public string RoomName
        {
            get => _roomName;
            set => SetProperty(ref _roomName, value);
        }

        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }
    }
}

