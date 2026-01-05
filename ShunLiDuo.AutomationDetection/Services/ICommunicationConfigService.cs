using System.Threading.Tasks;
using S7.Net;

namespace ShunLiDuo.AutomationDetection.Services
{
    public interface ICommunicationConfigService
    {
        Task<CommunicationConfig> GetConfigAsync();
        Task SaveConfigAsync(CommunicationConfig config);
    }

    public class CommunicationConfig
    {
        public string IpAddress { get; set; } = "192.168.1.100";
        public CpuType CpuType { get; set; } = CpuType.S71500;
        public short Rack { get; set; } = 0;
        public short Slot { get; set; } = 1;
        public bool AutoConnect { get; set; } = false;
    }
}

