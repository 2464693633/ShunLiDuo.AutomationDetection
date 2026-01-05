using System.Windows;
using System.Windows.Controls;
using ShunLiDuo.AutomationDetection.ViewModels;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class AddRoleDialog : Window
    {
        public AddRoleDialogViewModel ViewModel { get; private set; }
        public bool IsEditMode { get; private set; }
        public int RoleId { get; private set; }

        public AddRoleDialog(Services.IPermissionService permissionService, Models.RoleItem role = null)
        {
            InitializeComponent();
            IsEditMode = role != null;
            ViewModel = new AddRoleDialogViewModel(permissionService, role);
            DataContext = ViewModel;
            
            if (IsEditMode)
            {
                Title = "编辑角色";
                RoleId = role.Id;
                Loaded += (s, e) => TitleTextBlock.Text = "编辑角色";
            }
            else
            {
                Title = "新增角色";
                Loaded += (s, e) => TitleTextBlock.Text = "新增角色";
            }
        }

        public string RoleName => ViewModel.RoleName;
        public string Remark => ViewModel.Remark;
        public string Permissions => ViewModel.GetSelectedPermissions();

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is PermissionItem item)
            {
                // 避免在加载权限时触发不必要的更新
                if (!item.IsSelected)
                {
                    item.IsSelected = true;
                }
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is PermissionItem item)
            {
                // 避免在加载权限时触发不必要的更新
                if (item.IsSelected)
                {
                    item.IsSelected = false;
                }
            }
        }

        private void CheckBox_Indeterminate(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is PermissionItem item)
            {
                // 设置 Indeterminate 状态时，不直接修改 IsSelected
                // 让 UpdateSelectionState 来处理
                if (!item.IsIndeterminate)
                {
                    item.IsIndeterminate = true;
                }
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

