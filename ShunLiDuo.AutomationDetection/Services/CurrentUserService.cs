using ShunLiDuo.AutomationDetection.Models;
using Prism.Mvvm;

namespace ShunLiDuo.AutomationDetection.Services
{
    public class CurrentUserService : BindableBase, ICurrentUserService
    {
        private UserItem _currentUser;

        public UserItem CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }
    }
}

