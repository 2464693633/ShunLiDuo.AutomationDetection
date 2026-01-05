using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Data;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _repository;

        public AccountService(IAccountRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<UserItem>> GetAllAccountsAsync()
        {
            return await _repository.GetAllAccountsAsync();
        }

        public async Task<List<UserItem>> SearchAccountsAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return await GetAllAccountsAsync();
            }
            return await _repository.SearchAccountsAsync(keyword);
        }

        public async Task<UserItem> GetAccountByIdAsync(int id)
        {
            return await _repository.GetAccountByIdAsync(id);
        }

        public async Task<UserItem> ValidateLoginAsync(string loginAccount, string password)
        {
            if (string.IsNullOrWhiteSpace(loginAccount) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            return await _repository.GetAccountByLoginAsync(loginAccount, password);
        }

        public async Task<string> GetAccountPermissionsAsync(int accountId)
        {
            if (accountId <= 0)
            {
                return string.Empty;
            }

            return await _repository.GetAccountPermissionsAsync(accountId);
        }

        public async Task<bool> AddAccountAsync(UserItem account)
        {
            if (account == null || string.IsNullOrWhiteSpace(account.AccountNo) || 
                string.IsNullOrWhiteSpace(account.LoginAccount) || string.IsNullOrWhiteSpace(account.Password) ||
                string.IsNullOrWhiteSpace(account.Name))
            {
                return false;
            }

            var id = await _repository.InsertAccountAsync(account);
            if (id > 0)
            {
                account.Id = id;
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateAccountAsync(UserItem account)
        {
            if (account == null || account.Id <= 0 || string.IsNullOrWhiteSpace(account.AccountNo) || 
                string.IsNullOrWhiteSpace(account.LoginAccount) || string.IsNullOrWhiteSpace(account.Password) ||
                string.IsNullOrWhiteSpace(account.Name))
            {
                return false;
            }

            return await _repository.UpdateAccountAsync(account);
        }

        public async Task<bool> DeleteAccountAsync(int id)
        {
            if (id <= 0)
            {
                return false;
            }

            return await _repository.DeleteAccountAsync(id);
        }
    }
}

