using System.Windows;
using System.Windows.Controls;
using ShunLiDuo.AutomationDetection.ViewModels;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class AddRoleDialog : Window
    {
        public AddRoleDialogViewModel ViewModel { get; private set; }

        public AddRoleDialog(string existingPermissions = null)
        {
            InitializeComponent();
            ViewModel = new AddRoleDialogViewModel(existingPermissions);
            DataContext = ViewModel;
        }

        public string RoleName => ViewModel.RoleName;
        public string Remark => ViewModel.Remark;
        public string Permissions => ViewModel.GetSelectedPermissions();

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is PermissionItem item)
            {
                item.IsSelected = true;
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is PermissionItem item)
            {
                item.IsSelected = false;
            }
        }

        private void CheckBox_Indeterminate(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is PermissionItem item)
            {
                item.IsIndeterminate = true;
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

