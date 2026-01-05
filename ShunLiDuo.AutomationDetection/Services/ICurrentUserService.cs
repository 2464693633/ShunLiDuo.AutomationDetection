using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public interface ICurrentUserService
    {
        UserItem CurrentUser { get; set; }
    }
}

