using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public interface IAccountRepository
    {
        Task<List<UserItem>> GetAllAccountsAsync();
        Task<List<UserItem>> SearchAccountsAsync(string keyword);
        Task<UserItem> GetAccountByIdAsync(int id);
        Task<UserItem> GetAccountByLoginAsync(string loginAccount, string password);
        Task<string> GetAccountPermissionsAsync(int accountId);
        Task<int> InsertAccountAsync(UserItem account);
        Task<bool> UpdateAccountAsync(UserItem account);
        Task<bool> DeleteAccountAsync(int id);
    }
}

