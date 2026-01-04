using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public interface IDetectionRoomRepository
    {
        Task<List<DetectionRoomItem>> GetAllRoomsAsync();
        Task<int> InsertRoomAsync(DetectionRoomItem room);
    }
}

