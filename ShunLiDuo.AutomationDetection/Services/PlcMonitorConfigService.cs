using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Data;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public class PlcMonitorConfigService : IPlcMonitorConfigService
    {
        private readonly IPlcMonitorConfigRepository _repository;

        public PlcMonitorConfigService(IPlcMonitorConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<PlcMonitorConfigItem>> GetAllConfigsAsync()
        {
            return await _repository.GetAllConfigsAsync();
        }

        public async Task<PlcMonitorConfigItem> GetConfigByIdAsync(int id)
        {
            return await _repository.GetConfigByIdAsync(id);
        }

        public async Task<PlcMonitorConfigItem> GetConfigByRoomIdAsync(int roomId)
        {
            return await _repository.GetConfigByRoomIdAsync(roomId);
        }

        public async Task<int> CreateConfigAsync(PlcMonitorConfigItem config)
        {
            return await _repository.InsertConfigAsync(config);
        }

        public async Task<bool> UpdateConfigAsync(PlcMonitorConfigItem config)
        {
            return await _repository.UpdateConfigAsync(config);
        }

        public async Task<bool> DeleteConfigAsync(int id)
        {
            return await _repository.DeleteConfigAsync(id);
        }
    }
}

