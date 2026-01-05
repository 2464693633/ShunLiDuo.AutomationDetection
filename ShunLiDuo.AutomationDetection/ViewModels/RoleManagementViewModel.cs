using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Services;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class RoleManagementViewModel : BindableBase
    {
        private string _searchKeyword;
        private ObservableCollection<RoleItem> _roles;
        private int _totalCount;
        private bool _isLoading;
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;

        public RoleManagementViewModel(IRoleService roleService, IPermissionService permissionService)
        {
            _roleService = roleService;
            _permissionService = permissionService;
            Roles = new ObservableCollection<RoleItem>();
            SearchCommand = new DelegateCommand(OnSearch, () => !IsLoading);
            AddCommand = new DelegateCommand(OnAdd, () => !IsLoading);
            LoadRolesAsync();
        }

        public async void LoadRolesAsync()
        {
            IsLoading = true;
            try
            {
                var roles = await _roleService.GetAllRolesAsync();
                Roles.Clear();
                foreach (var role in roles)
                {
                    Roles.Add(role);
                }
                TotalCount = Roles.Count;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"加载角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var roles = await _roleService.SearchRolesAsync(SearchKeyword ?? string.Empty);
                Roles.Clear();
                foreach (var role in roles)
                {
                    Roles.Add(role);
                }
                TotalCount = Roles.Count;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"搜索角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnAdd()
        {
            var dialog = new Views.AddRoleDialog(_permissionService);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                try
                {
                    var newRole = new RoleItem
                    {
                        RoleName = dialog.RoleName ?? string.Empty,
                        Remark = dialog.Remark ?? string.Empty,
                        Permissions = dialog.Permissions ?? string.Empty
                    };

                    var success = await _roleService.AddRoleAsync(newRole);
                    if (success)
                    {
                        MessageBox.Show("角色添加成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadRolesAsync();
                    }
                    else
                    {
                        MessageBox.Show("角色添加失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"添加角色失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        public ObservableCollection<RoleItem> Roles
        {
            get => _roles;
            set => SetProperty(ref _roles, value);
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

