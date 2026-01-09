using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public class AlarmRecordRepository : IAlarmRecordRepository
    {
        private readonly DatabaseContext _dbContext;

        public AlarmRecordRepository()
        {
            _dbContext = new DatabaseContext();
        }

        public async Task<List<AlarmRecord>> GetAllAlarmsAsync()
        {
            return await Task.Run(() =>
            {
                var alarms = new List<AlarmRecord>();
                using (var command = _dbContext.Connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, AlarmCode, AlarmTitle, AlarmMessage,
                               RoomId, RoomName, DeviceName, Status, CreateTime, HandleTime, Handler, Remark
                        FROM AlarmRecords
                        ORDER BY CreateTime DESC";
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            alarms.Add(MapReaderToAlarm(reader));
                        }
                    }
                }
                return alarms;
            });
        }

        public async Task<List<AlarmRecord>> SearchAlarmsAsync(string keyword, int? roomId, DateTime? startTime, DateTime? endTime)
        {
            return await Task.Run(() =>
            {
                var alarms = new List<AlarmRecord>();
                var conditions = new List<string>();
                var parameters = new List<SQLiteParameter>();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    conditions.Add("(AlarmTitle LIKE @keyword OR AlarmMessage LIKE @keyword OR AlarmCode LIKE @keyword)");
                    parameters.Add(new SQLiteParameter("@keyword", $"%{keyword}%"));
                }

                if (roomId.HasValue)
                {
                    conditions.Add("RoomId = @roomId");
                    parameters.Add(new SQLiteParameter("@roomId", roomId.Value));
                }

                if (startTime.HasValue)
                {
                    conditions.Add("CreateTime >= @startTime");
                    parameters.Add(new SQLiteParameter("@startTime", startTime.Value));
                }

                if (endTime.HasValue)
                {
                    conditions.Add("CreateTime <= @endTime");
                    parameters.Add(new SQLiteParameter("@endTime", endTime.Value.AddDays(1)));
                }

                string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

                using (var command = _dbContext.Connection.CreateCommand())
                {
                    command.CommandText = $@"
                        SELECT Id, AlarmCode, AlarmTitle, AlarmMessage,
                               RoomId, RoomName, DeviceName, Status, CreateTime, HandleTime, Handler, Remark
                        FROM AlarmRecords
                        {whereClause}
                        ORDER BY CreateTime DESC";
                    
                    command.Parameters.AddRange(parameters.ToArray());
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            alarms.Add(MapReaderToAlarm(reader));
                        }
                    }
                }
                return alarms;
            });
        }

        public async Task<List<AlarmRecord>> GetUnhandledAlarmsAsync()
        {
            return await Task.Run(() =>
            {
                var alarms = new List<AlarmRecord>();
                using (var command = _dbContext.Connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, AlarmCode, AlarmTitle, AlarmMessage,
                               RoomId, RoomName, DeviceName, Status, CreateTime, HandleTime, Handler, Remark
                        FROM AlarmRecords
                        WHERE Status = '未处理'
                        ORDER BY CreateTime DESC";
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            alarms.Add(MapReaderToAlarm(reader));
                        }
                    }
                }
                return alarms;
            });
        }

        public async Task<AlarmRecord> GetAlarmByIdAsync(int id)
        {
            return await Task.Run(() =>
            {
                using (var command = _dbContext.Connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, AlarmCode, AlarmTitle, AlarmMessage,
                               RoomId, RoomName, DeviceName, Status, CreateTime, HandleTime, Handler, Remark
                        FROM AlarmRecords
                        WHERE Id = @id";
                    command.Parameters.Add(new SQLiteParameter("@id", id));
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapReaderToAlarm(reader);
                        }
                    }
                }
                return null;
            });
        }

        public async Task<AlarmRecord> GetAlarmByCodeAsync(string alarmCode)
        {
            return await Task.Run(() =>
            {
                using (var command = _dbContext.Connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, AlarmCode, AlarmTitle, AlarmMessage,
                               RoomId, RoomName, DeviceName, Status, CreateTime, HandleTime, Handler, Remark
                        FROM AlarmRecords
                        WHERE AlarmCode = @alarmCode";
                    command.Parameters.Add(new SQLiteParameter("@alarmCode", alarmCode));
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapReaderToAlarm(reader);
                        }
                    }
                }
                return null;
            });
        }

        public async Task<int> InsertAlarmAsync(AlarmRecord alarm)
        {
            return await Task.Run(() =>
            {
                using (var command = _dbContext.Connection.CreateCommand())
                {
                    // 生成报警编号
                    if (string.IsNullOrWhiteSpace(alarm.AlarmCode))
                    {
                        alarm.AlarmCode = $"AL{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
                    }

                    command.CommandText = @"
                        INSERT INTO AlarmRecords 
                        (AlarmCode, AlarmTitle, AlarmMessage, RoomId, RoomName, DeviceName, Status, CreateTime, Remark)
                        VALUES 
                        (@AlarmCode, @AlarmTitle, @AlarmMessage, @RoomId, @RoomName, @DeviceName, @Status, @CreateTime, @Remark);
                        SELECT last_insert_rowid();";
                    
                    command.Parameters.Add(new SQLiteParameter("@AlarmCode", alarm.AlarmCode));
                    command.Parameters.Add(new SQLiteParameter("@AlarmTitle", alarm.AlarmTitle));
                    command.Parameters.Add(new SQLiteParameter("@AlarmMessage", alarm.AlarmMessage ?? ""));
                    command.Parameters.Add(new SQLiteParameter("@RoomId", alarm.RoomId ?? (object)DBNull.Value));
                    command.Parameters.Add(new SQLiteParameter("@RoomName", alarm.RoomName ?? ""));
                    command.Parameters.Add(new SQLiteParameter("@DeviceName", alarm.DeviceName ?? ""));
                    command.Parameters.Add(new SQLiteParameter("@Status", alarm.Status ?? "未处理"));
                    command.Parameters.Add(new SQLiteParameter("@CreateTime", alarm.CreateTime));
                    command.Parameters.Add(new SQLiteParameter("@Remark", alarm.Remark ?? ""));
                    
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            });
        }

        public async Task<bool> UpdateAlarmAsync(AlarmRecord alarm)
        {
            return await Task.Run(() =>
            {
                using (var command = _dbContext.Connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE AlarmRecords 
                        SET AlarmTitle = @AlarmTitle,
                            AlarmMessage = @AlarmMessage, RoomId = @RoomId, RoomName = @RoomName,
                            DeviceName = @DeviceName, Status = @Status, HandleTime = @HandleTime,
                            Handler = @Handler, Remark = @Remark
                        WHERE Id = @Id";
                    
                    command.Parameters.Add(new SQLiteParameter("@Id", alarm.Id));
                    command.Parameters.Add(new SQLiteParameter("@AlarmTitle", alarm.AlarmTitle));
                    command.Parameters.Add(new SQLiteParameter("@AlarmMessage", alarm.AlarmMessage ?? ""));
                    command.Parameters.Add(new SQLiteParameter("@RoomId", alarm.RoomId ?? (object)DBNull.Value));
                    command.Parameters.Add(new SQLiteParameter("@RoomName", alarm.RoomName ?? ""));
                    command.Parameters.Add(new SQLiteParameter("@DeviceName", alarm.DeviceName ?? ""));
                    command.Parameters.Add(new SQLiteParameter("@Status", alarm.Status));
                    command.Parameters.Add(new SQLiteParameter("@HandleTime", alarm.HandleTime ?? (object)DBNull.Value));
                    command.Parameters.Add(new SQLiteParameter("@Handler", alarm.Handler ?? ""));
                    command.Parameters.Add(new SQLiteParameter("@Remark", alarm.Remark ?? ""));
                    
                    return command.ExecuteNonQuery() > 0;
                }
            });
        }

        public async Task<bool> DeleteAlarmAsync(int id)
        {
            return await Task.Run(() =>
            {
                using (var command = _dbContext.Connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM AlarmRecords WHERE Id = @Id";
                    command.Parameters.Add(new SQLiteParameter("@Id", id));
                    return command.ExecuteNonQuery() > 0;
                }
            });
        }

        public async Task<bool> HandleAlarmAsync(int id, string handler, string remark)
        {
            return await Task.Run(() =>
            {
                using (var command = _dbContext.Connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE AlarmRecords 
                        SET Status = '已处理', HandleTime = @HandleTime, Handler = @Handler, Remark = @Remark
                        WHERE Id = @Id";
                    
                    command.Parameters.Add(new SQLiteParameter("@Id", id));
                    command.Parameters.Add(new SQLiteParameter("@HandleTime", DateTime.Now));
                    command.Parameters.Add(new SQLiteParameter("@Handler", handler ?? ""));
                    command.Parameters.Add(new SQLiteParameter("@Remark", remark ?? ""));
                    
                    return command.ExecuteNonQuery() > 0;
                }
            });
        }

        private AlarmRecord MapReaderToAlarm(SQLiteDataReader reader)
        {
            return new AlarmRecord
            {
                Id = reader.GetInt32(0),
                AlarmCode = reader.IsDBNull(1) ? null : reader.GetString(1),
                AlarmTitle = reader.IsDBNull(2) ? null : reader.GetString(2),
                AlarmMessage = reader.IsDBNull(3) ? null : reader.GetString(3),
                RoomId = reader.IsDBNull(4) ? null : (int?)reader.GetInt32(4),
                RoomName = reader.IsDBNull(5) ? null : reader.GetString(5),
                DeviceName = reader.IsDBNull(6) ? null : reader.GetString(6),
                Status = reader.IsDBNull(7) ? "未处理" : reader.GetString(7),
                CreateTime = reader.IsDBNull(8) ? DateTime.Now : reader.GetDateTime(8),
                HandleTime = reader.IsDBNull(9) ? null : (DateTime?)reader.GetDateTime(9),
                Handler = reader.IsDBNull(10) ? null : reader.GetString(10),
                Remark = reader.IsDBNull(11) ? null : reader.GetString(11)
            };
        }
    }
}

