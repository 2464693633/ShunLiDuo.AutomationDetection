using System.Threading.Tasks;
using S7.Net;

namespace ShunLiDuo.AutomationDetection.Services
{
    /// <summary>
    /// 工作模式枚举
    /// </summary>
    public enum WorkMode
    {
        Standard = 0,  // 标准模式：带传送带、PLC控制、气缸控制
        Simple = 1     // 简易模式：仅数据记录，无传送带
    }
    
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
        
        // 上料扫码枪配置
        public string LoadingScannerPort { get; set; }
        public int LoadingScannerBaudRate { get; set; } = 9600;
        public int LoadingScannerDataBits { get; set; } = 8;
        public int LoadingScannerStopBits { get; set; } = 1;
        public string LoadingScannerParity { get; set; } = "None";
        public bool LoadingScannerIsEnabled { get; set; } = false;
        
        // 下料扫码枪配置
        public string UnloadingScannerPort { get; set; }
        public int UnloadingScannerBaudRate { get; set; } = 9600;
        public int UnloadingScannerDataBits { get; set; } = 8;
        public int UnloadingScannerStopBits { get; set; } = 1;
        public string UnloadingScannerParity { get; set; } = "None";
        public bool UnloadingScannerIsEnabled { get; set; } = false;
        
        // 上料弯道气缸参数
        public int LoadingCylinderExtendDelay { get; set; } = 3000;      // 伸出保持时间(ms)
        public int LoadingCylinderRetractDelay { get; set; } = 2000;     // 收缩保持时间(ms)
        public int LoadingCylinderInterlockDelay { get; set; } = 50;     // 互锁缓冲延时(ms)
        public int LoadingCylinderCooldown { get; set; } = 500;          // 冷却时间(ms)
        public int LoadingCylinderLoopInterval { get; set; } = 50;       // 循环间隔(ms)
        
        // 下料弯道气缸参数  
        public int UnloadingCylinderExtendDelay { get; set; } = 3000;
        public int UnloadingCylinderRetractDelay { get; set; } = 2000;
        public int UnloadingCylinderInterlockDelay { get; set; } = 50;
        public int UnloadingCylinderCooldown { get; set; } = 500;
        public int UnloadingCylinderLoopInterval { get; set; } = 50;
        
        // 工作模式
        public WorkMode Mode { get; set; } = WorkMode.Standard;
    }
}

