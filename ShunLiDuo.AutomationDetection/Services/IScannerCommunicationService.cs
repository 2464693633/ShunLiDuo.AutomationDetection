using System;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public interface IScannerCommunicationService
    {
        /// <summary>
        /// 测试串口连接
        /// </summary>
        Task<(bool Success, string ErrorMessage)> TestConnectionAsync(DetectionRoomItem room);

        /// <summary>
        /// 打开串口连接
        /// </summary>
        Task<bool> OpenConnectionAsync(DetectionRoomItem room);

        /// <summary>
        /// 关闭串口连接
        /// </summary>
        Task<bool> CloseConnectionAsync(int roomId);

        /// <summary>
        /// 检查串口是否已连接
        /// </summary>
        bool IsConnected(int roomId);

        /// <summary>
        /// 读取扫码数据
        /// </summary>
        Task<string> ReadScanDataAsync(int roomId);

        /// <summary>
        /// 串口数据接收事件
        /// </summary>
        event EventHandler<ScannerDataReceivedEventArgs> DataReceived;

        /// <summary>
        /// 自动连接所有已启用扫码器的检测室
        /// </summary>
        Task AutoConnectAllScannersAsync(System.Collections.Generic.List<DetectionRoomItem> rooms);
    }

    public class ScannerDataReceivedEventArgs : EventArgs
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public string ScanData { get; set; }
        public DateTime ReceiveTime { get; set; }
    }
}

