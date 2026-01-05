using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public interface IDetectionRoomRepository
    {
        Task<List<DetectionRoomItem>> GetAllRoomsAsync();
        Task<DetectionRoomItem> GetRoomByIdAsync(int id);
        Task<int> InsertRoomAsync(DetectionRoomItem room);
        Task<bool> UpdateRoomAsync(DetectionRoomItem room);
        Task<bool> DeleteRoomAsync(int id);
    }
}

