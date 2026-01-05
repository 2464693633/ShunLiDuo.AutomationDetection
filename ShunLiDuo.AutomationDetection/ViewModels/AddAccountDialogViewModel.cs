using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class AddAccountDialogViewModel : BindableBase
    {
        private string _accountNo;
        private string _loginAccount;
        private string _password;
        private string _name;
        private string _phone;
        private string _employeeNo;
        private string _remark;
        private string _selectedGender;
        private int? _selectedRoleId;
        private ObservableCollection<string> _genders;
        private ObservableCollection<RoleItem> _roles;
        private bool _isValid;
        private readonly IRoleService _roleService;

        public AddAccountDialogViewModel(IRoleService roleService, Models.UserItem account = null)
        {
            _roleService = roleService;
            Genders = new ObservableCollection<string> { "男", "女" };
            Roles = new ObservableCollection<RoleItem>();
            LoadRolesAsync();
            
            if (account != null)
            {
                AccountNo = account.AccountNo ?? string.Empty;
                LoginAccount = account.LoginAccount ?? string.Empty;
                Password = account.Password ?? string.Empty;
                Name = account.Name ?? string.Empty;
                SelectedGender = account.Gender ?? string.Empty;
                Phone = account.Phone ?? string.Empty;
                EmployeeNo = account.EmployeeNo ?? string.Empty;
                SelectedRoleId = account.RoleId;
                Remark = account.Remark ?? string.Empty;
            }
        }

        private async void LoadRolesAsync()
        {
            try
            {
                var roles = await _roleService.GetAllRolesAsync();
                Roles.Clear();
                foreach (var role in roles)
                {
                    Roles.Add(role);
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"加载角色列表失败: {ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public string AccountNo
        {
            get => _accountNo;
            set
            {
                SetProperty(ref _accountNo, value);
                Validate();
            }
        }

        public string LoginAccount
        {
            get => _loginAccount;
            set
            {
                SetProperty(ref _loginAccount, value);
                Validate();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                Validate();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                SetProperty(ref _name, value);
                Validate();
            }
        }

        public string Phone
        {
            get => _phone;
            set
            {
                SetProperty(ref _phone, value);
                Validate();
            }
        }

        public string EmployeeNo
        {
            get => _employeeNo;
            set
            {
                SetProperty(ref _employeeNo, value);
                Validate();
            }
        }

        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        public string SelectedGender
        {
            get => _selectedGender;
            set
            {
                SetProperty(ref _selectedGender, value);
                Validate();
            }
        }

        public int? SelectedRoleId
        {
            get => _selectedRoleId;
            set
            {
                SetProperty(ref _selectedRoleId, value);
                Validate();
            }
        }

        public ObservableCollection<string> Genders
        {
            get => _genders;
            set => SetProperty(ref _genders, value);
        }

        public ObservableCollection<RoleItem> Roles
        {
            get => _roles;
            set => SetProperty(ref _roles, value);
        }

        public bool IsValid
        {
            get => _isValid;
            set => SetProperty(ref _isValid, value);
        }

        private void Validate()
        {
            IsValid = !string.IsNullOrWhiteSpace(AccountNo)
                   && !string.IsNullOrWhiteSpace(LoginAccount)
                   && !string.IsNullOrWhiteSpace(Password)
                   && !string.IsNullOrWhiteSpace(Name)
                   && !string.IsNullOrWhiteSpace(SelectedGender)
                   && !string.IsNullOrWhiteSpace(Phone)
                   && !string.IsNullOrWhiteSpace(EmployeeNo)
                   && SelectedRoleId.HasValue;
        }
    }
}

