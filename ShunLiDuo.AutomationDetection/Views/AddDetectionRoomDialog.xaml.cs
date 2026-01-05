using System.Windows;
using ShunLiDuo.AutomationDetection.ViewModels;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class AddDetectionRoomDialog : Window
    {
        public AddDetectionRoomDialogViewModel ViewModel { get; private set; }
        public bool IsEditMode { get; private set; }
        public int RoomId { get; private set; }

        public AddDetectionRoomDialog(Models.DetectionRoomItem room = null)
        {
            InitializeComponent();
            IsEditMode = room != null;
            ViewModel = new AddDetectionRoomDialogViewModel(room);
            DataContext = ViewModel;
            
            if (IsEditMode)
            {
                Title = "编辑检测室";
                RoomId = room.Id;
                Loaded += (s, e) => TitleTextBlock.Text = "编辑检测室";
            }
            else
            {
                Title = "新增检测室";
                Loaded += (s, e) => TitleTextBlock.Text = "新增检测室";
            }
        }

        public string RoomNo => ViewModel.RoomNo;
        public string RoomName => ViewModel.RoomName;
        public string Remark => ViewModel.Remark;

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

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

