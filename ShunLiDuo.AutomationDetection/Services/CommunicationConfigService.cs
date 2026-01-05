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
                    string sql = "SELECT IpAddress, CpuType, Rack, Slot, AutoConnect FROM CommunicationConfig LIMIT 1";
                    using (var command = new SQLiteCommand(sql, _dbContext.Connection))
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new CommunicationConfig
                            {
                                IpAddress = reader.IsDBNull(0) ? "192.168.1.100" : reader.GetString(0),
                                CpuType = reader.IsDBNull(1) ? CpuType.S71500 : (CpuType)reader.GetInt32(1),
                                Rack = reader.IsDBNull(2) ? (short)0 : (short)reader.GetInt32(2),
                                Slot = reader.IsDBNull(3) ? (short)1 : (short)reader.GetInt32(3),
                                AutoConnect = reader.IsDBNull(4) ? false : (reader.GetInt32(4) != 0)
                            };
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
                        INSERT INTO CommunicationConfig (IpAddress, CpuType, Rack, Slot, AutoConnect, UpdateTime)
                        VALUES (@IpAddress, @CpuType, @Rack, @Slot, @AutoConnect, CURRENT_TIMESTAMP)";
                    
                    using (var command = new SQLiteCommand(insertSql, _dbContext.Connection))
                    {
                        command.Parameters.AddWithValue("@IpAddress", config.IpAddress ?? "192.168.1.100");
                        command.Parameters.AddWithValue("@CpuType", (int)config.CpuType);
                        command.Parameters.AddWithValue("@Rack", config.Rack);
                        command.Parameters.AddWithValue("@Slot", config.Slot);
                        command.Parameters.AddWithValue("@AutoConnect", config.AutoConnect ? 1 : 0);
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

