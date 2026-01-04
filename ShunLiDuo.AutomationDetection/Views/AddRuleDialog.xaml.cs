using System.Windows;
using System.Linq;
using ShunLiDuo.AutomationDetection.ViewModels;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class AddRuleDialog : Window
    {
        public AddRuleDialogViewModel ViewModel { get; private set; }

        public AddRuleDialog()
        {
            InitializeComponent();
            ViewModel = new AddRuleDialogViewModel();
            DataContext = ViewModel;
        }

        public string RuleNo => ViewModel.RuleNo;
        public string RuleName => ViewModel.RuleName;
        public string SelectedDetectionRooms => ViewModel.GetSelectedDetectionRooms();
        public string SelectedLogisticsBoxNos => ViewModel.GetSelectedLogisticsBoxNos();
        public string Remark => ViewModel.Remark;

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

