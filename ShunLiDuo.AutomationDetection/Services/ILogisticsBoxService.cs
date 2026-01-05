using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public interface ILogisticsBoxService
    {
        Task<List<LogisticsBoxItem>> GetAllBoxesAsync();
        Task<LogisticsBoxItem> GetBoxByIdAsync(int id);
        Task<bool> AddBoxAsync(LogisticsBoxItem box);
        Task<bool> UpdateBoxAsync(LogisticsBoxItem box);
        Task<bool> DeleteBoxAsync(int id);
    }
}

