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
        
        // 可以添加读写数据的方法
        // Task<byte[]> ReadDataAsync(string address, int length);
        // Task WriteDataAsync(string address, byte[] data);
    }
}

