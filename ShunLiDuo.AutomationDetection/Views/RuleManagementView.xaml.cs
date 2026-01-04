using System.Windows.Controls;
using Prism.Ioc;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.ViewModels;

namespace ShunLiDuo.AutomationDetection.Views
{
    public partial class RuleManagementView : UserControl
    {
        public RuleManagementView(IRuleService ruleService, IDetectionRoomService detectionRoomService, ILogisticsBoxService logisticsBoxService)
        {
            InitializeComponent();
            DataContext = new RuleManagementViewModel(ruleService, detectionRoomService, logisticsBoxService);
        }
    }
}

