using Prism.Mvvm;
using Prism.Commands;
using System.Linq;
using System.Windows;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class LoginWindowViewModel : BindableBase
    {
        private string _loginAccount;
        private string _password;
        private bool _isLoading;
        private string _errorMessage;
        private readonly IAccountService _accountService;

        public LoginWindowViewModel(IAccountService accountService)
        {
            _accountService = accountService;
            LoginCommand = new DelegateCommand(OnLogin, () => !IsLoading && !string.IsNullOrWhiteSpace(LoginAccount) && !string.IsNullOrWhiteSpace(Password));
            CancelCommand = new DelegateCommand(OnCancel);
        }

        public string LoginAccount
        {
            get => _loginAccount;
            set
            {
                SetProperty(ref _loginAccount, value);
                LoginCommand.RaiseCanExecuteChanged();
                // 清空错误信息
                if (!string.IsNullOrEmpty(ErrorMessage))
                {
                    ErrorMessage = string.Empty;
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                LoginCommand.RaiseCanExecuteChanged();
                // 清空错误信息
                if (!string.IsNullOrEmpty(ErrorMessage))
                {
                    ErrorMessage = string.Empty;
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                LoginCommand.RaiseCanExecuteChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public DelegateCommand LoginCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }

        public UserItem CurrentUser { get; private set; }
        
        private bool _loginSuccess;
        public bool LoginSuccess
        {
            get => _loginSuccess;
            private set => SetProperty(ref _loginSuccess, value);
        }

        private async void OnLogin()
        {
            if (string.IsNullOrWhiteSpace(LoginAccount) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "请输入账号和密码";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var user = await _accountService.ValidateLoginAsync(LoginAccount, Password);
                if (user != null)
                {
                    CurrentUser = user;
                    LoginSuccess = true;
                }
                else
                {
                    ErrorMessage = "账号或密码错误";
                    LoginSuccess = false;
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"登录失败: {ex.Message}";
                LoginSuccess = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnCancel()
        {
            Application.Current.Shutdown();
        }
    }
}

