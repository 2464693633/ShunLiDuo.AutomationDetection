using System;
using System.Threading.Tasks;
using S7.Net;

namespace ShunLiDuo.AutomationDetection.Services
{
    public interface IS7CommunicationService
    {
        bool IsConnected { get; }
        string ConnectionStatus { get; }
        
        event EventHandler<bool> ConnectionStatusChanged;
        
        Task<bool> ConnectAsync(string ipAddress, CpuType cpuType, short rack, short slot);
        Task DisconnectAsync();
        
        // 读取数据的方法
        Task<bool> ReadBoolAsync(string address);
        Task<byte> ReadByteAsync(string address);
        Task<short> ReadShortAsync(string address);
        Task<int> ReadIntAsync(string address);
        Task<float> ReadFloatAsync(string address);
        Task<byte[]> ReadBytesAsync(string address, int count);
        
        // 写入数据的方法（只使用布尔值）
        Task<bool> WriteBoolAsync(string address, bool value);
    }
}

