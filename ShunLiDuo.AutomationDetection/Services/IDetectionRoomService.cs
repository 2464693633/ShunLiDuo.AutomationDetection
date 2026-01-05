using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public interface IDetectionRoomService
    {
        Task<List<DetectionRoomItem>> GetAllRoomsAsync();
        Task<DetectionRoomItem> GetRoomByIdAsync(int id);
        Task<bool> AddRoomAsync(DetectionRoomItem room);
        Task<bool> UpdateRoomAsync(DetectionRoomItem room);
        Task<bool> DeleteRoomAsync(int id);
    }
}

