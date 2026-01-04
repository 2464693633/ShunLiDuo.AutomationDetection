using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public interface IDetectionRoomService
    {
        Task<List<DetectionRoomItem>> GetAllRoomsAsync();
        Task<bool> AddRoomAsync(DetectionRoomItem room);
    }
}

