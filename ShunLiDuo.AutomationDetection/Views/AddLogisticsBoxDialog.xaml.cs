using System.Windows;
using ShunLiDuo.AutomationDetection.ViewModels;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class AddLogisticsBoxDialog : Window
    {
        public AddLogisticsBoxDialogViewModel ViewModel { get; private set; }

        public AddLogisticsBoxDialog()
        {
            InitializeComponent();
            ViewModel = new AddLogisticsBoxDialogViewModel();
            DataContext = ViewModel;
        }

        public string BoxNo => ViewModel.BoxNo;
        public string BoxName => ViewModel.BoxName;
        public string Remark => ViewModel.Remark;

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

