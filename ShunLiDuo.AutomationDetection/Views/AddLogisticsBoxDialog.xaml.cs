using System.Windows;
using ShunLiDuo.AutomationDetection.ViewModels;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class AddLogisticsBoxDialog : Window
    {
        public AddLogisticsBoxDialogViewModel ViewModel { get; private set; }
        public bool IsEditMode { get; private set; }
        public int BoxId { get; private set; }

        public AddLogisticsBoxDialog(Models.LogisticsBoxItem box = null)
        {
            InitializeComponent();
            IsEditMode = box != null;
            ViewModel = new AddLogisticsBoxDialogViewModel(box);
            DataContext = ViewModel;
            
            if (IsEditMode)
            {
                Title = "编辑物流盒";
                BoxId = box.Id;
                Loaded += (s, e) => TitleTextBlock.Text = "编辑物流盒";
            }
            else
            {
                Title = "新增物流盒";
                Loaded += (s, e) => TitleTextBlock.Text = "新增物流盒";
            }
        }

        public string BoxNo => ViewModel.BoxNo;
        public string BoxName => ViewModel.BoxName;
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
            if (ViewModel.IsValid)
            {
                DialogResult = true;
                Close();
            }
        }
    }
}

