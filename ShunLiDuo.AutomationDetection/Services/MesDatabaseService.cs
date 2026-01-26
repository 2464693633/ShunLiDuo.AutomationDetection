using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShunLiDuo.AutomationDetection.Services
{
    public class MesDatabaseService : IMesDatabaseService
    {
        // 数据库连接字符串（硬编码，如需配置界面可提取到配置文件或数据库）
        private const string ConnectionString = "Data Source=192.168.3.213;Initial Catalog=SDLMES22020530;User ID=query;Password=Aa123456!;Connect Timeout=3";

        public async Task<string> GetRoomNumberByWorkOrderAsync(string workOrderNo)
        {
            if (string.IsNullOrWhiteSpace(workOrderNo))
                return null;

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    // 查询语句
                    string query = "SELECT roomnumber FROM [dbo].[v_dt_pp_zl_sj_roomnumber] WHERE sjcode = @Code";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Code", workOrderNo);

                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            string rawRoomNumber = result.ToString();
                            
                            // 解析逻辑：提取第一个数字
                            // 例如 "2#房间" -> "2"
                            var match = Regex.Match(rawRoomNumber, @"\d+");
                            if (match.Success)
                            {
                                return match.Value;
                            }
                            
                            // 如果没有数字，返回原始字符串（或者此时应该返回null? 视业务容错而定，暂时返回原始值以便排查）
                            return rawRoomNumber;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录日志或处理连接异常
                System.Diagnostics.Debug.WriteLine($"[MES DB Error] 查询检测室失败: {ex.Message}");
                // 可以选择抛出异常或返回null，这里返回null表示未找到
                return null;
            }

            return null;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MES DB Error] 连接测试失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 上料扫码：更新物流盒编码和状态为B
        /// </summary>
        public async Task<bool> UpdateLoadingScanAsync(string workOrderNo, string boxCode)
        {
            if (string.IsNullOrWhiteSpace(workOrderNo) || string.IsNullOrWhiteSpace(boxCode))
            {
                System.Diagnostics.Debug.WriteLine($"[MES DB] 上料扫码更新失败: 送检单或物流盒编码为空");
                return false;
            }

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    // 更新语句：根据送检单编号更新物流盒编码和状态
                    string updateSql = @"UPDATE [dbo].[dt_pp_zl_sj_main] 
                                         SET hz_code = @BoxCode, flag = 'B' 
                                         WHERE sjcode = @WorkOrderNo";

                    using (var command = new SqlCommand(updateSql, connection))
                    {
                        command.Parameters.AddWithValue("@BoxCode", boxCode);
                        command.Parameters.AddWithValue("@WorkOrderNo", workOrderNo);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        
                        if (rowsAffected > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MES DB] 上料扫码更新成功: 送检单={workOrderNo}, 物流盒={boxCode}, flag=B, 影响行数={rowsAffected}");
                            return true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[MES DB] 上料扫码更新失败: 未找到送检单 {workOrderNo}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MES DB Error] 上料扫码更新异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 下料扫码：更新状态为D
        /// </summary>
        public async Task<bool> UpdateUnloadingScanAsync(string workOrderNo)
        {
            if (string.IsNullOrWhiteSpace(workOrderNo))
            {
                System.Diagnostics.Debug.WriteLine($"[MES DB] 下料扫码更新失败: 送检单编号为空");
                return false;
            }

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    // 更新语句：根据送检单编号更新状态为D
                    string updateSql = @"UPDATE [dbo].[dt_pp_zl_sj_main] 
                                         SET flag = 'D' 
                                         WHERE sjcode = @WorkOrderNo";

                    using (var command = new SqlCommand(updateSql, connection))
                    {
                        command.Parameters.AddWithValue("@WorkOrderNo", workOrderNo);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MES DB] 下料扫码更新成功: 送检单={workOrderNo}, flag=D, 影响行数={rowsAffected}");
                            return true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[MES DB] 下料扫码更新失败: 未找到送检单 {workOrderNo}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MES DB Error] 下料扫码更新异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 简易模式扫码：更新物流盒编码和状态为D（一步完成）
        /// </summary>
        public async Task<bool> UpdateSimpleModeScanAsync(string workOrderNo, string boxCode)
        {
            if (string.IsNullOrWhiteSpace(workOrderNo) || string.IsNullOrWhiteSpace(boxCode))
            {
                System.Diagnostics.Debug.WriteLine($"[MES DB] 简易模式更新失败: 送检单或物流盒编码为空");
                return false;
            }

            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    // 更新语句：简易模式直接写入物流盒编码和状态D
                    string updateSql = @"UPDATE [dbo].[dt_pp_zl_sj_main] 
                                         SET hz_code = @BoxCode, flag = 'D' 
                                         WHERE sjcode = @WorkOrderNo";

                    using (var command = new SqlCommand(updateSql, connection))
                    {
                        command.Parameters.AddWithValue("@BoxCode", boxCode);
                        command.Parameters.AddWithValue("@WorkOrderNo", workOrderNo);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        
                        if (rowsAffected > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MES DB] 简易模式更新成功: 送检单={workOrderNo}, 物流盒={boxCode}, flag=D, 影响行数={rowsAffected}");
                            return true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[MES DB] 简易模式更新失败: 未找到送检单 {workOrderNo}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MES DB Error] 简易模式更新异常: {ex.Message}");
                return false;
            }
        }
    }
}
