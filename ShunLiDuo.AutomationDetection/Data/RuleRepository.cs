using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public class RuleRepository : IRuleRepository
    {
        private readonly DatabaseContext _context;

        public RuleRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<RuleItem>> GetAllRulesAsync()
        {
            return await Task.Run(() =>
            {
                var rules = new List<RuleItem>();
                string sql = "SELECT Id, RuleNo, RuleName, DetectionRooms, LogisticsBoxNos, Remark FROM Rules ORDER BY Id DESC";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rules.Add(new RuleItem
                        {
                            Id = reader.GetInt32(0),
                            RuleNo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            RuleName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            DetectionRooms = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            LogisticsBoxNos = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Remark = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            IsSelected = false
                        });
                    }
                }
                return rules;
            });
        }

        public async Task<List<RuleItem>> SearchRulesAsync(string keyword)
        {
            return await Task.Run(() =>
            {
                var rules = new List<RuleItem>();
                string sql = @"SELECT Id, RuleNo, RuleName, DetectionRooms, LogisticsBoxNos, Remark 
                              FROM Rules 
                              WHERE RuleNo LIKE @keyword OR RuleName LIKE @keyword 
                              ORDER BY Id DESC";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rules.Add(new RuleItem
                            {
                                Id = reader.GetInt32(0),
                                RuleNo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                RuleName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                DetectionRooms = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                LogisticsBoxNos = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                Remark = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                IsSelected = false
                            });
                        }
                    }
                }
                return rules;
            });
        }

        public async Task<RuleItem> GetRuleByIdAsync(int id)
        {
            return await Task.Run(() =>
            {
                string sql = "SELECT Id, RuleNo, RuleName, DetectionRooms, LogisticsBoxNos, Remark FROM Rules WHERE Id = @id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new RuleItem
                            {
                                Id = reader.GetInt32(0),
                                RuleNo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                RuleName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                DetectionRooms = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                LogisticsBoxNos = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                Remark = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                IsSelected = false
                            };
                        }
                    }
                }
                return null;
            });
        }

        public async Task<int> InsertRuleAsync(RuleItem rule)
        {
            return await Task.Run(() =>
            {
                string sql = @"INSERT INTO Rules (RuleNo, RuleName, DetectionRooms, LogisticsBoxNos, Remark, CreateTime, UpdateTime)
                              VALUES (@RuleNo, @RuleName, @DetectionRooms, @LogisticsBoxNos, @Remark, @CreateTime, @UpdateTime);
                              SELECT last_insert_rowid();";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@RuleNo", rule.RuleNo ?? string.Empty);
                    command.Parameters.AddWithValue("@RuleName", rule.RuleName ?? string.Empty);
                    command.Parameters.AddWithValue("@DetectionRooms", rule.DetectionRooms ?? string.Empty);
                    command.Parameters.AddWithValue("@LogisticsBoxNos", rule.LogisticsBoxNos ?? string.Empty);
                    command.Parameters.AddWithValue("@Remark", rule.Remark ?? string.Empty);
                    command.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            });
        }

        public async Task<bool> UpdateRuleAsync(RuleItem rule)
        {
            return await Task.Run(() =>
            {
                string sql = @"UPDATE Rules 
                              SET RuleNo = @RuleNo, RuleName = @RuleName, DetectionRooms = @DetectionRooms, 
                                  LogisticsBoxNos = @LogisticsBoxNos, Remark = @Remark, UpdateTime = @UpdateTime
                              WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@Id", rule.Id);
                    command.Parameters.AddWithValue("@RuleNo", rule.RuleNo ?? string.Empty);
                    command.Parameters.AddWithValue("@RuleName", rule.RuleName ?? string.Empty);
                    command.Parameters.AddWithValue("@DetectionRooms", rule.DetectionRooms ?? string.Empty);
                    command.Parameters.AddWithValue("@LogisticsBoxNos", rule.LogisticsBoxNos ?? string.Empty);
                    command.Parameters.AddWithValue("@Remark", rule.Remark ?? string.Empty);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            });
        }

        public async Task<bool> DeleteRuleAsync(int id)
        {
            return await Task.Run(() =>
            {
                string sql = "DELETE FROM Rules WHERE Id = @Id";

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

