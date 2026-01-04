using Prism.Mvvm;
using System.Collections.ObjectModel;

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
        private string _selectedRole;
        private ObservableCollection<string> _genders;
        private ObservableCollection<string> _roles;
        private bool _isValid;

        public AddAccountDialogViewModel()
        {
            InitializeData();
        }

        private void InitializeData()
        {
            Genders = new ObservableCollection<string> { "男", "女" };
            Roles = new ObservableCollection<string> { "管理员", "操作员", "查看员" };
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

        public string SelectedRole
        {
            get => _selectedRole;
            set
            {
                SetProperty(ref _selectedRole, value);
                Validate();
            }
        }

        public ObservableCollection<string> Genders
        {
            get => _genders;
            set => SetProperty(ref _genders, value);
        }

        public ObservableCollection<string> Roles
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
                   && !string.IsNullOrWhiteSpace(SelectedRole);
        }
    }
}

