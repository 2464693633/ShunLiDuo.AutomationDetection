using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public class ScannerCommunicationService : IScannerCommunicationService
    {
        private readonly Dictionary<int, SerialPort> _serialPorts = new Dictionary<int, SerialPort>();
        private readonly Dictionary<int, DetectionRoomItem> _roomConfigs = new Dictionary<int, DetectionRoomItem>();
        private readonly Dictionary<int, List<byte>> _readBuffers = new Dictionary<int, List<byte>>(); // 数据接收缓冲区
        private readonly Dictionary<int, System.Timers.Timer> _timeoutTimers = new Dictionary<int, System.Timers.Timer>(); // 超时定时器
        private readonly object _lockObject = new object();
        private const int DATA_TIMEOUT_MS = 100; // 数据接收超时时间（毫秒），如果100ms内没有新数据，则认为数据接收完成

        public event EventHandler<ScannerDataReceivedEventArgs> DataReceived;
        public event EventHandler<ScannerConnectionStatusChangedEventArgs> ConnectionStatusChanged;

        public async Task<(bool Success, string ErrorMessage)> TestConnectionAsync(DetectionRoomItem room)
        {
            return await Task.Run(() =>
            {
                if (room == null || string.IsNullOrWhiteSpace(room.ScannerPortName))
                {
                    return (false, "检测室配置为空或未配置串口号");
                }

                try
                {
                    // 检查串口是否存在
                    var availablePorts = SerialPort.GetPortNames();
                    if (!availablePorts.Contains(room.ScannerPortName))
                    {
                        return (false, $"串口 {room.ScannerPortName} 不存在。可用串口: {string.Join(", ", availablePorts)}");
                    }

                    // 先检查串口是否已经被本程序打开
                    lock (_lockObject)
                    {
                        if (_serialPorts.TryGetValue(room.Id, out var existingPort) && existingPort.IsOpen)
                        {
                            // 串口已经被本程序打开，检查参数是否匹配
                            if (existingPort.PortName == room.ScannerPortName &&
                                existingPort.BaudRate == room.ScannerBaudRate &&
                                existingPort.DataBits == room.ScannerDataBits &&
                                existingPort.StopBits == ParseStopBits(room.ScannerStopBits) &&
                                existingPort.Parity == ParseParity(room.ScannerParity))
                            {
                                return (true, "串口已连接，配置正确");
                            }
                            else
                            {
                                return (false, "串口已被本程序打开，但参数不匹配。请先关闭连接后重新配置");
                            }
                        }
                    }

                    // 串口没有被本程序打开，尝试打开进行测试
                    try
                    {
                        // 尝试打开串口进行测试
                        using (var testPort = new SerialPort(
                            room.ScannerPortName,
                            room.ScannerBaudRate,
                            ParseParity(room.ScannerParity),
                            room.ScannerDataBits,
                            ParseStopBits(room.ScannerStopBits)))
                        {
                            testPort.ReadTimeout = 1000;
                            testPort.WriteTimeout = 1000;
                            testPort.Encoding = Encoding.Default;  // 使用系统默认编码（支持中文和8位字符）
                            testPort.NewLine = "\r";  // 设置换行符为 \r
                            testPort.Open();
                            
                            // 如果成功打开，说明配置正确
                            if (testPort.IsOpen)
                            {
                                testPort.Close();
                                return (true, "连接测试成功");
                            }
                            else
                            {
                                return (false, "串口打开失败，但未抛出异常");
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return (false, $"串口 {room.ScannerPortName} 已被其他程序占用，请关闭占用该串口的程序后重试");
                    }
                    catch (ArgumentException ex)
                    {
                        return (false, $"串口参数错误: {ex.Message}");
                    }
                    catch (InvalidOperationException ex)
                    {
                        return (false, $"串口操作错误: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"测试串口连接失败: {ex.Message}");
                    return (false, $"测试连接时发生错误: {ex.Message}");
                }
            });
        }

        public async Task<bool> OpenConnectionAsync(DetectionRoomItem room)
        {
            return await Task.Run(() =>
            {
                if (room == null || string.IsNullOrWhiteSpace(room.ScannerPortName))
                {
                    return false;
                }

                Models.DetectionRoomItem connectedRoom = null;
                bool wasDisconnected = false;
                int disconnectedRoomId = room.Id;

                lock (_lockObject)
                {
                    // 如果已经连接，先关闭
                    if (_serialPorts.ContainsKey(room.Id))
                    {
                        wasDisconnected = CloseConnectionCore(room.Id);
                    }

                    try
                    {
                        var serialPort = new SerialPort(
                            room.ScannerPortName,
                            room.ScannerBaudRate,
                            ParseParity(room.ScannerParity),
                            room.ScannerDataBits,
                            ParseStopBits(room.ScannerStopBits))
                        {
                            ReadTimeout = 1000,
                            WriteTimeout = 1000,
                            Encoding = Encoding.Default,
                            NewLine = "\r"
                        };

                        serialPort.DataReceived += (sender, e) => OnDataReceived(room.Id, room.RoomName, sender as SerialPort);
                        serialPort.Open();

                        _serialPorts[room.Id] = serialPort;
                        _roomConfigs[room.Id] = room;

                        System.Diagnostics.Debug.WriteLine($"[串口连接] 成功打开串口 - 检测室ID:{room.Id}, 检测室名称:{room.RoomName}, 串口:{room.ScannerPortName}, 波特率:{room.ScannerBaudRate}");
                        connectedRoom = room;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[串口连接] 打开串口失败 - 检测室ID:{room?.Id}, 串口:{room?.ScannerPortName}, 错误:{ex.Message}, 堆栈:{ex.StackTrace}");
                    }
                }

                // 锁释放后再触发事件，避免死锁
                
                // 如果刚才关闭了旧连接，触发断开事件
                if (wasDisconnected)
                {
                     ConnectionStatusChanged?.Invoke(this, new ScannerConnectionStatusChangedEventArgs
                     {
                         RoomId = disconnectedRoomId,
                         IsConnected = false
                     });
                }

                // 如果成功连接，触发连接事件
                if (connectedRoom != null)
                {
                    var eventArgs = new ScannerConnectionStatusChangedEventArgs
                    {
                        RoomId = connectedRoom.Id,
                        IsConnected = true
                    };
                    System.Diagnostics.Debug.WriteLine($"[串口连接] 触发连接状态变化事件 (锁外) - 检测室ID:{connectedRoom.Id}, IsConnected:True");
                    ConnectionStatusChanged?.Invoke(this, eventArgs);
                    return true;
                }
                
                return false;
            });
        }

        public async Task<bool> CloseConnectionAsync(int roomId)
        {
            return await Task.Run(() =>
            {
                bool wasDisconnected = false;
                lock (_lockObject)
                {
                    wasDisconnected = CloseConnectionCore(roomId);
                }
                
                if (wasDisconnected)
                {
                     ConnectionStatusChanged?.Invoke(this, new ScannerConnectionStatusChangedEventArgs
                     {
                         RoomId = roomId,
                         IsConnected = false
                     });
                     return true;
                }
                return false;
            });
        }

        /// <summary>
        /// 关闭串口连接的核心逻辑（不包含锁，不触发事件）
        /// 调用者必须持有锁
        /// </summary>
        /// <returns>如果是关闭了开启的连接返回true，否则false</returns>
        private bool CloseConnectionCore(int roomId)
        {
            if (_serialPorts.TryGetValue(roomId, out var serialPort))
            {
                try
                {
                    bool wasOpen = serialPort.IsOpen;
                    if (wasOpen)
                    {
                        serialPort.Close();
                    }
                    serialPort.Dispose();
                    _serialPorts.Remove(roomId);
                    _roomConfigs.Remove(roomId);
                    _readBuffers.Remove(roomId);
                    
                    if (_timeoutTimers.TryGetValue(roomId, out var timer))
                    {
                        timer.Stop();
                        timer.Dispose();
                        _timeoutTimers.Remove(roomId);
                    }
                    
                    return wasOpen;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"关闭串口失败: {ex.Message}");
                    return false;
                }
            }
            return false;
        }

        public bool IsConnected(int roomId)
        {
            lock (_lockObject)
            {
                if (_serialPorts.TryGetValue(roomId, out var serialPort))
                {
                    return serialPort.IsOpen;
                }
                return false;
            }
        }

        public async Task<string> ReadScanDataAsync(int roomId)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    if (_serialPorts.TryGetValue(roomId, out var serialPort) && serialPort.IsOpen)
                    {
                        try
                        {
                            if (serialPort.BytesToRead > 0)
                            {
                                return serialPort.ReadLine().Trim();
                            }
                        }
                        catch (TimeoutException)
                        {
                            // 超时是正常的，没有数据可读
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"读取串口数据失败: {ex.Message}");
                        }
                    }
                }
                return null;
            });
        }

        private void OnDataReceived(int roomId, string roomName, SerialPort serialPort)
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    // 检查是否有数据可读
                    int bytesToRead = serialPort.BytesToRead;
                    if (bytesToRead > 0)
                    {
                        // 读取所有可用字节数据
                        byte[] buffer = new byte[bytesToRead];
                        int bytesRead = serialPort.Read(buffer, 0, bytesToRead);
                        
                        // 记录原始字节数据用于调试
                        string hexString = string.Join(" ", buffer.Take(bytesRead).Select(b => b.ToString("X2")));
                        System.Diagnostics.Debug.WriteLine($"[串口接收] 检测室ID:{roomId}, 原始字节数据(HEX): {hexString}, 字节数:{bytesRead}");
                        
                        // 获取或创建缓冲区
                        lock (_lockObject)
                        {
                            if (!_readBuffers.ContainsKey(roomId))
                            {
                                _readBuffers[roomId] = new List<byte>();
                            }
                            
                            // 将接收到的数据添加到缓冲区
                            for (int i = 0; i < bytesRead; i++)
                            {
                                _readBuffers[roomId].Add(buffer[i]);
                            }
                            
                            // 检查缓冲区中是否包含完整的行（以 \r 或 \n 结尾）
                            List<byte> currentBuffer = _readBuffers[roomId];
                            
                            // 检查是否包含回车符 (0x0D) 或换行符 (0x0A)
                            int crIndex = currentBuffer.IndexOf(0x0D);
                            int lfIndex = currentBuffer.IndexOf(0x0A);
                            int lineEndIndex = -1;
                            
                            if (crIndex >= 0)
                            {
                                lineEndIndex = crIndex;
                            }
                            else if (lfIndex >= 0)
                            {
                                lineEndIndex = lfIndex;
                            }
                            
                            // 如果找到了行结束符，立即处理
                            if (lineEndIndex >= 0)
                            {
                                // 如果有定时器在运行，先停止
                                if (_timeoutTimers.TryGetValue(roomId, out var timer))
                                {
                                    timer.Stop();
                                    timer.Dispose();
                                    _timeoutTimers.Remove(roomId);
                                }
                                
                                ProcessBufferData(roomId, roomName, lineEndIndex);
                            }
                            else
                            {
                                    // [针对无结束符的优化] 检查是否是以 'H' 开头的 7位 或 6位 数据
                                    // 用户反馈格式通常是 Hxxxxxx (7位) 或 Hxxxxx (6位)，且数据是一次性完整发送的
                                    // 所以只要达到 6位，我们就判断是完整数据进行处理
                                    int hIndex = currentBuffer.IndexOf(0x48); // 'H'
                                    if (hIndex >= 0)
                                    {
                                        int currentLength = currentBuffer.Count - hIndex;
                                        
                                        // 达到 6位 即触发（涵盖 6位 和 7位）
                                        if (currentLength >= 6)
                                        {
                                            // 优先取 7位（如果缓冲区够长），否则取 6位
                                            int packetLength = (currentLength >= 7) ? 7 : 6;
                                            int endIndex = hIndex + packetLength - 1;
                                            
                                            // 停止定时器
                                            if (_timeoutTimers.TryGetValue(roomId, out var timer))
                                            {
                                                timer.Stop();
                                                timer.Dispose();
                                                _timeoutTimers.Remove(roomId);
                                            }
                                            ProcessBufferData(roomId, roomName, endIndex);
                                            // 继续处理后续数据
                                            // continue; // 错误：这里不在循环内，不能使用 continue。ProcessBufferData 会移除数据，下一次事件会处理剩余（如果有）
                                            return; // 使用 return 结束本次处理，避免触发超时定时器 
                                        }
                                    }

                                // 没有结束符且未达到触发长度，启动或重置超时定时器
                                // 等待一段时间看是否还有后续数据
                                StartOrResetTimeoutTimer(roomId, roomName);
                            }
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[串口接收] 串口未打开或为空 - 检测室ID:{roomId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[串口接收] 处理串口数据失败 - 检测室ID:{roomId}, 错误:{ex.Message}, 堆栈:{ex.StackTrace}");
            }
        }

        private Parity ParseParity(string parity)
        {
            if (string.IsNullOrWhiteSpace(parity))
                return Parity.None;

            switch (parity.ToUpper())
            {
                case "ODD":
                    return Parity.Odd;
                case "EVEN":
                    return Parity.Even;
                case "MARK":
                    return Parity.Mark;
                case "SPACE":
                    return Parity.Space;
                default:
                    return Parity.None;
            }
        }

        private StopBits ParseStopBits(int stopBits)
        {
            switch (stopBits)
            {
                case 1:
                    return StopBits.One;
                case 2:
                    return StopBits.Two;
                default:
                    return StopBits.One;
            }
        }

        /// <summary>
        /// 将十六进制字符串转换为ASCII字符串
        /// 例如："48 30 30 30 31 0D" -> "H0001"
        /// </summary>
        private string ConvertHexStringToAscii(string hexString)
        {
            try
            {
                // 如果字符串看起来像十六进制字符串（包含空格和十六进制字符）
                if (hexString.Contains(" ") && System.Text.RegularExpressions.Regex.IsMatch(hexString, @"^[0-9A-Fa-f\s]+$"))
                {
                    // 去除空格，分割成字节
                    string[] hexBytes = hexString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    List<byte> bytes = new List<byte>();
                    
                    foreach (string hexByte in hexBytes)
                    {
                        // 跳过回车符的十六进制表示（0D 或 0d）
                        if (hexByte.Equals("0D", StringComparison.OrdinalIgnoreCase) || 
                            hexByte.Equals("0A", StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // 跳过回车符和换行符
                        }
                        
                        try
                        {
                            byte b = Convert.ToByte(hexByte, 16);
                            bytes.Add(b);
                        }
                        catch
                        {
                            // 如果转换失败，可能是非十六进制字符，直接返回原字符串
                            return hexString.Trim();
                        }
                    }
                    
                    // 将字节转换为字符串
                    if (bytes.Count > 0)
                    {
                        return Encoding.Default.GetString(bytes.ToArray()).Trim();
                    }
                }
                
                // 如果不是十六进制字符串格式，直接返回（去除回车符）
                return hexString.TrimEnd('\r', '\n', '\0').Trim();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[串口接收] 转换十六进制字符串失败: {ex.Message}");
                return hexString.Trim();
            }
        }

        /// <summary>
        /// 处理缓冲区中的数据
        /// </summary>
        /// <summary>
        /// 处理缓冲区中的数据
        /// </summary>
        private void ProcessBufferData(int roomId, string roomName, int lineEndIndex)
        {
            if (!_readBuffers.ContainsKey(roomId) || _readBuffers[roomId].Count == 0)
            {
                return;
            }

            List<byte> currentBuffer = _readBuffers[roomId];
            int lengthToProcess = lineEndIndex > 0 && lineEndIndex < currentBuffer.Count 
                ? lineEndIndex + 1 
                : currentBuffer.Count;
            
            // 提取原始字节数据
            byte[] rawBytes = currentBuffer.Take(lengthToProcess).ToArray();
            
            // 转换为字符串（使用默认编码）
            string rawString = Encoding.Default.GetString(rawBytes).Trim();
            
            // 记录调试信息
            string hexString = string.Join(" ", rawBytes.Select(b => b.ToString("X2")));
            
            // 尝试将十六进制字符串转换为实际的ASCII字符（如果内容本身是十六进制字符串的话，兼容旧逻辑）
            string data = ConvertHexStringToAscii(rawString);
            
            // 移除已处理的数据
            if (lengthToProcess < currentBuffer.Count)
            {
                currentBuffer.RemoveRange(0, lengthToProcess);
                
                // 如果紧接着是换行符(0x0A)，也要移除（处理 \r\n 的情况）
                if (currentBuffer.Count > 0 && currentBuffer[0] == 0x0A)
                {
                    currentBuffer.RemoveAt(0);
                }
            }
            else
            {
                currentBuffer.Clear();
            }
            
            if (!string.IsNullOrWhiteSpace(data) || rawBytes.Length > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[串口接收] 检测室ID:{roomId}, 检测室名称:{roomName}, 原始HEX:{hexString}, 转换后数据:{data}");
                
                DataReceived?.Invoke(this, new ScannerDataReceivedEventArgs
                {
                    RoomId = roomId,
                    RoomName = roomName,
                    ScanData = data,
                    RawBytes = rawBytes, // 传递原始字节
                    ReceiveTime = DateTime.Now
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[串口接收] 检测室ID:{roomId}, 数据为空");
            }
        }

        /// <summary>
        /// 启动或重置超时定时器
        /// </summary>
        private void StartOrResetTimeoutTimer(int roomId, string roomName)
        {
            lock (_lockObject)
            {
                // 如果定时器已存在，先停止
                if (_timeoutTimers.TryGetValue(roomId, out var existingTimer))
                {
                    existingTimer.Stop();
                    existingTimer.Dispose();
                }
                
                // 创建新的定时器
                var timer = new System.Timers.Timer(DATA_TIMEOUT_MS);
                timer.Elapsed += (sender, e) =>
                {
                    timer.Stop();
                    lock (_lockObject)
                    {
                        if (_readBuffers.ContainsKey(roomId) && _readBuffers[roomId].Count > 0)
                        {
                            // 超时后处理整个缓冲区（没有结束符的情况）
                            int bufferLength = _readBuffers[roomId].Count;
                            System.Diagnostics.Debug.WriteLine($"[串口接收] 检测室ID:{roomId}, 超时处理，缓冲区长度:{bufferLength}");
                            ProcessBufferData(roomId, roomName, bufferLength);
                        }
                        
                        // 清理定时器
                        if (_timeoutTimers.TryGetValue(roomId, out var timerToRemove) && timerToRemove == timer)
                        {
                            timerToRemove.Dispose();
                            _timeoutTimers.Remove(roomId);
                        }
                    }
                };
                timer.AutoReset = false;
                timer.Start();
                _timeoutTimers[roomId] = timer;
            }
        }

        /// <summary>
        /// 停止超时定时器
        /// </summary>
        private void StopTimeoutTimer(int roomId)
        {
            lock (_lockObject)
            {
                if (_timeoutTimers.TryGetValue(roomId, out var timer))
                {
                    timer.Stop();
                    timer.Dispose();
                    _timeoutTimers.Remove(roomId);
                }
            }
        }

        public async Task AutoConnectAllScannersAsync(System.Collections.Generic.List<DetectionRoomItem> rooms)
        {
            if (rooms == null || rooms.Count == 0)
            {
                return;
            }

            await Task.Run(async () =>
            {
                foreach (var room in rooms)
                {
                    if (room.ScannerIsEnabled && !string.IsNullOrWhiteSpace(room.ScannerPortName))
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"[自动连接] 尝试连接检测室串口 - 检测室ID:{room.Id}, 检测室名称:{room.RoomName}, 串口:{room.ScannerPortName}");
                            var result = await OpenConnectionAsync(room);
                            if (result)
                            {
                                System.Diagnostics.Debug.WriteLine($"[自动连接] 成功连接检测室串口 - 检测室ID:{room.Id}, 检测室名称:{room.RoomName}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[自动连接] 连接检测室串口失败 - 检测室ID:{room.Id}, 检测室名称:{room.RoomName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[自动连接] 连接检测室串口异常 - 检测室ID:{room.Id}, 检测室名称:{room.RoomName}, 错误:{ex.Message}");
                        }
                        
                        // 每个串口连接之间稍微延迟，避免同时打开多个串口导致问题
                        await Task.Delay(100);
                    }
                }
            });
        }
        public async Task CloseAllConnectionsAsync()
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        var roomIds = _serialPorts.Keys.ToList();
                        foreach (var roomId in roomIds)
                        {
                            CloseConnectionCore(roomId);
                            System.Diagnostics.Debug.WriteLine($"[串口服务] 应用程序已正常关闭串口连接 - 检测室ID:{roomId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[串口服务] 关闭所有连接时发生异常: {ex.Message}");
                    }
                }
            });
        }
    }
}

