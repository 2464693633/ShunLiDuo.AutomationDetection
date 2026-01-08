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

                // 异步连接，确保异常被完全捕获，不重新抛出
                bool connectionSuccess = false;
                Exception connectionException = null;
                
                await Task.Run(() =>
                {
                    try
                    {
                        _plc.Open();
                        connectionSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        // 捕获所有异常，不重新抛出，保存起来让外层统一处理
                        connectionException = ex;
                        connectionSuccess = false;
                    }
                });
                
                // 如果连接过程中发生异常，在这里统一处理，不重新抛出
                if (!connectionSuccess)
                {
                    IsConnected = false;
                    
                    if (connectionException != null)
                    {
                        // 根据异常类型设置错误信息
                        if (connectionException is S7.Net.PlcException plcEx)
                        {
                            string errorMessage = GetFriendlyErrorMessage(plcEx);
                            ConnectionStatus = $"连接失败: {errorMessage}";
                        }
                        else if (connectionException is System.Net.Sockets.SocketException)
                        {
                            ConnectionStatus = $"连接失败: 无法连接到 {ipAddress}:102";
                        }
                        else
                        {
                            ConnectionStatus = $"连接失败: {connectionException.Message}";
                        }
                    }
                    else
                    {
                        ConnectionStatus = "连接失败：无法建立连接";
                    }
                    
                    try
                    {
                        _plc?.Close();
                    }
                    catch
                    {
                        // 忽略关闭时的异常
                    }
                    _plc = null;
                    return false;
                }

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
            catch (Exception ex)
            {
                // 额外的安全捕获，防止任何未预期的异常（虽然理论上不应该到达这里）
                IsConnected = false;
                ConnectionStatus = $"连接失败: {ex.Message}";
                try
                {
                    _plc?.Close();
                }
                catch
                {
                    // 忽略关闭时的异常
                }
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

        public async Task<bool> ReadBoolAsync(string address)
        {
            if (!IsConnected || _plc == null || !_plc.IsConnected)
            {
                throw new InvalidOperationException("PLC未连接");
            }

            return await Task.Run(() =>
            {
                try
                {
                    var dataType = S7.Net.DataType.DataBlock;
                    var dbNumber = 0;
                    var startByte = 0;
                    var bitNumber = 0;

                    ParseAddress(address, out dataType, out dbNumber, out startByte, out bitNumber);

                    // S7.Net没有直接的ReadBit方法，需要读取字节然后提取位
                    var bytes = _plc.ReadBytes(dataType, dbNumber, startByte, 1);
                    var byteValue = bytes[0];
                    return (byteValue & (1 << bitNumber)) != 0;
                }
                catch (Exception ex)
                {
                    throw new Exception($"读取布尔值失败 (地址: {address}): {ex.Message}", ex);
                }
            });
        }

        public async Task<byte> ReadByteAsync(string address)
        {
            if (!IsConnected || _plc == null || !_plc.IsConnected)
            {
                throw new InvalidOperationException("PLC未连接");
            }

            return await Task.Run(() =>
            {
                try
                {
                    var dataType = S7.Net.DataType.DataBlock;
                    var dbNumber = 0;
                    var startByte = 0;

                    ParseAddress(address, out dataType, out dbNumber, out startByte, out _);

                    var bytes = _plc.ReadBytes(dataType, dbNumber, startByte, 1);
                    return bytes[0];
                }
                catch (Exception ex)
                {
                    throw new Exception($"读取字节失败 (地址: {address}): {ex.Message}", ex);
                }
            });
        }

        public async Task<short> ReadShortAsync(string address)
        {
            if (!IsConnected || _plc == null || !_plc.IsConnected)
            {
                throw new InvalidOperationException("PLC未连接");
            }

            return await Task.Run(() =>
            {
                try
                {
                    var dataType = S7.Net.DataType.DataBlock;
                    var dbNumber = 0;
                    var startByte = 0;

                    ParseAddress(address, out dataType, out dbNumber, out startByte, out _);

                    var bytes = _plc.ReadBytes(dataType, dbNumber, startByte, 2);
                    // S7.Net使用大端字节序，需要手动转换
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);
                    return BitConverter.ToInt16(bytes, 0);
                }
                catch (Exception ex)
                {
                    throw new Exception($"读取短整型失败 (地址: {address}): {ex.Message}", ex);
                }
            });
        }

        public async Task<int> ReadIntAsync(string address)
        {
            if (!IsConnected || _plc == null || !_plc.IsConnected)
            {
                throw new InvalidOperationException("PLC未连接");
            }

            return await Task.Run(() =>
            {
                try
                {
                    var dataType = S7.Net.DataType.DataBlock;
                    var dbNumber = 0;
                    var startByte = 0;

                    ParseAddress(address, out dataType, out dbNumber, out startByte, out _);

                    var bytes = _plc.ReadBytes(dataType, dbNumber, startByte, 4);
                    // S7.Net使用大端字节序，需要手动转换
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);
                    return BitConverter.ToInt32(bytes, 0);
                }
                catch (Exception ex)
                {
                    throw new Exception($"读取整型失败 (地址: {address}): {ex.Message}", ex);
                }
            });
        }

        public async Task<float> ReadFloatAsync(string address)
        {
            if (!IsConnected || _plc == null || !_plc.IsConnected)
            {
                throw new InvalidOperationException("PLC未连接");
            }

            return await Task.Run(() =>
            {
                try
                {
                    var dataType = S7.Net.DataType.DataBlock;
                    var dbNumber = 0;
                    var startByte = 0;

                    ParseAddress(address, out dataType, out dbNumber, out startByte, out _);

                    var bytes = _plc.ReadBytes(dataType, dbNumber, startByte, 4);
                    // S7.Net使用大端字节序，需要手动转换
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);
                    return BitConverter.ToSingle(bytes, 0);
                }
                catch (Exception ex)
                {
                    throw new Exception($"读取浮点数失败 (地址: {address}): {ex.Message}", ex);
                }
            });
        }

        public async Task<byte[]> ReadBytesAsync(string address, int count)
        {
            if (!IsConnected || _plc == null || !_plc.IsConnected)
            {
                throw new InvalidOperationException("PLC未连接");
            }

            return await Task.Run(() =>
            {
                try
                {
                    var dataType = S7.Net.DataType.DataBlock;
                    var dbNumber = 0;
                    var startByte = 0;

                    ParseAddress(address, out dataType, out dbNumber, out startByte, out _);

                    return _plc.ReadBytes(dataType, dbNumber, startByte, count);
                }
                catch (Exception ex)
                {
                    throw new Exception($"读取字节数组失败 (地址: {address}, 长度: {count}): {ex.Message}", ex);
                }
            });
        }

        public async Task<bool> WriteBoolAsync(string address, bool value)
        {
            if (!IsConnected || _plc == null || !_plc.IsConnected)
            {
                throw new InvalidOperationException("PLC未连接");
            }

            return await Task.Run(() =>
            {
                try
                {
                    var dataType = S7.Net.DataType.DataBlock;
                    var dbNumber = 0;
                    var startByte = 0;
                    var bitNumber = 0;

                    ParseAddress(address, out dataType, out dbNumber, out startByte, out bitNumber);

                    // 读取当前字节值
                    var bytes = _plc.ReadBytes(dataType, dbNumber, startByte, 1);
                    var byteValue = bytes[0];

                    // 设置或清除特定位
                    if (value)
                    {
                        byteValue |= (byte)(1 << bitNumber);  // 设置位为1
                    }
                    else
                    {
                        byteValue &= (byte)~(1 << bitNumber); // 清除位为0
                    }

                    // 写回字节
                    _plc.WriteBytes(dataType, dbNumber, startByte, new byte[] { byteValue });
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"写入布尔值失败 (地址: {address}): {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// 解析PLC地址，支持格式：
        /// DB1.DBX0.0 (数据块1，字节0，位0)
        /// DB1.DBB0 (数据块1，字节0)
        /// DB1.DBW0 (数据块1，字0，2字节)
        /// DB1.DBD0 (数据块1，双字0，4字节)
        /// I0.0 (输入，字节0，位0)
        /// Q0.0 (输出，字节0，位0)
        /// M0.0 (标志位，字节0，位0)
        /// </summary>
        private void ParseAddress(string address, out S7.Net.DataType dataType, out int dbNumber, out int startByte, out int bitNumber)
        {
            dataType = S7.Net.DataType.DataBlock;
            dbNumber = 0;
            startByte = 0;
            bitNumber = 0;

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("地址不能为空");
            }

            address = address.Trim().ToUpper();

            // 解析数据块地址 DB1.DBX0.0 或 DB1.DBB0
            if (address.StartsWith("DB"))
            {
                dataType = S7.Net.DataType.DataBlock;
                var parts = address.Split('.');
                if (parts.Length < 2)
                {
                    throw new ArgumentException($"无效的地址格式: {address}");
                }

                // 提取DB编号
                var dbPart = parts[0].Substring(2); // 去掉 "DB"
                if (!int.TryParse(dbPart, out dbNumber))
                {
                    throw new ArgumentException($"无效的DB编号: {dbPart}");
                }

                // 解析地址类型和位置
                var addrPart = parts[1];
                if (addrPart.StartsWith("DBX"))
                {
                    // 位地址 DB1.DBX0.0
                    var bytePart = addrPart.Substring(3); // 去掉 "DBX"
                    if (!int.TryParse(bytePart, out startByte))
                    {
                        throw new ArgumentException($"无效的字节地址: {bytePart}");
                    }

                    if (parts.Length > 2)
                    {
                        if (!int.TryParse(parts[2], out bitNumber))
                        {
                            throw new ArgumentException($"无效的位地址: {parts[2]}");
                        }
                    }
                }
                else if (addrPart.StartsWith("DBB"))
                {
                    // 字节地址 DB1.DBB0
                    var bytePart = addrPart.Substring(3); // 去掉 "DBB"
                    if (!int.TryParse(bytePart, out startByte))
                    {
                        throw new ArgumentException($"无效的字节地址: {bytePart}");
                    }
                }
                else if (addrPart.StartsWith("DBW"))
                {
                    // 字地址 DB1.DBW0
                    var bytePart = addrPart.Substring(3); // 去掉 "DBW"
                    if (!int.TryParse(bytePart, out startByte))
                    {
                        throw new ArgumentException($"无效的字地址: {bytePart}");
                    }
                }
                else if (addrPart.StartsWith("DBD"))
                {
                    // 双字地址 DB1.DBD0
                    var bytePart = addrPart.Substring(3); // 去掉 "DBD"
                    if (!int.TryParse(bytePart, out startByte))
                    {
                        throw new ArgumentException($"无效的双字地址: {bytePart}");
                    }
                }
                else
                {
                    throw new ArgumentException($"不支持的地址类型: {addrPart}");
                }
            }
            // 解析输入地址 I0.0
            else if (address.StartsWith("I"))
            {
                dataType = S7.Net.DataType.Input;
                var parts = address.Substring(1).Split('.');
                if (!int.TryParse(parts[0], out startByte))
                {
                    throw new ArgumentException($"无效的输入字节地址: {parts[0]}");
                }
                if (parts.Length > 1 && !int.TryParse(parts[1], out bitNumber))
                {
                    throw new ArgumentException($"无效的输入位地址: {parts[1]}");
                }
            }
            // 解析输出地址 Q0.0
            else if (address.StartsWith("Q"))
            {
                dataType = S7.Net.DataType.Output;
                var parts = address.Substring(1).Split('.');
                if (!int.TryParse(parts[0], out startByte))
                {
                    throw new ArgumentException($"无效的输出字节地址: {parts[0]}");
                }
                if (parts.Length > 1 && !int.TryParse(parts[1], out bitNumber))
                {
                    throw new ArgumentException($"无效的输出位地址: {parts[1]}");
                }
            }
            // 解析标志位地址 M0.0
            else if (address.StartsWith("M"))
            {
                dataType = S7.Net.DataType.Memory;
                var parts = address.Substring(1).Split('.');
                if (!int.TryParse(parts[0], out startByte))
                {
                    throw new ArgumentException($"无效的标志位字节地址: {parts[0]}");
                }
                if (parts.Length > 1 && !int.TryParse(parts[1], out bitNumber))
                {
                    throw new ArgumentException($"无效的标志位地址: {parts[1]}");
                }
            }
            else
            {
                throw new ArgumentException($"不支持的地址格式: {address}");
            }
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

