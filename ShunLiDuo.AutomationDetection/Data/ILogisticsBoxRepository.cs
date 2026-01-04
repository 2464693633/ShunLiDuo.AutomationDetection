using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public interface ILogisticsBoxRepository
    {
        Task<List<LogisticsBoxItem>> GetAllBoxesAsync();
        Task<int> InsertBoxAsync(LogisticsBoxItem box);
    }
}

