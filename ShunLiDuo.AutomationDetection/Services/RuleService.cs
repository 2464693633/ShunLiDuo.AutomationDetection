using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Data;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public class RuleService : IRuleService
    {
        private readonly IRuleRepository _repository;

        public RuleService(IRuleRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RuleItem>> GetAllRulesAsync()
        {
            return await _repository.GetAllRulesAsync();
        }

        public async Task<List<RuleItem>> SearchRulesAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return await GetAllRulesAsync();
            }
            return await _repository.SearchRulesAsync(keyword);
        }

        public async Task<RuleItem> GetRuleByIdAsync(int id)
        {
            return await _repository.GetRuleByIdAsync(id);
        }

        public async Task<bool> AddRuleAsync(RuleItem rule)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.RuleNo) || string.IsNullOrWhiteSpace(rule.RuleName))
            {
                return false;
            }

            var id = await _repository.InsertRuleAsync(rule);
            if (id > 0)
            {
                rule.Id = id;
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateRuleAsync(RuleItem rule)
        {
            if (rule == null || rule.Id <= 0 || string.IsNullOrWhiteSpace(rule.RuleNo) || string.IsNullOrWhiteSpace(rule.RuleName))
            {
                return false;
            }

            return await _repository.UpdateRuleAsync(rule);
        }

        public async Task<bool> DeleteRuleAsync(int id)
        {
            if (id <= 0)
            {
                return false;
            }

            return await _repository.DeleteRuleAsync(id);
        }
    }
}

