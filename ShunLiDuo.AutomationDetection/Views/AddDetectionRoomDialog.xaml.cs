using System.Windows;
using ShunLiDuo.AutomationDetection.ViewModels;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class AddDetectionRoomDialog : Window
    {
        public AddDetectionRoomDialogViewModel ViewModel { get; private set; }

        public AddDetectionRoomDialog()
        {
            InitializeComponent();
            ViewModel = new AddDetectionRoomDialogViewModel();
            DataContext = ViewModel;
        }

        public string RoomNo => ViewModel.RoomNo;
        public string RoomName => ViewModel.RoomName;
        public string Remark => ViewModel.Remark;

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

