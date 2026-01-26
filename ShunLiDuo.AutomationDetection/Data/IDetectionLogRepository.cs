using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public interface IDetectionLogRepository
    {
        Task<List<DetectionLogItem>> GetAllLogsAsync();
        Task<List<DetectionLogItem>> SearchLogsAsync(string keyword);
        Task<List<DetectionLogItem>> GetLogsByRoomIdAsync(int roomId);
        Task<List<DetectionLogItem>> GetLogsByBoxCodeAsync(string boxCode);
        Task<DetectionLogItem> GetLogByIdAsync(int id);
        Task<int> InsertLogAsync(DetectionLogItem log);
        Task<bool> UpdateLogAsync(DetectionLogItem log);
        Task<bool> UpdateWorkOrderNoAsync(int id, string workOrderNo);

        Task<bool> UpdateInspectorNameAsync(int id, string inspectorName);
        Task<bool> UpdateRoomInfoAsync(int id, int roomId, string roomName, string status); // [新增]
        Task<bool> DeleteLogAsync(int id);
    }
}

