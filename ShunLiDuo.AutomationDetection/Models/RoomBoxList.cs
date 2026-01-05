using System.Collections.ObjectModel;
using Prism.Mvvm;

namespace ShunLiDuo.AutomationDetection.Models
{
    public class RoomBoxList : BindableBase
    {
        private DetectionRoomItem _room;
        private ObservableCollection<string> _boxes;

        public RoomBoxList()
        {
            Boxes = new ObservableCollection<string>();
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
    }
}

