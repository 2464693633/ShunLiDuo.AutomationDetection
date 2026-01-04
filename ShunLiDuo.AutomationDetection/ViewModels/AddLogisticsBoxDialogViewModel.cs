using Prism.Mvvm;


namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class AddLogisticsBoxDialogViewModel : BindableBase
    {
        private string _boxNo;
        private string _boxName;
        private string _remark;

        public string BoxNo
        {
            get => _boxNo;
            set
            {
                SetProperty(ref _boxNo, value);
                Validate();
            }
        }

        public string BoxName
        {
            get => _boxName;
            set
            {
                SetProperty(ref _boxName, value);
                Validate();
            }
        }

        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            set => SetProperty(ref _isValid, value);
        }

        private void Validate()
        {
            IsValid = !string.IsNullOrWhiteSpace(BoxNo) && !string.IsNullOrWhiteSpace(BoxName);
        }
    }
}

