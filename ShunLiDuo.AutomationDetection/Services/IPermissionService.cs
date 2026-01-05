using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public interface IPermissionService
    {
        Task<List<PermissionItem>> GetPermissionTreeAsync();
    }
}

