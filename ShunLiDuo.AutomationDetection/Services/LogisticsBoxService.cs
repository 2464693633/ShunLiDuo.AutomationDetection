using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Data;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public class LogisticsBoxService : ILogisticsBoxService
    {
        private readonly ILogisticsBoxRepository _repository;

        public LogisticsBoxService(ILogisticsBoxRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<LogisticsBoxItem>> GetAllBoxesAsync()
        {
            return await _repository.GetAllBoxesAsync();
        }

        public async Task<LogisticsBoxItem> GetBoxByIdAsync(int id)
        {
            return await _repository.GetBoxByIdAsync(id);
        }

        public async Task<bool> AddBoxAsync(LogisticsBoxItem box)
        {
            if (box == null || string.IsNullOrWhiteSpace(box.BoxNo) || string.IsNullOrWhiteSpace(box.BoxName))
            {
                return false;
            }

            var id = await _repository.InsertBoxAsync(box);
            if (id > 0)
            {
                box.Id = id;
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateBoxAsync(LogisticsBoxItem box)
        {
            if (box == null || box.Id <= 0 || string.IsNullOrWhiteSpace(box.BoxNo) || string.IsNullOrWhiteSpace(box.BoxName))
            {
                return false;
            }

            return await _repository.UpdateBoxAsync(box);
        }

        public async Task<bool> DeleteBoxAsync(int id)
        {
            if (id <= 0)
            {
                return false;
            }

            return await _repository.DeleteBoxAsync(id);
        }
    }
}

