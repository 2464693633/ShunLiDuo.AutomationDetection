using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Data;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public class DetectionLogService : IDetectionLogService
    {
        private readonly IDetectionLogRepository _repository;

        public DetectionLogService(IDetectionLogRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<DetectionLogItem>> GetAllLogsAsync()
        {
            return await _repository.GetAllLogsAsync();
        }

        public async Task<List<DetectionLogItem>> SearchLogsAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return await GetAllLogsAsync();
            }
            return await _repository.SearchLogsAsync(keyword);
        }

        public async Task<List<DetectionLogItem>> GetLogsByRoomIdAsync(int roomId)
        {
            return await _repository.GetLogsByRoomIdAsync(roomId);
        }

        public async Task<List<DetectionLogItem>> GetLogsByBoxCodeAsync(string boxCode)
        {
            return await _repository.GetLogsByBoxCodeAsync(boxCode);
        }

        public async Task<DetectionLogItem> GetLogByIdAsync(int id)
        {
            return await _repository.GetLogByIdAsync(id);
        }

        public async Task<bool> AddLogAsync(DetectionLogItem log)
        {
            if (log == null || string.IsNullOrWhiteSpace(log.LogisticsBoxCode) || string.IsNullOrWhiteSpace(log.Status))
            {
                return false;
            }

            if (log.CreateTime == default(DateTime))
            {
                log.CreateTime = System.DateTime.Now;
            }

            var id = await _repository.InsertLogAsync(log);
            if (id > 0)
            {
                log.Id = id;
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateLogAsync(DetectionLogItem log)
        {
            if (log == null || log.Id <= 0)
            {
                return false;
            }

            return await _repository.UpdateLogAsync(log);
        }

        public async Task<bool> UpdateWorkOrderNoAsync(int id, string workOrderNo)
        {
            if (id <= 0) return false;
            return await _repository.UpdateWorkOrderNoAsync(id, workOrderNo);
        }

        public async Task<bool> UpdateInspectorNameAsync(int id, string inspectorName)
        {
            if (id <= 0) return false;
            return await _repository.UpdateInspectorNameAsync(id, inspectorName);
        }

        public async Task<bool> UpdateRoomInfoAsync(int id, int roomId, string roomName, string status)
        {
            if (id <= 0) return false;
            return await _repository.UpdateRoomInfoAsync(id, roomId, roomName, status);
        }

        public async Task<bool> DeleteLogAsync(int id)
        {
            if (id <= 0)
            {
                return false;
            }

            return await _repository.DeleteLogAsync(id);
        }
    }
}

