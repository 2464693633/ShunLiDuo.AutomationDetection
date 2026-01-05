using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public class LogisticsBoxRepository : ILogisticsBoxRepository
    {
        private readonly DatabaseContext _context;

        public LogisticsBoxRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<LogisticsBoxItem>> GetAllBoxesAsync()
        {
            return await Task.Run(() =>
            {
                var boxes = new List<LogisticsBoxItem>();
                string sql = "SELECT Id, BoxNo, BoxName, Remark FROM LogisticsBoxes ORDER BY Id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        boxes.Add(new LogisticsBoxItem
                        {
                            Id = reader.GetInt32(0),
                            BoxNo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            BoxName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Remark = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            IsSelected = false
                        });
                    }
                }
                return boxes;
            });
        }

        public async Task<LogisticsBoxItem> GetBoxByIdAsync(int id)
        {
            return await Task.Run(() =>
            {
                string sql = "SELECT Id, BoxNo, BoxName, Remark FROM LogisticsBoxes WHERE Id = @id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new LogisticsBoxItem
                            {
                                Id = reader.GetInt32(0),
                                BoxNo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                BoxName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                Remark = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                IsSelected = false
                            };
                        }
                    }
                }
                return null;
            });
        }

        public async Task<int> InsertBoxAsync(LogisticsBoxItem box)
        {
            return await Task.Run(() =>
            {
                string sql = @"INSERT INTO LogisticsBoxes (BoxNo, BoxName, Remark, CreateTime, UpdateTime)
                              VALUES (@BoxNo, @BoxName, @Remark, @CreateTime, @UpdateTime);
                              SELECT last_insert_rowid();";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@BoxNo", box.BoxNo ?? string.Empty);
                    command.Parameters.AddWithValue("@BoxName", box.BoxName ?? string.Empty);
                    command.Parameters.AddWithValue("@Remark", box.Remark ?? string.Empty);
                    command.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            });
        }

        public async Task<bool> UpdateBoxAsync(LogisticsBoxItem box)
        {
            return await Task.Run(() =>
            {
                string sql = @"UPDATE LogisticsBoxes 
                              SET BoxNo = @BoxNo, BoxName = @BoxName, Remark = @Remark, UpdateTime = @UpdateTime
                              WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@Id", box.Id);
                    command.Parameters.AddWithValue("@BoxNo", box.BoxNo ?? string.Empty);
                    command.Parameters.AddWithValue("@BoxName", box.BoxName ?? string.Empty);
                    command.Parameters.AddWithValue("@Remark", box.Remark ?? string.Empty);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            });
        }

        public async Task<bool> DeleteBoxAsync(int id)
        {
            return await Task.Run(() =>
            {
                string sql = "DELETE FROM LogisticsBoxes WHERE Id = @Id";

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

