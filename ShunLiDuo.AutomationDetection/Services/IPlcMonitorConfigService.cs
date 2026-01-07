using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public interface IPlcMonitorConfigService
    {
        Task<List<PlcMonitorConfigItem>> GetAllConfigsAsync();
        Task<PlcMonitorConfigItem> GetConfigByIdAsync(int id);
        Task<PlcMonitorConfigItem> GetConfigByRoomIdAsync(int roomId);
        Task<int> CreateConfigAsync(PlcMonitorConfigItem config);
        Task<bool> UpdateConfigAsync(PlcMonitorConfigItem config);
        Task<bool> DeleteConfigAsync(int id);
    }
}

