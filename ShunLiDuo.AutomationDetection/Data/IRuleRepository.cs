using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public interface IRuleRepository
    {
        Task<List<RuleItem>> GetAllRulesAsync();
        Task<List<RuleItem>> SearchRulesAsync(string keyword);
        Task<RuleItem> GetRuleByIdAsync(int id);
        Task<int> InsertRuleAsync(RuleItem rule);
        Task<bool> UpdateRuleAsync(RuleItem rule);
        Task<bool> DeleteRuleAsync(int id);
    }
}

