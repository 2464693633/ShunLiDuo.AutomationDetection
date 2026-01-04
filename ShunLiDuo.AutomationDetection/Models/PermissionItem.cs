using System.Collections.ObjectModel;
using Prism.Mvvm;

namespace ShunLiDuo.AutomationDetection.Models
{
    public class PermissionItem : BindableBase
    {
        private string _name;
        private string _code;
        private bool _isSelected;
        private bool _isIndeterminate;
        private ObservableCollection<PermissionItem> _children;
        private PermissionItem _parent;

        public PermissionItem()
        {
            Children = new ObservableCollection<PermissionItem>();
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    UpdateIndeterminateState();
                    // 如果选择父权限，自动选择所有子权限
                    if (value && Children != null && Children.Count > 0)
                    {
                        foreach (var child in Children)
                        {
                            child.IsSelected = true;
                        }
                    }
                    // 如果取消父权限，自动取消所有子权限
                    else if (!value && Children != null && Children.Count > 0)
                    {
                        foreach (var child in Children)
                        {
                            child.IsSelected = false;
                        }
                    }
                    // 通知父权限更新状态
                    if (_parent != null)
                    {
                        _parent.UpdateSelectionState();
                    }
                }
            }
        }

        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set => SetProperty(ref _isIndeterminate, value);
        }

        public ObservableCollection<PermissionItem> Children
        {
            get => _children;
            set
            {
                if (_children != null)
                {
                    foreach (var child in _children)
                    {
                        child.Parent = null;
                    }
                }
                SetProperty(ref _children, value);
                if (_children != null)
                {
                    foreach (var child in _children)
                    {
                        child.Parent = this;
                    }
                }
            }
        }

        public PermissionItem Parent
        {
            get => _parent;
            set => SetProperty(ref _parent, value);
        }

        public void UpdateSelectionState()
        {
            if (Children == null || Children.Count == 0)
                return;

            int selectedCount = 0;
            foreach (var child in Children)
            {
                if (child.IsSelected)
                    selectedCount++;
            }

            if (selectedCount == 0)
            {
                IsSelected = false;
                IsIndeterminate = false;
            }
            else if (selectedCount == Children.Count)
            {
                IsSelected = true;
                IsIndeterminate = false;
            }
            else
            {
                IsSelected = false;
                IsIndeterminate = true;
            }
        }

        private void UpdateIndeterminateState()
        {
            IsIndeterminate = false;
        }
    }
}

