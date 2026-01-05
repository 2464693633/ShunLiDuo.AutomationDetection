using System.Windows;
using System.Windows.Input;
using ShunLiDuo.AutomationDetection.ViewModels;
using ShunLiDuo.AutomationDetection.Services;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindowViewModel ViewModel { get; private set; }

        public LoginWindow(IAccountService accountService)
        {
            InitializeComponent();
            ViewModel = new LoginWindowViewModel(accountService);
            DataContext = ViewModel;
            
            // 监听登录成功事件
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.LoginSuccess) && ViewModel.LoginSuccess)
            {
                // 设置 DialogResult 会自动关闭窗口
                DialogResult = true;
            }
        }

        public Models.UserItem CurrentUser => ViewModel?.CurrentUser;

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.Password = PasswordBox.Password;
            }
        }

        private void LoginAccountTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PasswordBox.Focus();
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ViewModel.LoginCommand.CanExecute())
            {
                ViewModel.LoginCommand.Execute();
            }
        }
    }
}

