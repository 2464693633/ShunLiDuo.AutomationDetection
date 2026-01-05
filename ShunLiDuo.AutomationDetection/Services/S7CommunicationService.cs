using System;
using System.Threading.Tasks;
using S7.Net;

namespace ShunLiDuo.AutomationDetection.Services
{
    public class S7CommunicationService : IS7CommunicationService, IDisposable
    {
        private Plc _plc;
        private bool _isConnected;
        private string _connectionStatus = "未连接";

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    ConnectionStatus = value ? "已连接" : "未连接";
                    ConnectionStatusChanged?.Invoke(this, value);
                }
            }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            private set
            {
                _connectionStatus = value;
            }
        }

        public event EventHandler<bool> ConnectionStatusChanged;

        public async Task<bool> ConnectAsync(string ipAddress, CpuType cpuType, short rack, short slot)
        {
            try
            {
                // 如果已连接，先断开
                if (_plc != null && _plc.IsConnected)
                {
                    await DisconnectAsync();
                }

                // 创建PLC连接对象
                // cpuType: PLC型号（S71200, S71500, S7300, S7400, etc.）
                // rack: 机架号（对于S7-1200/1500通常为0，对于S7-300/400根据实际配置）
                // slot: 槽号（对于S7-1200/1500通常为1，对于S7-300/400通常为2）
                // 注意：S7.Net使用标准S7通讯端口102（ISO-on-TCP）
                _plc = new Plc(cpuType, ipAddress, rack, slot);
                
                // 设置超时时间（毫秒）
                _plc.ReadTimeout = 5000;
                _plc.WriteTimeout = 5000;

                // 异步连接
                await Task.Run(() =>
                {
                    _plc.Open();
                });

                // 检查连接状态
                if (_plc != null && _plc.IsConnected)
                {
                    IsConnected = true;
                    ConnectionStatus = "已连接";
                    return true;
                }
                else
                {
                    IsConnected = false;
                    ConnectionStatus = "连接失败：无法建立连接";
                    _plc?.Close();
                    _plc = null;
                    return false;
                }
            }
            catch (S7.Net.PlcException plcEx)
            {
                IsConnected = false;
                // 处理S7.Net特定的异常，提供更友好的错误信息
                string errorMessage = GetFriendlyErrorMessage(plcEx);
                ConnectionStatus = $"连接失败: {errorMessage}";
                _plc?.Close();
                _plc = null;
                return false;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                ConnectionStatus = $"连接失败: {ex.Message}";
                _plc?.Close();
                _plc = null;
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (_plc != null)
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            if (_plc.IsConnected)
                            {
                                _plc.Close();
                            }
                        }
                        catch
                        {
                            // 忽略关闭时的异常
                        }
                    });
                }
                
                IsConnected = false;
                ConnectionStatus = "未连接";
                _plc = null;
            }
            catch (Exception ex)
            {
                // 断开连接失败时，仍然设置为未连接状态
                IsConnected = false;
                ConnectionStatus = $"断开连接失败: {ex.Message}";
                _plc = null;
            }
        }

        private string GetFriendlyErrorMessage(S7.Net.PlcException ex)
        {
            // 根据错误消息内容提供友好的错误信息
            string message = ex.Message ?? string.Empty;
            string innerMessage = ex.InnerException?.Message ?? string.Empty;
            
            // 检查TPKT错误（最常见的连接错误）
            if (message.Contains("TPKT") || message.Contains("incomplete") || innerMessage.Contains("TPKT"))
            {
                return "无法建立连接，请检查：\n1. IP地址是否正确\n2. PLC是否在线\n3. 网络连接是否正常\n4. 防火墙是否阻止连接\n5. PLC型号、机架号、槽号是否正确";
            }
            
            // 检查IP地址相关错误
            if (message.Contains("127.0.0.1") || message.Contains("localhost"))
            {
                return "IP地址为本地回环地址，请使用PLC的实际IP地址";
            }
            
            // 检查连接超时
            if (message.Contains("timeout") || message.Contains("超时"))
            {
                return "连接超时，请检查：\n1. PLC是否在线\n2. 网络连接是否正常\n3. IP地址是否正确";
            }
            
            // 检查PLC型号相关错误
            if (message.Contains("CpuType") || message.Contains("CPU") || message.Contains("型号"))
            {
                return "PLC型号不匹配，请检查选择的PLC型号是否正确";
            }
            
            // 检查机架号和槽号相关错误
            if (message.Contains("rack") || message.Contains("slot") || message.Contains("机架") || message.Contains("槽"))
            {
                return "机架号或槽号错误，请检查配置：\nS7-1200/1500: 机架号=0, 槽号=1\nS7-300/400: 机架号=0, 槽号=2（通常）";
            }
            
            // 默认返回原始消息，但添加内部异常信息
            if (!string.IsNullOrEmpty(innerMessage) && innerMessage != message)
            {
                return $"{message}\n详细: {innerMessage}";
            }
            
            return message;
        }

        public void Dispose()
        {
            try
            {
                if (_plc != null)
                {
                    if (_plc.IsConnected)
                    {
                        _plc.Close();
                    }
                    _plc = null;
                }
                IsConnected = false;
                ConnectionStatus = "未连接";
            }
            catch
            {
                // 忽略释放时的异常
            }
        }
    }
}

