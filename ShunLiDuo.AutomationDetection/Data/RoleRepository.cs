using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public class RoleRepository : IRoleRepository
    {
        private readonly DatabaseContext _context;

        public RoleRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<RoleItem>> GetAllRolesAsync()
        {
            return await Task.Run(() =>
            {
                var roles = new List<RoleItem>();
                string sql = "SELECT Id, RoleNo, RoleName, Remark, Permissions FROM Roles ORDER BY Id DESC";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(new RoleItem
                        {
                            Id = reader.GetInt32(0),
                            RoleNo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            RoleName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Remark = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Permissions = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                        });
                    }
                }
                return roles;
            });
        }

        public async Task<List<RoleItem>> SearchRolesAsync(string keyword)
        {
            return await Task.Run(() =>
            {
                var roles = new List<RoleItem>();
                string sql = @"SELECT Id, RoleNo, RoleName, Remark, Permissions 
                              FROM Roles 
                              WHERE RoleNo LIKE @keyword OR RoleName LIKE @keyword 
                              ORDER BY Id DESC";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            roles.Add(new RoleItem
                            {
                                Id = reader.GetInt32(0),
                                RoleNo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                RoleName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                Remark = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                Permissions = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                            });
                        }
                    }
                }
                return roles;
            });
        }

        public async Task<RoleItem> GetRoleByIdAsync(int id)
        {
            return await Task.Run(() =>
            {
                string sql = "SELECT Id, RoleNo, RoleName, Remark, Permissions FROM Roles WHERE Id = @id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new RoleItem
                            {
                                Id = reader.GetInt32(0),
                                RoleNo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                RoleName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                Remark = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                Permissions = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                            };
                        }
                    }
                }
                return null;
            });
        }

        public async Task<int> InsertRoleAsync(RoleItem role)
        {
            return await Task.Run(() =>
            {
                string sql = @"INSERT INTO Roles (RoleNo, RoleName, Remark, Permissions, CreateTime, UpdateTime)
                              VALUES (@RoleNo, @RoleName, @Remark, @Permissions, @CreateTime, @UpdateTime);
                              SELECT last_insert_rowid();";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@RoleNo", role.RoleNo ?? string.Empty);
                    command.Parameters.AddWithValue("@RoleName", role.RoleName ?? string.Empty);
                    command.Parameters.AddWithValue("@Remark", role.Remark ?? string.Empty);
                    command.Parameters.AddWithValue("@Permissions", role.Permissions ?? string.Empty);
                    command.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            });
        }

        public async Task<bool> UpdateRoleAsync(RoleItem role)
        {
            return await Task.Run(() =>
            {
                string sql = @"UPDATE Roles 
                              SET RoleNo = @RoleNo, RoleName = @RoleName, Remark = @Remark, 
                                  Permissions = @Permissions, UpdateTime = @UpdateTime
                              WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@RoleNo", role.RoleNo ?? string.Empty);
                    command.Parameters.AddWithValue("@RoleName", role.RoleName ?? string.Empty);
                    command.Parameters.AddWithValue("@Remark", role.Remark ?? string.Empty);
                    command.Parameters.AddWithValue("@Permissions", role.Permissions ?? string.Empty);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@Id", role.Id);

                    return command.ExecuteNonQuery() > 0;
                }
            });
        }

        public async Task<bool> DeleteRoleAsync(int id)
        {
            return await Task.Run(() =>
            {
                string sql = "DELETE FROM Roles WHERE Id = @id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    return command.ExecuteNonQuery() > 0;
                }
            });
        }
    }
}

