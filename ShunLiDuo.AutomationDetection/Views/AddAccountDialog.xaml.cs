using System.Windows;
using System.Windows.Controls;
using ShunLiDuo.AutomationDetection.ViewModels;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class AddAccountDialog : Window
    {
        public AddAccountDialogViewModel ViewModel { get; private set; }
        public bool IsEditMode { get; private set; }
        public int AccountId { get; private set; }

        public AddAccountDialog(Services.IRoleService roleService, UserItem account = null)
        {
            InitializeComponent();
            IsEditMode = account != null;
            ViewModel = new ViewModels.AddAccountDialogViewModel(roleService, account);
            DataContext = ViewModel;
            
            if (IsEditMode)
            {
                Title = "编辑账户";
                AccountId = account.Id;
                // 在Loaded事件中设置PasswordBox的密码
                Loaded += (s, e) =>
                {
                    if (account != null)
                    {
                        PasswordBox.Password = account.Password ?? string.Empty;
                    }
                    TitleTextBlock.Text = "编辑账户";
                };
            }
            else
            {
                Title = "新增账户";
                Loaded += (s, e) => TitleTextBlock.Text = "新增账户";
            }
        }

        public string AccountNo => ViewModel.AccountNo;
        public string LoginAccount => ViewModel.LoginAccount;
        public string Password => ViewModel.Password;
        public string Name => ViewModel.Name;
        public string Gender => ViewModel.SelectedGender;
        public string Phone => ViewModel.Phone;
        public string EmployeeNo => ViewModel.EmployeeNo;
        public int? RoleId => ViewModel.SelectedRoleId;
        public string Remark => ViewModel.Remark;

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                ViewModel.Password = passwordBox.Password;
            }
        }

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

