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
                string sql = "SELECT Id, AccountNo, LoginAccount, Password, Name, Gender, Phone, EmployeeNo, Role, Remark FROM Accounts ORDER BY Id DESC";

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
                            Role = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
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
                string sql = @"SELECT Id, AccountNo, LoginAccount, Password, Name, Gender, Phone, EmployeeNo, Role, Remark 
                              FROM Accounts 
                              WHERE LoginAccount LIKE @keyword OR Name LIKE @keyword 
                              ORDER BY Id DESC";

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
                                Role = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
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
                string sql = "SELECT Id, AccountNo, LoginAccount, Password, Name, Gender, Phone, EmployeeNo, Role, Remark FROM Accounts WHERE Id = @id";

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
                                Role = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
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
                string sql = @"INSERT INTO Accounts (AccountNo, LoginAccount, Password, Name, Gender, Phone, EmployeeNo, Role, Remark, CreateTime, UpdateTime)
                              VALUES (@AccountNo, @LoginAccount, @Password, @Name, @Gender, @Phone, @EmployeeNo, @Role, @Remark, @CreateTime, @UpdateTime);
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
                    command.Parameters.AddWithValue("@Role", account.Role ?? string.Empty);
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
                                  Role = @Role, Remark = @Remark, UpdateTime = @UpdateTime
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
                    command.Parameters.AddWithValue("@Role", account.Role ?? string.Empty);
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
    }
}

