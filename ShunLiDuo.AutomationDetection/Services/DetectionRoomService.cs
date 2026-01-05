using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Data;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public class DetectionRoomService : IDetectionRoomService
    {
        private readonly IDetectionRoomRepository _repository;

        public DetectionRoomService(IDetectionRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<DetectionRoomItem>> GetAllRoomsAsync()
        {
            return await _repository.GetAllRoomsAsync();
        }

        public async Task<DetectionRoomItem> GetRoomByIdAsync(int id)
        {
            return await _repository.GetRoomByIdAsync(id);
        }

        public async Task<bool> AddRoomAsync(DetectionRoomItem room)
        {
            if (room == null || string.IsNullOrWhiteSpace(room.RoomNo) || string.IsNullOrWhiteSpace(room.RoomName))
            {
                return false;
            }

            var id = await _repository.InsertRoomAsync(room);
            if (id > 0)
            {
                room.Id = id;
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateRoomAsync(DetectionRoomItem room)
        {
            if (room == null || room.Id <= 0 || string.IsNullOrWhiteSpace(room.RoomNo) || string.IsNullOrWhiteSpace(room.RoomName))
            {
                return false;
            }

            return await _repository.UpdateRoomAsync(room);
        }

        public async Task<bool> DeleteRoomAsync(int id)
        {
            if (id <= 0)
            {
                return false;
            }

            return await _repository.DeleteRoomAsync(id);
        }
    }
}

