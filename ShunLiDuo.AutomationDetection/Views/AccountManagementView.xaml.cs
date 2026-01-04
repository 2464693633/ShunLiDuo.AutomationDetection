using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.ViewModels;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class AccountManagementView : UserControl
    {
        public AccountManagementView(IAccountService accountService)
        {
            InitializeComponent();
            DataContext = new AccountManagementViewModel(accountService);
        }

        private void Edit_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("编辑功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Delete_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("删除功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void View_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("查看功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

