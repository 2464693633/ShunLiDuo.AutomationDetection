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
                string sql = @"SELECT Id, RoomNo, RoomName, Remark, ScannerPortName, ScannerBaudRate, 
                              ScannerDataBits, ScannerStopBits, ScannerParity, ScannerIsEnabled 
                              FROM DetectionRooms ORDER BY Id";

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
                            ScannerPortName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            ScannerBaudRate = reader.IsDBNull(5) ? 9600 : reader.GetInt32(5),
                            ScannerDataBits = reader.IsDBNull(6) ? 8 : reader.GetInt32(6),
                            ScannerStopBits = reader.IsDBNull(7) ? 1 : reader.GetInt32(7),
                            ScannerParity = reader.IsDBNull(8) ? "None" : reader.GetString(8),
                            ScannerIsEnabled = reader.IsDBNull(9) ? false : reader.GetInt32(9) == 1,
                            IsSelected = false
                        });
                    }
                }
                return rooms;
            });
        }

        public async Task<DetectionRoomItem> GetRoomByIdAsync(int id)
        {
            return await Task.Run(() =>
            {
                string sql = @"SELECT Id, RoomNo, RoomName, Remark, ScannerPortName, ScannerBaudRate, 
                              ScannerDataBits, ScannerStopBits, ScannerParity, ScannerIsEnabled 
                              FROM DetectionRooms WHERE Id = @id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new DetectionRoomItem
                            {
                                Id = reader.GetInt32(0),
                                RoomNo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                RoomName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                Remark = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                ScannerPortName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                ScannerBaudRate = reader.IsDBNull(5) ? 9600 : reader.GetInt32(5),
                                ScannerDataBits = reader.IsDBNull(6) ? 8 : reader.GetInt32(6),
                                ScannerStopBits = reader.IsDBNull(7) ? 1 : reader.GetInt32(7),
                                ScannerParity = reader.IsDBNull(8) ? "None" : reader.GetString(8),
                                ScannerIsEnabled = reader.IsDBNull(9) ? false : reader.GetInt32(9) == 1,
                                IsSelected = false
                            };
                        }
                    }
                }
                return null;
            });
        }

        public async Task<int> InsertRoomAsync(DetectionRoomItem room)
        {
            return await Task.Run(() =>
            {
                string sql = @"INSERT INTO DetectionRooms (RoomNo, RoomName, Remark, ScannerPortName, ScannerBaudRate, 
                              ScannerDataBits, ScannerStopBits, ScannerParity, ScannerIsEnabled, CreateTime, UpdateTime)
                              VALUES (@RoomNo, @RoomName, @Remark, @ScannerPortName, @ScannerBaudRate, 
                              @ScannerDataBits, @ScannerStopBits, @ScannerParity, @ScannerIsEnabled, @CreateTime, @UpdateTime);
                              SELECT last_insert_rowid();";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@RoomNo", room.RoomNo ?? string.Empty);
                    command.Parameters.AddWithValue("@RoomName", room.RoomName ?? string.Empty);
                    command.Parameters.AddWithValue("@Remark", room.Remark ?? string.Empty);
                    command.Parameters.AddWithValue("@ScannerPortName", room.ScannerPortName ?? string.Empty);
                    command.Parameters.AddWithValue("@ScannerBaudRate", room.ScannerBaudRate);
                    command.Parameters.AddWithValue("@ScannerDataBits", room.ScannerDataBits);
                    command.Parameters.AddWithValue("@ScannerStopBits", room.ScannerStopBits);
                    command.Parameters.AddWithValue("@ScannerParity", room.ScannerParity ?? "None");
                    command.Parameters.AddWithValue("@ScannerIsEnabled", room.ScannerIsEnabled ? 1 : 0);
                    command.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            });
        }

        public async Task<bool> UpdateRoomAsync(DetectionRoomItem room)
        {
            return await Task.Run(() =>
            {
                string sql = @"UPDATE DetectionRooms 
                              SET RoomNo = @RoomNo, RoomName = @RoomName, Remark = @Remark, 
                              ScannerPortName = @ScannerPortName, ScannerBaudRate = @ScannerBaudRate,
                              ScannerDataBits = @ScannerDataBits, ScannerStopBits = @ScannerStopBits,
                              ScannerParity = @ScannerParity, ScannerIsEnabled = @ScannerIsEnabled,
                              UpdateTime = @UpdateTime
                              WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@Id", room.Id);
                    command.Parameters.AddWithValue("@RoomNo", room.RoomNo ?? string.Empty);
                    command.Parameters.AddWithValue("@RoomName", room.RoomName ?? string.Empty);
                    command.Parameters.AddWithValue("@Remark", room.Remark ?? string.Empty);
                    command.Parameters.AddWithValue("@ScannerPortName", room.ScannerPortName ?? string.Empty);
                    command.Parameters.AddWithValue("@ScannerBaudRate", room.ScannerBaudRate);
                    command.Parameters.AddWithValue("@ScannerDataBits", room.ScannerDataBits);
                    command.Parameters.AddWithValue("@ScannerStopBits", room.ScannerStopBits);
                    command.Parameters.AddWithValue("@ScannerParity", room.ScannerParity ?? "None");
                    command.Parameters.AddWithValue("@ScannerIsEnabled", room.ScannerIsEnabled ? 1 : 0);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            });
        }

        public async Task<bool> DeleteRoomAsync(int id)
        {
            return await Task.Run(() =>
            {
                string sql = "DELETE FROM DetectionRooms WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            });
        }
    }
}

