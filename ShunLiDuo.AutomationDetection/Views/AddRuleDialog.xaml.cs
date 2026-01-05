using System.Windows;
using System.Linq;
using ShunLiDuo.AutomationDetection.ViewModels;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class AddRuleDialog : Window
    {
        public AddRuleDialogViewModel ViewModel { get; private set; }
        public bool IsEditMode { get; private set; }
        public int RuleId { get; private set; }

        public AddRuleDialog(RuleItem rule = null)
        {
            InitializeComponent();
            IsEditMode = rule != null;
            ViewModel = new AddRuleDialogViewModel(rule);
            DataContext = ViewModel;
            
            if (IsEditMode)
            {
                Title = "编辑规则";
                RuleId = rule.Id;
                Loaded += (s, e) => TitleTextBlock.Text = "编辑规则";
            }
            else
            {
                Title = "新增规则";
                Loaded += (s, e) => TitleTextBlock.Text = "新增规则";
            }
        }

        public string RuleNo => ViewModel.RuleNo;
        public string RuleName => ViewModel.RuleName;
        public string SelectedDetectionRooms => ViewModel.GetSelectedDetectionRooms();
        public string SelectedLogisticsBoxNos => ViewModel.GetSelectedLogisticsBoxNos();
        public string Remark => ViewModel.Remark;

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

