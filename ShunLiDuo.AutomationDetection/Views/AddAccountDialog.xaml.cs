using System.Windows;
using System.Windows.Controls;
using ShunLiDuo.AutomationDetection.ViewModels;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class AddAccountDialog : Window
    {
        public AddAccountDialogViewModel ViewModel { get; private set; }

        public AddAccountDialog()
        {
            InitializeComponent();
            ViewModel = new AddAccountDialogViewModel();
            DataContext = ViewModel;
        }

        public string AccountNo => ViewModel.AccountNo;
        public string LoginAccount => ViewModel.LoginAccount;
        public string Password => ViewModel.Password;
        public string Name => ViewModel.Name;
        public string Gender => ViewModel.SelectedGender;
        public string Phone => ViewModel.Phone;
        public string EmployeeNo => ViewModel.EmployeeNo;
        public string Role => ViewModel.SelectedRole;
        public string Remark => ViewModel.Remark;

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                ViewModel.Password = passwordBox.Password;
            }
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

