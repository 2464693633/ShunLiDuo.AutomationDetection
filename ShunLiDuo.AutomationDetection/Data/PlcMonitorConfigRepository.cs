using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public class PlcMonitorConfigRepository : IPlcMonitorConfigRepository
    {
        private readonly DatabaseContext _context;

        public PlcMonitorConfigRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<PlcMonitorConfigItem>> GetAllConfigsAsync()
        {
            return await Task.Run(() =>
            {
                var configs = new List<PlcMonitorConfigItem>();
                string sql = @"
                    SELECT 
                        pmc.Id, pmc.RoomId, dr.RoomName, dr.RoomNo,
                        pmc.Cylinder1Name, pmc.Cylinder1ExtendAddress, pmc.Cylinder1RetractAddress,
                        pmc.Cylinder1ExtendFeedbackAddress, pmc.Cylinder1RetractFeedbackAddress, pmc.Cylinder1DataType,
                        pmc.Cylinder2Name, pmc.Cylinder2ExtendAddress, pmc.Cylinder2RetractAddress,
                        pmc.Cylinder2ExtendFeedbackAddress, pmc.Cylinder2RetractFeedbackAddress, pmc.Cylinder2DataType,
                        pmc.SensorName, pmc.SensorAddress, pmc.SensorDataType,
                        pmc.Remark, pmc.CreateTime, pmc.UpdateTime
                    FROM PlcMonitorConfig pmc
                    LEFT JOIN DetectionRooms dr ON pmc.RoomId = dr.Id
                    ORDER BY pmc.Id DESC";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        configs.Add(new PlcMonitorConfigItem
                        {
                            Id = reader.GetInt32(0),
                            RoomId = reader.GetInt32(1),
                            RoomName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            RoomNo = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Cylinder1Name = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Cylinder1ExtendAddress = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            Cylinder1RetractAddress = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                            Cylinder1ExtendFeedbackAddress = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                            Cylinder1RetractFeedbackAddress = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                            Cylinder1DataType = reader.IsDBNull(9) ? "Bool" : reader.GetString(9),
                            Cylinder2Name = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                            Cylinder2ExtendAddress = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                            Cylinder2RetractAddress = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                            Cylinder2ExtendFeedbackAddress = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                            Cylinder2RetractFeedbackAddress = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                            Cylinder2DataType = reader.IsDBNull(15) ? "Bool" : reader.GetString(15),
                            SensorName = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
                            SensorAddress = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
                            SensorDataType = reader.IsDBNull(18) ? "Bool" : reader.GetString(18),
                            Remark = reader.IsDBNull(19) ? string.Empty : reader.GetString(19),
                            CreateTime = reader.IsDBNull(20) ? DateTime.Now : reader.GetDateTime(20),
                            UpdateTime = reader.IsDBNull(21) ? (DateTime?)null : reader.GetDateTime(21)
                        });
                    }
                }
                return configs;
            });
        }

        public async Task<PlcMonitorConfigItem> GetConfigByIdAsync(int id)
        {
            return await Task.Run(() =>
            {
                string sql = @"
                    SELECT 
                        pmc.Id, pmc.RoomId, dr.RoomName, dr.RoomNo,
                        pmc.Cylinder1Name, pmc.Cylinder1ExtendAddress, pmc.Cylinder1RetractAddress,
                        pmc.Cylinder1ExtendFeedbackAddress, pmc.Cylinder1RetractFeedbackAddress, pmc.Cylinder1DataType,
                        pmc.Cylinder2Name, pmc.Cylinder2ExtendAddress, pmc.Cylinder2RetractAddress,
                        pmc.Cylinder2ExtendFeedbackAddress, pmc.Cylinder2RetractFeedbackAddress, pmc.Cylinder2DataType,
                        pmc.SensorName, pmc.SensorAddress, pmc.SensorDataType,
                        pmc.Remark, pmc.CreateTime, pmc.UpdateTime
                    FROM PlcMonitorConfig pmc
                    LEFT JOIN DetectionRooms dr ON pmc.RoomId = dr.Id
                    WHERE pmc.Id = @id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new PlcMonitorConfigItem
                            {
                                Id = reader.GetInt32(0),
                                RoomId = reader.GetInt32(1),
                                RoomName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                RoomNo = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                Cylinder1Name = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                Cylinder1ExtendAddress = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                Cylinder1RetractAddress = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                Cylinder1ExtendFeedbackAddress = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                                Cylinder1RetractFeedbackAddress = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                                Cylinder1DataType = reader.IsDBNull(9) ? "Bool" : reader.GetString(9),
                                Cylinder2Name = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                                Cylinder2ExtendAddress = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                                Cylinder2RetractAddress = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                                Cylinder2ExtendFeedbackAddress = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                                Cylinder2RetractFeedbackAddress = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                                Cylinder2DataType = reader.IsDBNull(15) ? "Bool" : reader.GetString(15),
                                SensorName = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
                                SensorAddress = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
                                SensorDataType = reader.IsDBNull(18) ? "Bool" : reader.GetString(18),
                                Remark = reader.IsDBNull(19) ? string.Empty : reader.GetString(19),
                                CreateTime = reader.IsDBNull(20) ? DateTime.Now : reader.GetDateTime(20),
                                UpdateTime = reader.IsDBNull(21) ? (DateTime?)null : reader.GetDateTime(21)
                            };
                        }
                    }
                }
                return null;
            });
        }

        public async Task<PlcMonitorConfigItem> GetConfigByRoomIdAsync(int roomId)
        {
            return await Task.Run(() =>
            {
                string sql = @"
                    SELECT 
                        pmc.Id, pmc.RoomId, dr.RoomName, dr.RoomNo,
                        pmc.Cylinder1Name, pmc.Cylinder1ExtendAddress, pmc.Cylinder1RetractAddress,
                        pmc.Cylinder1ExtendFeedbackAddress, pmc.Cylinder1RetractFeedbackAddress, pmc.Cylinder1DataType,
                        pmc.Cylinder2Name, pmc.Cylinder2ExtendAddress, pmc.Cylinder2RetractAddress,
                        pmc.Cylinder2ExtendFeedbackAddress, pmc.Cylinder2RetractFeedbackAddress, pmc.Cylinder2DataType,
                        pmc.SensorName, pmc.SensorAddress, pmc.SensorDataType,
                        pmc.Remark, pmc.CreateTime, pmc.UpdateTime
                    FROM PlcMonitorConfig pmc
                    LEFT JOIN DetectionRooms dr ON pmc.RoomId = dr.Id
                    WHERE pmc.RoomId = @roomId";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@roomId", roomId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new PlcMonitorConfigItem
                            {
                                Id = reader.GetInt32(0),
                                RoomId = reader.GetInt32(1),
                                RoomName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                RoomNo = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                Cylinder1Name = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                Cylinder1ExtendAddress = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                Cylinder1RetractAddress = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                Cylinder1ExtendFeedbackAddress = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                                Cylinder1RetractFeedbackAddress = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                                Cylinder1DataType = reader.IsDBNull(9) ? "Bool" : reader.GetString(9),
                                Cylinder2Name = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                                Cylinder2ExtendAddress = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                                Cylinder2RetractAddress = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                                Cylinder2ExtendFeedbackAddress = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                                Cylinder2RetractFeedbackAddress = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                                Cylinder2DataType = reader.IsDBNull(15) ? "Bool" : reader.GetString(15),
                                SensorName = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
                                SensorAddress = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
                                SensorDataType = reader.IsDBNull(18) ? "Bool" : reader.GetString(18),
                                Remark = reader.IsDBNull(19) ? string.Empty : reader.GetString(19),
                                CreateTime = reader.IsDBNull(20) ? DateTime.Now : reader.GetDateTime(20),
                                UpdateTime = reader.IsDBNull(21) ? (DateTime?)null : reader.GetDateTime(21)
                            };
                        }
                    }
                }
                return null;
            });
        }

        public async Task<int> InsertConfigAsync(PlcMonitorConfigItem config)
        {
            return await Task.Run(() =>
            {
                string sql = @"
                    INSERT INTO PlcMonitorConfig 
                    (RoomId, Cylinder1Name, Cylinder1ExtendAddress, Cylinder1RetractAddress,
                     Cylinder1ExtendFeedbackAddress, Cylinder1RetractFeedbackAddress, Cylinder1DataType,
                     Cylinder2Name, Cylinder2ExtendAddress, Cylinder2RetractAddress,
                     Cylinder2ExtendFeedbackAddress, Cylinder2RetractFeedbackAddress, Cylinder2DataType,
                     SensorName, SensorAddress, SensorDataType, Remark, CreateTime, UpdateTime)
                    VALUES 
                    (@RoomId, @Cylinder1Name, @Cylinder1ExtendAddress, @Cylinder1RetractAddress,
                     @Cylinder1ExtendFeedbackAddress, @Cylinder1RetractFeedbackAddress, @Cylinder1DataType,
                     @Cylinder2Name, @Cylinder2ExtendAddress, @Cylinder2RetractAddress,
                     @Cylinder2ExtendFeedbackAddress, @Cylinder2RetractFeedbackAddress, @Cylinder2DataType,
                     @SensorName, @SensorAddress, @SensorDataType, @Remark, @CreateTime, @UpdateTime);
                    SELECT last_insert_rowid();";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@RoomId", config.RoomId);
                    command.Parameters.AddWithValue("@Cylinder1Name", (object)config.Cylinder1Name ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder1ExtendAddress", (object)config.Cylinder1ExtendAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder1RetractAddress", (object)config.Cylinder1RetractAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder1ExtendFeedbackAddress", (object)config.Cylinder1ExtendFeedbackAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder1RetractFeedbackAddress", (object)config.Cylinder1RetractFeedbackAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder1DataType", (object)config.Cylinder1DataType ?? "Bool");
                    command.Parameters.AddWithValue("@Cylinder2Name", (object)config.Cylinder2Name ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder2ExtendAddress", (object)config.Cylinder2ExtendAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder2RetractAddress", (object)config.Cylinder2RetractAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder2ExtendFeedbackAddress", (object)config.Cylinder2ExtendFeedbackAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder2RetractFeedbackAddress", (object)config.Cylinder2RetractFeedbackAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder2DataType", (object)config.Cylinder2DataType ?? "Bool");
                    command.Parameters.AddWithValue("@SensorName", (object)config.SensorName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@SensorAddress", (object)config.SensorAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@SensorDataType", (object)config.SensorDataType ?? "Bool");
                    command.Parameters.AddWithValue("@Remark", (object)config.Remark ?? DBNull.Value);
                    command.Parameters.AddWithValue("@CreateTime", config.CreateTime);
                    command.Parameters.AddWithValue("@UpdateTime", (object)config.UpdateTime ?? DBNull.Value);

                    return Convert.ToInt32(command.ExecuteScalar());
                }
            });
        }

        public async Task<bool> UpdateConfigAsync(PlcMonitorConfigItem config)
        {
            return await Task.Run(() =>
            {
                string sql = @"
                    UPDATE PlcMonitorConfig SET
                        RoomId = @RoomId,
                        Cylinder1Name = @Cylinder1Name,
                        Cylinder1ExtendAddress = @Cylinder1ExtendAddress,
                        Cylinder1RetractAddress = @Cylinder1RetractAddress,
                        Cylinder1ExtendFeedbackAddress = @Cylinder1ExtendFeedbackAddress,
                        Cylinder1RetractFeedbackAddress = @Cylinder1RetractFeedbackAddress,
                        Cylinder1DataType = @Cylinder1DataType,
                        Cylinder2Name = @Cylinder2Name,
                        Cylinder2ExtendAddress = @Cylinder2ExtendAddress,
                        Cylinder2RetractAddress = @Cylinder2RetractAddress,
                        Cylinder2ExtendFeedbackAddress = @Cylinder2ExtendFeedbackAddress,
                        Cylinder2RetractFeedbackAddress = @Cylinder2RetractFeedbackAddress,
                        Cylinder2DataType = @Cylinder2DataType,
                        SensorName = @SensorName,
                        SensorAddress = @SensorAddress,
                        SensorDataType = @SensorDataType,
                        Remark = @Remark,
                        UpdateTime = @UpdateTime
                    WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@Id", config.Id);
                    command.Parameters.AddWithValue("@RoomId", config.RoomId);
                    command.Parameters.AddWithValue("@Cylinder1Name", (object)config.Cylinder1Name ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder1ExtendAddress", (object)config.Cylinder1ExtendAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder1RetractAddress", (object)config.Cylinder1RetractAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder1ExtendFeedbackAddress", (object)config.Cylinder1ExtendFeedbackAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder1RetractFeedbackAddress", (object)config.Cylinder1RetractFeedbackAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder1DataType", (object)config.Cylinder1DataType ?? "Bool");
                    command.Parameters.AddWithValue("@Cylinder2Name", (object)config.Cylinder2Name ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder2ExtendAddress", (object)config.Cylinder2ExtendAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder2RetractAddress", (object)config.Cylinder2RetractAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder2ExtendFeedbackAddress", (object)config.Cylinder2ExtendFeedbackAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder2RetractFeedbackAddress", (object)config.Cylinder2RetractFeedbackAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Cylinder2DataType", (object)config.Cylinder2DataType ?? "Bool");
                    command.Parameters.AddWithValue("@SensorName", (object)config.SensorName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@SensorAddress", (object)config.SensorAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("@SensorDataType", (object)config.SensorDataType ?? "Bool");
                    command.Parameters.AddWithValue("@Remark", (object)config.Remark ?? DBNull.Value);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    return command.ExecuteNonQuery() > 0;
                }
            });
        }

        public async Task<bool> DeleteConfigAsync(int id)
        {
            return await Task.Run(() =>
            {
                string sql = "DELETE FROM PlcMonitorConfig WHERE Id = @Id";
                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    return command.ExecuteNonQuery() > 0;
                }
            });
        }
    }
}

