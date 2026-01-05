using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public interface IAccountService
    {
        Task<List<UserItem>> GetAllAccountsAsync();
        Task<List<UserItem>> SearchAccountsAsync(string keyword);
        Task<UserItem> GetAccountByIdAsync(int id);
        Task<UserItem> ValidateLoginAsync(string loginAccount, string password);
        Task<string> GetAccountPermissionsAsync(int accountId);
        Task<bool> AddAccountAsync(UserItem account);
        Task<bool> UpdateAccountAsync(UserItem account);
        Task<bool> DeleteAccountAsync(int id);
    }
}

