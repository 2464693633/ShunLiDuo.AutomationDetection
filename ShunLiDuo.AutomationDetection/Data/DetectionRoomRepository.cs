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
                              ScannerDataBits, ScannerStopBits, ScannerParity, ScannerIsEnabled,
                              Cylinder1ExtendAddress, Cylinder1RetractAddress,
                              Cylinder1ExtendFeedbackAddress, Cylinder1RetractFeedbackAddress, Cylinder1DataType,
                              Cylinder2ExtendAddress, Cylinder2RetractAddress,
                              Cylinder2ExtendFeedbackAddress, Cylinder2RetractFeedbackAddress, Cylinder2DataType,
                              SensorAddress, SensorDataType,
                              PushCylinderRetractTimeout, PushCylinderExtendTimeout,
                              BlockingCylinderRetractTimeout, BlockingCylinderExtendTimeout,
                              SensorDetectTimeout, PassageDelayTime, SensorConfirmDelayTime,
                              EnableBlockingCylinderRetractFeedback
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
                            Cylinder1ExtendAddress = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                            Cylinder1RetractAddress = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                            Cylinder1ExtendFeedbackAddress = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                            Cylinder1RetractFeedbackAddress = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                            Cylinder1DataType = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                            Cylinder2ExtendAddress = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
                            Cylinder2RetractAddress = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
                            Cylinder2ExtendFeedbackAddress = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
                            Cylinder2RetractFeedbackAddress = reader.IsDBNull(18) ? string.Empty : reader.GetString(18),
                            Cylinder2DataType = reader.IsDBNull(19) ? string.Empty : reader.GetString(19),
                            SensorAddress = reader.IsDBNull(20) ? string.Empty : reader.GetString(20),
                            SensorDataType = reader.IsDBNull(21) ? string.Empty : reader.GetString(21),
                            // 如果值为NULL或0，使用新默认值30000；否则使用数据库中的值
                            // 注意：索引从0开始，根据SELECT语句的顺序计算
                            PushCylinderRetractTimeout = reader.IsDBNull(22) || reader.GetInt32(22) == 0 ? 30000 : reader.GetInt32(22),
                            PushCylinderExtendTimeout = reader.IsDBNull(23) || reader.GetInt32(23) == 0 ? 30000 : reader.GetInt32(23),
                            BlockingCylinderRetractTimeout = reader.IsDBNull(24) || reader.GetInt32(24) == 0 ? 30000 : reader.GetInt32(24),
                            BlockingCylinderExtendTimeout = reader.IsDBNull(25) || reader.GetInt32(25) == 0 ? 30000 : reader.GetInt32(25),
                            SensorDetectTimeout = reader.IsDBNull(26) ? 15000 : reader.GetInt32(26),
                            PassageDelayTime = reader.IsDBNull(27) ? 5000 : reader.GetInt32(27),
                            SensorConfirmDelayTime = reader.IsDBNull(28) ? 3000 : reader.GetInt32(28),
                            EnableBlockingCylinderRetractFeedback = reader.IsDBNull(29) ? true : (reader.GetInt32(29) == 1),
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
                              ScannerDataBits, ScannerStopBits, ScannerParity, ScannerIsEnabled,
                              Cylinder1ExtendAddress, Cylinder1RetractAddress,
                              Cylinder1ExtendFeedbackAddress, Cylinder1RetractFeedbackAddress, Cylinder1DataType,
                              Cylinder2ExtendAddress, Cylinder2RetractAddress,
                              Cylinder2ExtendFeedbackAddress, Cylinder2RetractFeedbackAddress, Cylinder2DataType,
                              SensorAddress, SensorDataType,
                              PushCylinderRetractTimeout, PushCylinderExtendTimeout,
                              BlockingCylinderRetractTimeout, BlockingCylinderExtendTimeout,
                              SensorDetectTimeout, PassageDelayTime, SensorConfirmDelayTime,
                              EnableBlockingCylinderRetractFeedback
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
                                Cylinder1ExtendAddress = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                                Cylinder1RetractAddress = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                                Cylinder1ExtendFeedbackAddress = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                                Cylinder1RetractFeedbackAddress = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                                Cylinder1DataType = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                                Cylinder2ExtendAddress = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
                                Cylinder2RetractAddress = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
                                Cylinder2ExtendFeedbackAddress = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
                                Cylinder2RetractFeedbackAddress = reader.IsDBNull(18) ? string.Empty : reader.GetString(18),
                                Cylinder2DataType = reader.IsDBNull(19) ? string.Empty : reader.GetString(19),
                                SensorAddress = reader.IsDBNull(20) ? string.Empty : reader.GetString(20),
                                SensorDataType = reader.IsDBNull(21) ? string.Empty : reader.GetString(21),
                                // 如果值为NULL或0，使用新默认值30000；否则使用数据库中的值
                                // 注意：索引从0开始，根据SELECT语句的顺序计算
                                PushCylinderRetractTimeout = reader.IsDBNull(22) || reader.GetInt32(22) == 0 ? 30000 : reader.GetInt32(22),
                                PushCylinderExtendTimeout = reader.IsDBNull(23) || reader.GetInt32(23) == 0 ? 30000 : reader.GetInt32(23),
                                BlockingCylinderRetractTimeout = reader.IsDBNull(24) || reader.GetInt32(24) == 0 ? 30000 : reader.GetInt32(24),
                                BlockingCylinderExtendTimeout = reader.IsDBNull(25) || reader.GetInt32(25) == 0 ? 30000 : reader.GetInt32(25),
                                SensorDetectTimeout = reader.IsDBNull(26) ? 15000 : reader.GetInt32(26),
                                PassageDelayTime = reader.IsDBNull(27) ? 5000 : reader.GetInt32(27),
                                SensorConfirmDelayTime = reader.IsDBNull(28) ? 3000 : reader.GetInt32(28),
                                EnableBlockingCylinderRetractFeedback = reader.IsDBNull(29) ? true : (reader.GetInt32(29) == 1),
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
                              ScannerDataBits, ScannerStopBits, ScannerParity, ScannerIsEnabled,
                              Cylinder1ExtendAddress, Cylinder1RetractAddress,
                              Cylinder1ExtendFeedbackAddress, Cylinder1RetractFeedbackAddress, Cylinder1DataType,
                              Cylinder2ExtendAddress, Cylinder2RetractAddress,
                              Cylinder2ExtendFeedbackAddress, Cylinder2RetractFeedbackAddress, Cylinder2DataType,
                              SensorAddress, SensorDataType,
                              PushCylinderRetractTimeout, PushCylinderExtendTimeout,
                              BlockingCylinderRetractTimeout, BlockingCylinderExtendTimeout,
                              SensorDetectTimeout, PassageDelayTime, SensorConfirmDelayTime,
                              EnableBlockingCylinderRetractFeedback,
                              CreateTime, UpdateTime)
                              VALUES (@RoomNo, @RoomName, @Remark, @ScannerPortName, @ScannerBaudRate, 
                              @ScannerDataBits, @ScannerStopBits, @ScannerParity, @ScannerIsEnabled,
                              @Cylinder1ExtendAddress, @Cylinder1RetractAddress,
                              @Cylinder1ExtendFeedbackAddress, @Cylinder1RetractFeedbackAddress, @Cylinder1DataType,
                              @Cylinder2ExtendAddress, @Cylinder2RetractAddress,
                              @Cylinder2ExtendFeedbackAddress, @Cylinder2RetractFeedbackAddress, @Cylinder2DataType,
                              @SensorAddress, @SensorDataType,
                              @PushCylinderRetractTimeout, @PushCylinderExtendTimeout,
                              @BlockingCylinderRetractTimeout, @BlockingCylinderExtendTimeout,
                              @SensorDetectTimeout, @PassageDelayTime, @SensorConfirmDelayTime,
                              @EnableBlockingCylinderRetractFeedback,
                              @CreateTime, @UpdateTime);
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
                    command.Parameters.AddWithValue("@Cylinder1ExtendAddress", room.Cylinder1ExtendAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder1RetractAddress", room.Cylinder1RetractAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder1ExtendFeedbackAddress", room.Cylinder1ExtendFeedbackAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder1RetractFeedbackAddress", room.Cylinder1RetractFeedbackAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder1DataType", room.Cylinder1DataType ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder2ExtendAddress", room.Cylinder2ExtendAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder2RetractAddress", room.Cylinder2RetractAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder2ExtendFeedbackAddress", room.Cylinder2ExtendFeedbackAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder2RetractFeedbackAddress", room.Cylinder2RetractFeedbackAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder2DataType", room.Cylinder2DataType ?? string.Empty);
                    command.Parameters.AddWithValue("@SensorAddress", room.SensorAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@SensorDataType", room.SensorDataType ?? string.Empty);
                    command.Parameters.AddWithValue("@PushCylinderRetractTimeout", room.PushCylinderRetractTimeout);
                    command.Parameters.AddWithValue("@PushCylinderExtendTimeout", room.PushCylinderExtendTimeout);
                    command.Parameters.AddWithValue("@BlockingCylinderRetractTimeout", room.BlockingCylinderRetractTimeout);
                    command.Parameters.AddWithValue("@BlockingCylinderExtendTimeout", room.BlockingCylinderExtendTimeout);
                    command.Parameters.AddWithValue("@SensorDetectTimeout", room.SensorDetectTimeout);
                    command.Parameters.AddWithValue("@PassageDelayTime", room.PassageDelayTime);
                    command.Parameters.AddWithValue("@SensorConfirmDelayTime", room.SensorConfirmDelayTime);
                    command.Parameters.AddWithValue("@EnableBlockingCylinderRetractFeedback", room.EnableBlockingCylinderRetractFeedback ? 1 : 0);
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
                              Cylinder1ExtendAddress = @Cylinder1ExtendAddress,
                              Cylinder1RetractAddress = @Cylinder1RetractAddress,
                              Cylinder1ExtendFeedbackAddress = @Cylinder1ExtendFeedbackAddress,
                              Cylinder1RetractFeedbackAddress = @Cylinder1RetractFeedbackAddress,
                              Cylinder1DataType = @Cylinder1DataType,
                              Cylinder2ExtendAddress = @Cylinder2ExtendAddress,
                              Cylinder2RetractAddress = @Cylinder2RetractAddress,
                              Cylinder2ExtendFeedbackAddress = @Cylinder2ExtendFeedbackAddress,
                              Cylinder2RetractFeedbackAddress = @Cylinder2RetractFeedbackAddress,
                              Cylinder2DataType = @Cylinder2DataType,
                              SensorAddress = @SensorAddress,
                              SensorDataType = @SensorDataType,
                              PushCylinderRetractTimeout = @PushCylinderRetractTimeout,
                              PushCylinderExtendTimeout = @PushCylinderExtendTimeout,
                              BlockingCylinderRetractTimeout = @BlockingCylinderRetractTimeout,
                              BlockingCylinderExtendTimeout = @BlockingCylinderExtendTimeout,
                              SensorDetectTimeout = @SensorDetectTimeout,
                              PassageDelayTime = @PassageDelayTime,
                              SensorConfirmDelayTime = @SensorConfirmDelayTime,
                              EnableBlockingCylinderRetractFeedback = @EnableBlockingCylinderRetractFeedback,
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
                    command.Parameters.AddWithValue("@Cylinder1ExtendAddress", room.Cylinder1ExtendAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder1RetractAddress", room.Cylinder1RetractAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder1ExtendFeedbackAddress", room.Cylinder1ExtendFeedbackAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder1RetractFeedbackAddress", room.Cylinder1RetractFeedbackAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder1DataType", room.Cylinder1DataType ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder2ExtendAddress", room.Cylinder2ExtendAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder2RetractAddress", room.Cylinder2RetractAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder2ExtendFeedbackAddress", room.Cylinder2ExtendFeedbackAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder2RetractFeedbackAddress", room.Cylinder2RetractFeedbackAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@Cylinder2DataType", room.Cylinder2DataType ?? string.Empty);
                    command.Parameters.AddWithValue("@SensorAddress", room.SensorAddress ?? string.Empty);
                    command.Parameters.AddWithValue("@SensorDataType", room.SensorDataType ?? string.Empty);
                    command.Parameters.AddWithValue("@PushCylinderRetractTimeout", room.PushCylinderRetractTimeout);
                    command.Parameters.AddWithValue("@PushCylinderExtendTimeout", room.PushCylinderExtendTimeout);
                    command.Parameters.AddWithValue("@BlockingCylinderRetractTimeout", room.BlockingCylinderRetractTimeout);
                    command.Parameters.AddWithValue("@BlockingCylinderExtendTimeout", room.BlockingCylinderExtendTimeout);
                    command.Parameters.AddWithValue("@SensorDetectTimeout", room.SensorDetectTimeout);
                    command.Parameters.AddWithValue("@PassageDelayTime", room.PassageDelayTime);
                    command.Parameters.AddWithValue("@SensorConfirmDelayTime", room.SensorConfirmDelayTime);
                    command.Parameters.AddWithValue("@EnableBlockingCylinderRetractFeedback", room.EnableBlockingCylinderRetractFeedback ? 1 : 0);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    // 添加调试日志，检查参数值
                    System.Diagnostics.Debug.WriteLine($"[数据库更新] PushCylinderRetractTimeout: {room.PushCylinderRetractTimeout}");
                    System.Diagnostics.Debug.WriteLine($"[数据库更新] PushCylinderExtendTimeout: {room.PushCylinderExtendTimeout}");
                    System.Diagnostics.Debug.WriteLine($"[数据库更新] BlockingCylinderRetractTimeout: {room.BlockingCylinderRetractTimeout}");
                    System.Diagnostics.Debug.WriteLine($"[数据库更新] BlockingCylinderExtendTimeout: {room.BlockingCylinderExtendTimeout}");
                    System.Diagnostics.Debug.WriteLine($"[数据库更新] SensorDetectTimeout: {room.SensorDetectTimeout}");
                    System.Diagnostics.Debug.WriteLine($"[数据库更新] PassageDelayTime: {room.PassageDelayTime}");
                    System.Diagnostics.Debug.WriteLine($"[数据库更新] SensorConfirmDelayTime: {room.SensorConfirmDelayTime}");

                    int rowsAffected = command.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine($"[数据库更新] 受影响行数: {rowsAffected}");
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

