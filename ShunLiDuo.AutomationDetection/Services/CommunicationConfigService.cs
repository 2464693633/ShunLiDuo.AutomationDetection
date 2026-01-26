using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Data;
using S7.Net;

namespace ShunLiDuo.AutomationDetection.Services
{
    public class CommunicationConfigService : ICommunicationConfigService
    {
        private readonly DatabaseContext _dbContext;

        public CommunicationConfigService(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CommunicationConfig> GetConfigAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    string sql = @"
                        SELECT 
                            IpAddress, CpuType, Rack, Slot, AutoConnect,
                            LoadingScannerPort, LoadingScannerBaudRate, LoadingScannerDataBits, LoadingScannerStopBits, LoadingScannerParity, LoadingScannerIsEnabled,
                            UnloadingScannerPort, UnloadingScannerBaudRate, UnloadingScannerDataBits, UnloadingScannerStopBits, UnloadingScannerParity, UnloadingScannerIsEnabled,
                            LoadingCylinderExtendDelay, LoadingCylinderRetractDelay, LoadingCylinderInterlockDelay, LoadingCylinderCooldown, LoadingCylinderLoopInterval,
                            UnloadingCylinderExtendDelay, UnloadingCylinderRetractDelay, UnloadingCylinderInterlockDelay, UnloadingCylinderCooldown, UnloadingCylinderLoopInterval,
                            Mode
                        FROM CommunicationConfig LIMIT 1";
                    
                    using (var command = new SQLiteCommand(sql, _dbContext.Connection))
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var config = new CommunicationConfig
                            {
                                IpAddress = reader.IsDBNull(0) ? "192.168.1.100" : reader.GetString(0),
                                CpuType = reader.IsDBNull(1) ? CpuType.S71500 : (CpuType)reader.GetInt32(1),
                                Rack = reader.IsDBNull(2) ? (short)0 : (short)reader.GetInt32(2),
                                Slot = reader.IsDBNull(3) ? (short)1 : (short)reader.GetInt32(3),
                                AutoConnect = reader.IsDBNull(4) ? false : (reader.GetInt32(4) != 0)
                            };
                            
                            // 检查是否有上料扫码枪配置字段 (索引 从 5 开始)
                            if (reader.FieldCount > 5)
                            {
                                try
                                {
                                    // 上料扫码枪
                                    config.LoadingScannerPort = reader.IsDBNull(5) ? null : reader.GetString(5);
                                    config.LoadingScannerBaudRate = reader.IsDBNull(6) ? 9600 : reader.GetInt32(6);
                                    config.LoadingScannerDataBits = reader.IsDBNull(7) ? 8 : reader.GetInt32(7);
                                    config.LoadingScannerStopBits = reader.IsDBNull(8) ? 1 : reader.GetInt32(8);
                                    config.LoadingScannerParity = reader.IsDBNull(9) ? "None" : reader.GetString(9);
                                    config.LoadingScannerIsEnabled = reader.IsDBNull(10) ? false : (reader.GetInt32(10) != 0);
                                    
                                    // 下料扫码枪
                                    config.UnloadingScannerPort = reader.IsDBNull(11) ? null : reader.GetString(11);
                                    config.UnloadingScannerBaudRate = reader.IsDBNull(12) ? 9600 : reader.GetInt32(12);
                                    config.UnloadingScannerDataBits = reader.IsDBNull(13) ? 8 : reader.GetInt32(13);
                                    config.UnloadingScannerStopBits = reader.IsDBNull(14) ? 1 : reader.GetInt32(14);
                                    config.UnloadingScannerParity = reader.IsDBNull(15) ? "None" : reader.GetString(15);
                                    config.UnloadingScannerIsEnabled = reader.IsDBNull(16) ? false : (reader.GetInt32(16) != 0);
                                    
                                    // 气缸参数（索引从17开始）
                                    if (reader.FieldCount > 17)
                                    {
                                        config.LoadingCylinderExtendDelay = reader.IsDBNull(17) ? 3000 : reader.GetInt32(17);
                                        config.LoadingCylinderRetractDelay = reader.IsDBNull(18) ? 2000 : reader.GetInt32(18);
                                        config.LoadingCylinderInterlockDelay = reader.IsDBNull(19) ? 50 : reader.GetInt32(19);
                                        config.LoadingCylinderCooldown = reader.IsDBNull(20) ? 500 : reader.GetInt32(20);
                                        config.LoadingCylinderLoopInterval = reader.IsDBNull(21) ? 50 : reader.GetInt32(21);
                                        
                                        config.UnloadingCylinderExtendDelay = reader.IsDBNull(22) ? 3000 : reader.GetInt32(22);
                                        config.UnloadingCylinderRetractDelay = reader.IsDBNull(23) ? 2000 : reader.GetInt32(23);
                                        config.UnloadingCylinderInterlockDelay = reader.IsDBNull(24) ? 50 : reader.GetInt32(24);
                                        config.UnloadingCylinderCooldown = reader.IsDBNull(25) ? 500 : reader.GetInt32(25);
                                        config.UnloadingCylinderLoopInterval = reader.IsDBNull(26) ? 50 : reader.GetInt32(26);
                                    }
                                    
                                    // 工作模式（索引27）
                                    if (reader.FieldCount > 27)
                                    {
                                        config.Mode = reader.IsDBNull(27) ? WorkMode.Standard : (WorkMode)reader.GetInt32(27);
                                    }
                                }
                                catch
                                {
                                    // 如果读取新字段失败（可能是旧数据库结构），忽略错误
                                }
                            }
                            
                            return config;
                        }
                    }
                }
                catch
                {
                    // 如果表不存在或查询失败，返回默认配置
                }

                // 返回默认配置
                return new CommunicationConfig();
            });
        }

        public async Task SaveConfigAsync(CommunicationConfig config)
        {
            await Task.Run(() =>
            {
                try
                {
                    // 先删除旧配置（只保留一条记录）
                    string deleteSql = "DELETE FROM CommunicationConfig";
                    using (var deleteCommand = new SQLiteCommand(deleteSql, _dbContext.Connection))
                    {
                        deleteCommand.ExecuteNonQuery();
                    }

                    // 插入新配置
                    string insertSql = @"
                        INSERT INTO CommunicationConfig (
                            IpAddress, CpuType, Rack, Slot, AutoConnect, UpdateTime,
                            LoadingScannerPort, LoadingScannerBaudRate, LoadingScannerDataBits, LoadingScannerStopBits, LoadingScannerParity, LoadingScannerIsEnabled,
                            UnloadingScannerPort, UnloadingScannerBaudRate, UnloadingScannerDataBits, UnloadingScannerStopBits, UnloadingScannerParity, UnloadingScannerIsEnabled,
                            LoadingCylinderExtendDelay, LoadingCylinderRetractDelay, LoadingCylinderInterlockDelay, LoadingCylinderCooldown, LoadingCylinderLoopInterval,
                            UnloadingCylinderExtendDelay, UnloadingCylinderRetractDelay, UnloadingCylinderInterlockDelay, UnloadingCylinderCooldown, UnloadingCylinderLoopInterval,
                            Mode
                        )
                        VALUES (
                            @IpAddress, @CpuType, @Rack, @Slot, @AutoConnect, CURRENT_TIMESTAMP,
                            @LoadingScannerPort, @LoadingScannerBaudRate, @LoadingScannerDataBits, @LoadingScannerStopBits, @LoadingScannerParity, @LoadingScannerIsEnabled,
                            @UnloadingScannerPort, @UnloadingScannerBaudRate, @UnloadingScannerDataBits, @UnloadingScannerStopBits, @UnloadingScannerParity, @UnloadingScannerIsEnabled,
                            @LoadingCylinderExtendDelay, @LoadingCylinderRetractDelay, @LoadingCylinderInterlockDelay, @LoadingCylinderCooldown, @LoadingCylinderLoopInterval,
                            @UnloadingCylinderExtendDelay, @UnloadingCylinderRetractDelay, @UnloadingCylinderInterlockDelay, @UnloadingCylinderCooldown, @UnloadingCylinderLoopInterval,
                            @Mode
                        )";
                    
                    using (var command = new SQLiteCommand(insertSql, _dbContext.Connection))
                    {
                        command.Parameters.AddWithValue("@IpAddress", config.IpAddress ?? "192.168.1.100");
                        command.Parameters.AddWithValue("@CpuType", (int)config.CpuType);
                        command.Parameters.AddWithValue("@Rack", config.Rack);
                        command.Parameters.AddWithValue("@Slot", config.Slot);
                        command.Parameters.AddWithValue("@AutoConnect", config.AutoConnect ? 1 : 0);
                        
                        // 上料扫码枪
                        command.Parameters.AddWithValue("@LoadingScannerPort", config.LoadingScannerPort ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@LoadingScannerBaudRate", config.LoadingScannerBaudRate);
                        command.Parameters.AddWithValue("@LoadingScannerDataBits", config.LoadingScannerDataBits);
                        command.Parameters.AddWithValue("@LoadingScannerStopBits", config.LoadingScannerStopBits);
                        command.Parameters.AddWithValue("@LoadingScannerParity", config.LoadingScannerParity ?? "None");
                        command.Parameters.AddWithValue("@LoadingScannerIsEnabled", config.LoadingScannerIsEnabled ? 1 : 0);
                        
                        // 下料扫码枪
                        command.Parameters.AddWithValue("@UnloadingScannerPort", config.UnloadingScannerPort ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@UnloadingScannerBaudRate", config.UnloadingScannerBaudRate);
                        command.Parameters.AddWithValue("@UnloadingScannerDataBits", config.UnloadingScannerDataBits);
                        command.Parameters.AddWithValue("@UnloadingScannerStopBits", config.UnloadingScannerStopBits);
                        command.Parameters.AddWithValue("@UnloadingScannerParity", config.UnloadingScannerParity ?? "None");
                        command.Parameters.AddWithValue("@UnloadingScannerIsEnabled", config.UnloadingScannerIsEnabled ? 1 : 0);
                        
                        // 气缸参数
                        command.Parameters.AddWithValue("@LoadingCylinderExtendDelay", config.LoadingCylinderExtendDelay);
                        command.Parameters.AddWithValue("@LoadingCylinderRetractDelay", config.LoadingCylinderRetractDelay);
                        command.Parameters.AddWithValue("@LoadingCylinderInterlockDelay", config.LoadingCylinderInterlockDelay);
                        command.Parameters.AddWithValue("@LoadingCylinderCooldown", config.LoadingCylinderCooldown);
                        command.Parameters.AddWithValue("@LoadingCylinderLoopInterval", config.LoadingCylinderLoopInterval);
                        
                        command.Parameters.AddWithValue("@UnloadingCylinderExtendDelay", config.UnloadingCylinderExtendDelay);
                        command.Parameters.AddWithValue("@UnloadingCylinderRetractDelay", config.UnloadingCylinderRetractDelay);
                        command.Parameters.AddWithValue("@UnloadingCylinderInterlockDelay", config.UnloadingCylinderInterlockDelay);
                        command.Parameters.AddWithValue("@UnloadingCylinderCooldown", config.UnloadingCylinderCooldown);
                        command.Parameters.AddWithValue("@UnloadingCylinderLoopInterval", config.UnloadingCylinderLoopInterval);
                        
                        // 工作模式
                        command.Parameters.AddWithValue("@Mode", (int)config.Mode);
                        
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"保存通讯配置失败: {ex.Message}", ex);
                }
            });
        }
    }
}

