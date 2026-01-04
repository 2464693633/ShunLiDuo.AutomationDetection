using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public class DetectionRoomRepository : IDetectionRoomRepository
    {
        private readonly DatabaseContext _context;

        public DetectionRoomRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<DetectionRoomItem>> GetAllRoomsAsync()
        {
            return await Task.Run(() =>
            {
                var rooms = new List<DetectionRoomItem>();
                string sql = "SELECT Id, RoomNo, RoomName, Remark FROM DetectionRooms ORDER BY Id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rooms.Add(new DetectionRoomItem
                        {
                            Id = reader.GetInt32(0),
                            RoomNo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            RoomName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Remark = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            IsSelected = false
                        });
                    }
                }
                return rooms;
            });
        }

        public async Task<int> InsertRoomAsync(DetectionRoomItem room)
        {
            return await Task.Run(() =>
            {
                string sql = @"INSERT INTO DetectionRooms (RoomNo, RoomName, Remark, CreateTime, UpdateTime)
                              VALUES (@RoomNo, @RoomName, @Remark, @CreateTime, @UpdateTime);
                              SELECT last_insert_rowid();";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@RoomNo", room.RoomNo ?? string.Empty);
                    command.Parameters.AddWithValue("@RoomName", room.RoomName ?? string.Empty);
                    command.Parameters.AddWithValue("@Remark", room.Remark ?? string.Empty);
                    command.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            });
        }
    }
}

