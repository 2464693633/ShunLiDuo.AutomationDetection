using System.Windows.Controls;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.ViewModels;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class DetectionRoomManagementView : UserControl
    {
        public DetectionRoomManagementView(IDetectionRoomService detectionRoomService)
        {
            InitializeComponent();
            DataContext = new DetectionRoomManagementViewModel(detectionRoomService);
        }
    }
}

