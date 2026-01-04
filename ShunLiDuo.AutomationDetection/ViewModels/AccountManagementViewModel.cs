using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Services;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class AccountManagementViewModel : BindableBase
    {
        private string _searchKeyword;
        private ObservableCollection<UserItem> _users;
        private int _totalCount;
        private bool _isLoading;
        private readonly IAccountService _accountService;

        public AccountManagementViewModel(IAccountService accountService)
        {
            _accountService = accountService;
            Users = new ObservableCollection<UserItem>();
            SearchCommand = new DelegateCommand(OnSearch, () => !IsLoading);
            AddCommand = new DelegateCommand(OnAdd, () => !IsLoading);
            LoadAccountsAsync();
        }

        private async void LoadAccountsAsync()
        {
            IsLoading = true;
            try
            {
                var accounts = await _accountService.GetAllAccountsAsync();
                Users.Clear();
                foreach (var account in accounts)
                {
                    Users.Add(account);
                }
                TotalCount = Users.Count;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"加载账户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnSearch()
        {
            IsLoading = true;
            try
            {
                var accounts = await _accountService.SearchAccountsAsync(SearchKeyword ?? string.Empty);
                Users.Clear();
                foreach (var account in accounts)
                {
                    Users.Add(account);
                }
                TotalCount = Users.Count;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"搜索账户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnAdd()
        {
            var dialog = new Views.AddAccountDialog();
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                try
                {
                    var newAccount = new UserItem
                    {
                        AccountNo = dialog.AccountNo ?? string.Empty,
                        LoginAccount = dialog.LoginAccount ?? string.Empty,
                        Password = dialog.Password ?? string.Empty,
                        Name = dialog.Name ?? string.Empty,
                        Gender = dialog.Gender ?? string.Empty,
                        Phone = dialog.Phone ?? string.Empty,
                        EmployeeNo = dialog.EmployeeNo ?? string.Empty,
                        Role = dialog.Role ?? string.Empty,
                        Remark = dialog.Remark ?? string.Empty
                    };

                    var success = await _accountService.AddAccountAsync(newAccount);
                    if (success)
                    {
                        MessageBox.Show("账户添加成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadAccountsAsync();
                    }
                    else
                    {
                        MessageBox.Show("账户添加失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"添加账户失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        public ObservableCollection<UserItem> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                SearchCommand.RaiseCanExecuteChanged();
                AddCommand.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand SearchCommand { get; private set; }
        public DelegateCommand AddCommand { get; private set; }
    }
}

