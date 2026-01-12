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
                // 直接设置选中状态，让 PermissionItem 的逻辑处理子项
                item.IsSelected = true;
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is PermissionItem item)
            {
                // 直接设置未选中状态，让 PermissionItem 的逻辑处理子项
                item.IsSelected = false;
            }
        }

        private void CheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is PermissionItem item)
            {
                // 同步初始状态
                UpdateCheckBoxState(checkBox, item);
                
                // 订阅 PropertyChanged 事件以同步状态变化
                item.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(PermissionItem.IsIndeterminate) || 
                        args.PropertyName == nameof(PermissionItem.IsSelected))
                    {
                        // 当 IsIndeterminate 或 IsSelected 改变时更新复选框状态
                        UpdateCheckBoxState(checkBox, item);
                    }
                };
            }
        }

        private void UpdateCheckBoxState(CheckBox checkBox, PermissionItem item)
        {
            // 临时解除事件处理，避免触发 Checked/Unchecked 事件
            checkBox.Checked -= CheckBox_Checked;
            checkBox.Unchecked -= CheckBox_Unchecked;
            checkBox.Indeterminate -= CheckBox_Indeterminate;
            
            if (item.IsIndeterminate)
            {
                checkBox.IsChecked = null;
            }
            else
            {
                checkBox.IsChecked = item.IsSelected;
            }
            
            // 重新订阅事件
            checkBox.Checked += CheckBox_Checked;
            checkBox.Unchecked += CheckBox_Unchecked;
            checkBox.Indeterminate += CheckBox_Indeterminate;
        }

        private void CheckBox_Indeterminate(object sender, RoutedEventArgs e)
        {
            // Indeterminate 状态通常由父权限自动管理，不需要手动处理
            // 当用户点击进入 Indeterminate 状态时，通常是因为部分子项被选中
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

