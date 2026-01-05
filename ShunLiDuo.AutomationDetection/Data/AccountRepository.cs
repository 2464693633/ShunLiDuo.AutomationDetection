using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public class AccountRepository : IAccountRepository
    {
        private readonly DatabaseContext _context;

        public AccountRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<UserItem>> GetAllAccountsAsync()
        {
            return await Task.Run(() =>
            {
                var accounts = new List<UserItem>();
                string sql = @"SELECT a.Id, a.AccountNo, a.LoginAccount, a.Password, a.Name, a.Gender, a.Phone, a.EmployeeNo, a.RoleId, a.Remark, r.RoleName
                              FROM Accounts a
                              LEFT JOIN Roles r ON a.RoleId = r.Id
                              ORDER BY a.Id DESC";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        accounts.Add(new UserItem
                        {
                            Id = reader.GetInt32(0),
                            AccountNo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            LoginAccount = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Password = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Name = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Gender = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            Phone = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                            EmployeeNo = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                            RoleId = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                            Role = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                            Remark = reader.IsDBNull(9) ? string.Empty : reader.GetString(9)
                        });
                    }
                }
                return accounts;
            });
        }

        public async Task<List<UserItem>> SearchAccountsAsync(string keyword)
        {
            return await Task.Run(() =>
            {
                var accounts = new List<UserItem>();
                string sql = @"SELECT a.Id, a.AccountNo, a.LoginAccount, a.Password, a.Name, a.Gender, a.Phone, a.EmployeeNo, a.RoleId, a.Remark, r.RoleName
                              FROM Accounts a
                              LEFT JOIN Roles r ON a.RoleId = r.Id
                              WHERE a.LoginAccount LIKE @keyword OR a.Name LIKE @keyword 
                              ORDER BY a.Id DESC";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@keyword", $"%{keyword}%");
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            accounts.Add(new UserItem
                            {
                                Id = reader.GetInt32(0),
                                AccountNo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                LoginAccount = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                Password = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                Name = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                Gender = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                Phone = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                EmployeeNo = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                                RoleId = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                                Role = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                                Remark = reader.IsDBNull(9) ? string.Empty : reader.GetString(9)
                            });
                        }
                    }
                }
                return accounts;
            });
        }

        public async Task<UserItem> GetAccountByIdAsync(int id)
        {
            return await Task.Run(() =>
            {
                string sql = @"SELECT a.Id, a.AccountNo, a.LoginAccount, a.Password, a.Name, a.Gender, a.Phone, a.EmployeeNo, a.RoleId, a.Remark, r.RoleName
                              FROM Accounts a
                              LEFT JOIN Roles r ON a.RoleId = r.Id
                              WHERE a.Id = @id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new UserItem
                            {
                                Id = reader.GetInt32(0),
                                AccountNo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                LoginAccount = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                Password = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                Name = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                Gender = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                Phone = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                EmployeeNo = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                                RoleId = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                                Role = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                                Remark = reader.IsDBNull(9) ? string.Empty : reader.GetString(9)
                            };
                        }
                    }
                }
                return null;
            });
        }

        public async Task<UserItem> GetAccountByLoginAsync(string loginAccount, string password)
        {
            return await Task.Run(() =>
            {
                string sql = @"SELECT a.Id, a.AccountNo, a.LoginAccount, a.Password, a.Name, a.Gender, a.Phone, a.EmployeeNo, a.RoleId, a.Remark, r.RoleName
                              FROM Accounts a
                              LEFT JOIN Roles r ON a.RoleId = r.Id
                              WHERE a.LoginAccount = @LoginAccount AND a.Password = @Password";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@LoginAccount", loginAccount ?? string.Empty);
                    command.Parameters.AddWithValue("@Password", password ?? string.Empty);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new UserItem
                            {
                                Id = reader.GetInt32(0),
                                AccountNo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                LoginAccount = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                Password = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                Name = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                Gender = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                Phone = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                EmployeeNo = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                                RoleId = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                                Role = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                                Remark = reader.IsDBNull(9) ? string.Empty : reader.GetString(9)
                            };
                        }
                    }
                }
                return null;
            });
        }

        public async Task<int> InsertAccountAsync(UserItem account)
        {
            return await Task.Run(() =>
            {
                string sql = @"INSERT INTO Accounts (AccountNo, LoginAccount, Password, Name, Gender, Phone, EmployeeNo, RoleId, Remark, CreateTime, UpdateTime)
                              VALUES (@AccountNo, @LoginAccount, @Password, @Name, @Gender, @Phone, @EmployeeNo, @RoleId, @Remark, @CreateTime, @UpdateTime);
                              SELECT last_insert_rowid();";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@AccountNo", account.AccountNo ?? string.Empty);
                    command.Parameters.AddWithValue("@LoginAccount", account.LoginAccount ?? string.Empty);
                    command.Parameters.AddWithValue("@Password", account.Password ?? string.Empty);
                    command.Parameters.AddWithValue("@Name", account.Name ?? string.Empty);
                    command.Parameters.AddWithValue("@Gender", account.Gender ?? string.Empty);
                    command.Parameters.AddWithValue("@Phone", account.Phone ?? string.Empty);
                    command.Parameters.AddWithValue("@EmployeeNo", account.EmployeeNo ?? string.Empty);
                    command.Parameters.AddWithValue("@RoleId", account.RoleId.HasValue ? (object)account.RoleId.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@Remark", account.Remark ?? string.Empty);
                    command.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            });
        }

        public async Task<bool> UpdateAccountAsync(UserItem account)
        {
            return await Task.Run(() =>
            {
                string sql = @"UPDATE Accounts 
                              SET AccountNo = @AccountNo, LoginAccount = @LoginAccount, Password = @Password, 
                                  Name = @Name, Gender = @Gender, Phone = @Phone, EmployeeNo = @EmployeeNo, 
                                  RoleId = @RoleId, Remark = @Remark, UpdateTime = @UpdateTime
                              WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@AccountNo", account.AccountNo ?? string.Empty);
                    command.Parameters.AddWithValue("@LoginAccount", account.LoginAccount ?? string.Empty);
                    command.Parameters.AddWithValue("@Password", account.Password ?? string.Empty);
                    command.Parameters.AddWithValue("@Name", account.Name ?? string.Empty);
                    command.Parameters.AddWithValue("@Gender", account.Gender ?? string.Empty);
                    command.Parameters.AddWithValue("@Phone", account.Phone ?? string.Empty);
                    command.Parameters.AddWithValue("@EmployeeNo", account.EmployeeNo ?? string.Empty);
                    command.Parameters.AddWithValue("@RoleId", account.RoleId.HasValue ? (object)account.RoleId.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@Remark", account.Remark ?? string.Empty);
                    command.Parameters.AddWithValue("@UpdateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@Id", account.Id);

                    return command.ExecuteNonQuery() > 0;
                }
            });
        }

        public async Task<bool> DeleteAccountAsync(int id)
        {
            return await Task.Run(() =>
            {
                string sql = "DELETE FROM Accounts WHERE Id = @id";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    return command.ExecuteNonQuery() > 0;
                }
            });
        }

        public async Task<string> GetAccountPermissionsAsync(int accountId)
        {
            return await Task.Run(() =>
            {
                string sql = @"SELECT r.Permissions 
                              FROM Accounts a
                              INNER JOIN Roles r ON a.RoleId = r.Id
                              WHERE a.Id = @AccountId";

                using (var command = new SQLiteCommand(sql, _context.Connection))
                {
                    command.Parameters.AddWithValue("@AccountId", accountId);
                    var result = command.ExecuteScalar();
                    return result != null && result != DBNull.Value ? result.ToString() : string.Empty;
                }
            });
        }
    }
}

