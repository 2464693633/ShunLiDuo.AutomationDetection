using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public class DetectionLogRepository : IDetectionLogRepository
    {
        private readonly DatabaseContext _context;

        public DetectionLogRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<DetectionLogItem>> GetAllLogsAsync()
        {
            return await Task.Run(() =>
            {
                var logs = new List<DetectionLogItem>();
                string sql = @"SELECT Id, LogisticsBoxCode, RoomId, RoomName, Status, StartTime, EndTime, CreateTime, Remark 
                              FROM DetectionLogs 
                              ORDER BY CreateTime DESC";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        logs.Add(new DetectionLogItem
                        {
                            Id = reader.GetInt32(0),
                            LogisticsBoxCode = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            RoomId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                            RoomName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Status = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            StartTime = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                            EndTime = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                            CreateTime = reader.IsDBNull(7) ? DateTime.Now : reader.GetDateTime(7),
                            Remark = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                        });
                    }
                }
                return logs;
            });
        }

        public async Task<List<DetectionLogItem>> SearchLogsAsync(string keyword)
        {
            return await Task.Run(() =>
            {
                var logs = new List<DetectionLogItem>();
                string sql = @"SELECT Id, LogisticsBoxCode, RoomId, RoomName, Status, StartTime, EndTime, CreateTime, Remark 
                              FROM DetectionLogs 
                              WHERE LogisticsBoxCode LIKE @keyword OR RoomName LIKE @keyword
                              ORDER BY CreateTime DESC";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new DetectionLogItem
                            {
                                Id = reader.GetInt32(0),
                                LogisticsBoxCode = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                RoomId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                                RoomName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                Status = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                StartTime = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                EndTime = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                                CreateTime = reader.IsDBNull(7) ? DateTime.Now : reader.GetDateTime(7),
                                Remark = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                            });
                        }
                    }
                }
                return logs;
            });
        }

        public async Task<List<DetectionLogItem>> GetLogsByRoomIdAsync(int roomId)
        {
            return await Task.Run(() =>
            {
                var logs = new List<DetectionLogItem>();
                string sql = @"SELECT Id, LogisticsBoxCode, RoomId, RoomName, Status, StartTime, EndTime, CreateTime, Remark 
                              FROM DetectionLogs 
                              WHERE RoomId = @roomId
                              ORDER BY CreateTime DESC";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@roomId", roomId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new DetectionLogItem
                            {
                                Id = reader.GetInt32(0),
                                LogisticsBoxCode = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                RoomId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                                RoomName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                Status = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                StartTime = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                EndTime = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                                CreateTime = reader.IsDBNull(7) ? DateTime.Now : reader.GetDateTime(7),
                                Remark = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                            });
                        }
                    }
                }
                return logs;
            });
        }

        public async Task<List<DetectionLogItem>> GetLogsByBoxCodeAsync(string boxCode)
        {
            return await Task.Run(() =>
            {
                var logs = new List<DetectionLogItem>();
                string sql = @"SELECT Id, LogisticsBoxCode, RoomId, RoomName, Status, StartTime, EndTime, CreateTime, Remark 
                              FROM DetectionLogs 
                              WHERE LogisticsBoxCode = @boxCode
                              ORDER BY CreateTime DESC";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@boxCode", boxCode);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new DetectionLogItem
                            {
                                Id = reader.GetInt32(0),
                                LogisticsBoxCode = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                RoomId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                                RoomName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                Status = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                StartTime = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                EndTime = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                                CreateTime = reader.IsDBNull(7) ? DateTime.Now : reader.GetDateTime(7),
                                Remark = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                            });
                        }
                    }
                }
                return logs;
            });
        }

        public async Task<DetectionLogItem> GetLogByIdAsync(int id)
        {
            return await Task.Run(() =>
            {
                string sql = @"SELECT Id, LogisticsBoxCode, RoomId, RoomName, Status, StartTime, EndTime, CreateTime, Remark 
                              FROM DetectionLogs 
                              WHERE Id = @id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new DetectionLogItem
                            {
                                Id = reader.GetInt32(0),
                                LogisticsBoxCode = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                RoomId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                                RoomName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                Status = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                StartTime = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                EndTime = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                                CreateTime = reader.IsDBNull(7) ? DateTime.Now : reader.GetDateTime(7),
                                Remark = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                            };
                        }
                    }
                }
                return null;
            });
        }

        public async Task<int> InsertLogAsync(DetectionLogItem log)
        {
            if (log == null || string.IsNullOrWhiteSpace(log.LogisticsBoxCode))
            {
                return 0;
            }

            return await Task.Run(() =>
            {
                string sql = @"INSERT INTO DetectionLogs (LogisticsBoxCode, RoomId, RoomName, Status, StartTime, EndTime, CreateTime, Remark)
                              VALUES (@LogisticsBoxCode, @RoomId, @RoomName, @Status, @StartTime, @EndTime, @CreateTime, @Remark);
                              SELECT last_insert_rowid();";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@LogisticsBoxCode", log.LogisticsBoxCode ?? string.Empty);
                    command.Parameters.AddWithValue("@RoomId", (object)log.RoomId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@RoomName", log.RoomName ?? string.Empty);
                    command.Parameters.AddWithValue("@Status", log.Status ?? string.Empty);
                    command.Parameters.AddWithValue("@StartTime", (object)log.StartTime ?? DBNull.Value);
                    command.Parameters.AddWithValue("@EndTime", (object)log.EndTime ?? DBNull.Value);
                    command.Parameters.AddWithValue("@CreateTime", log.CreateTime);
                    command.Parameters.AddWithValue("@Remark", log.Remark ?? string.Empty);

                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            });
        }

        public async Task<bool> UpdateLogAsync(DetectionLogItem log)
        {
            if (log == null || log.Id <= 0)
            {
                return false;
            }

            return await Task.Run(() =>
            {
                string sql = @"UPDATE DetectionLogs 
                              SET LogisticsBoxCode = @LogisticsBoxCode, 
                                  RoomId = @RoomId, 
                                  RoomName = @RoomName, 
                                  Status = @Status, 
                                  StartTime = @StartTime, 
                                  EndTime = @EndTime, 
                                  Remark = @Remark
                              WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@Id", log.Id);
                    command.Parameters.AddWithValue("@LogisticsBoxCode", log.LogisticsBoxCode ?? string.Empty);
                    command.Parameters.AddWithValue("@RoomId", (object)log.RoomId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@RoomName", log.RoomName ?? string.Empty);
                    command.Parameters.AddWithValue("@Status", log.Status ?? string.Empty);
                    command.Parameters.AddWithValue("@StartTime", (object)log.StartTime ?? DBNull.Value);
                    command.Parameters.AddWithValue("@EndTime", (object)log.EndTime ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Remark", log.Remark ?? string.Empty);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            });
        }

        public async Task<bool> DeleteLogAsync(int id)
        {
            if (id <= 0)
            {
                return false;
            }

            return await Task.Run(() =>
            {
                string sql = "DELETE FROM DetectionLogs WHERE Id = @Id";

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

