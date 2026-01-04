using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public interface ILogisticsBoxService
    {
        Task<List<LogisticsBoxItem>> GetAllBoxesAsync();
        Task<bool> AddBoxAsync(LogisticsBoxItem box);
    }
}

