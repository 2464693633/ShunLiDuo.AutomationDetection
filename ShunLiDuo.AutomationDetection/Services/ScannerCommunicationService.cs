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
        private readonly Dictionary<int, StringBuilder> _readBuffers = new Dictionary<int, StringBuilder>(); // 数据接收缓冲区
        private readonly object _lockObject = new object();

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
                            testPort.Encoding = Encoding.ASCII;  // 使用ASCII编码
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

                lock (_lockObject)
                {
                    // 如果已经连接，先关闭
                    if (_serialPorts.ContainsKey(room.Id))
                    {
                        CloseConnectionInternal(room.Id);
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
                            Encoding = Encoding.ASCII,  // 使用ASCII编码接收数据
                            NewLine = "\r"  // 设置换行符为 \r (0x0D)，匹配扫码器的回车符
                        };

                        serialPort.DataReceived += (sender, e) => OnDataReceived(room.Id, room.RoomName, sender as SerialPort);
                        serialPort.Open();

                        _serialPorts[room.Id] = serialPort;
                        _roomConfigs[room.Id] = room;

                        System.Diagnostics.Debug.WriteLine($"[串口连接] 成功打开串口 - 检测室ID:{room.Id}, 检测室名称:{room.RoomName}, 串口:{room.ScannerPortName}, 波特率:{room.ScannerBaudRate}");
                        
                        // 触发连接状态变化事件（在后台线程触发，事件处理会切换到UI线程）
                        var eventArgs = new ScannerConnectionStatusChangedEventArgs
                        {
                            RoomId = room.Id,
                            IsConnected = true
                        };
                        System.Diagnostics.Debug.WriteLine($"[串口连接] 准备触发连接状态变化事件 - 检测室ID:{room.Id}, IsConnected:True");
                        ConnectionStatusChanged?.Invoke(this, eventArgs);
                        System.Diagnostics.Debug.WriteLine($"[串口连接] 已触发连接状态变化事件 - 检测室ID:{room.Id}, 事件订阅者数量:{(ConnectionStatusChanged?.GetInvocationList()?.Length ?? 0)}");
                        
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[串口连接] 打开串口失败 - 检测室ID:{room?.Id}, 串口:{room?.ScannerPortName}, 错误:{ex.Message}, 堆栈:{ex.StackTrace}");
                        return false;
                    }
                }
            });
        }

        public async Task<bool> CloseConnectionAsync(int roomId)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    return CloseConnectionInternal(roomId);
                }
            });
        }

        private bool CloseConnectionInternal(int roomId)
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
                    _readBuffers.Remove(roomId); // 清理缓冲区
                    
                    // 触发连接状态变化事件
                    if (wasOpen)
                    {
                        ConnectionStatusChanged?.Invoke(this, new ScannerConnectionStatusChangedEventArgs
                        {
                            RoomId = roomId,
                            IsConnected = false
                        });
                    }
                    
                    return true;
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
                        
                        // 转换为ASCII字符串
                        string receivedData = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        System.Diagnostics.Debug.WriteLine($"[串口接收] 检测室ID:{roomId}, 接收到的字符串: '{receivedData}' (长度:{receivedData.Length})");
                        
                        // 获取或创建缓冲区
                        lock (_lockObject)
                        {
                            if (!_readBuffers.ContainsKey(roomId))
                            {
                                _readBuffers[roomId] = new StringBuilder();
                            }
                            
                            // 将接收到的数据添加到缓冲区
                            _readBuffers[roomId].Append(receivedData);
                            
                            // 检查缓冲区中是否包含完整的行（以 \r 或 \n 结尾）
                            string bufferContent = _readBuffers[roomId].ToString();
                            
                            // 检查是否包含回车符或换行符
                            int crIndex = bufferContent.IndexOf('\r');
                            int lfIndex = bufferContent.IndexOf('\n');
                            int lineEndIndex = -1;
                            
                            if (crIndex >= 0)
                            {
                                lineEndIndex = crIndex;
                            }
                            else if (lfIndex >= 0)
                            {
                                lineEndIndex = lfIndex;
                            }
                            
                            // 如果没有找到回车符，检查是否是十六进制字符串格式（如 "48 30 30 30 31 0D"）
                            if (lineEndIndex < 0)
                            {
                                // 检查是否包含 "0D" 或 "0d"（回车符的十六进制表示）
                                int hexCrIndex = -1;
                                string upperContent = bufferContent.ToUpper();
                                
                                // 查找 "0D" 或 " 0D " 或 "0D " 或 " 0D"
                                if (upperContent.Contains("0D"))
                                {
                                    hexCrIndex = upperContent.IndexOf("0D");
                                    // 确保 "0D" 前后是空格或字符串边界
                                    if (hexCrIndex >= 0)
                                    {
                                        bool isValid = false;
                                        // 检查前面是否是空格或字符串开始
                                        if (hexCrIndex == 0 || bufferContent[hexCrIndex - 1] == ' ')
                                        {
                                            // 检查后面是否是空格、回车符或字符串结束
                                            if (hexCrIndex + 2 >= bufferContent.Length || 
                                                bufferContent[hexCrIndex + 2] == ' ' || 
                                                bufferContent[hexCrIndex + 2] == '\r' || 
                                                bufferContent[hexCrIndex + 2] == '\n')
                                            {
                                                isValid = true;
                                            }
                                        }
                                        
                                        if (isValid)
                                        {
                                            lineEndIndex = hexCrIndex + 2; // 包含 "0D"
                                        }
                                    }
                                }
                            }
                            
                            if (lineEndIndex >= 0)
                            {
                                // 提取完整的行数据
                                string rawData = bufferContent.Substring(0, lineEndIndex).Trim();
                                
                                // 尝试将十六进制字符串转换为实际的ASCII字符
                                string data = ConvertHexStringToAscii(rawData);
                                
                                // 移除已处理的数据
                                if (lineEndIndex + 1 < bufferContent.Length)
                                {
                                    _readBuffers[roomId].Remove(0, lineEndIndex + 1);
                                    
                                    // 如果还有换行符，也要移除
                                    if (_readBuffers[roomId].Length > 0 && _readBuffers[roomId][0] == '\n')
                                    {
                                        _readBuffers[roomId].Remove(0, 1);
                                    }
                                }
                                else
                                {
                                    _readBuffers[roomId].Clear();
                                }
                                
                                if (!string.IsNullOrWhiteSpace(data))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[串口接收] 检测室ID:{roomId}, 检测室名称:{roomName}, 原始数据:'{rawData}', 转换后数据:{data}");
                                    
                                    DataReceived?.Invoke(this, new ScannerDataReceivedEventArgs
                                    {
                                        RoomId = roomId,
                                        RoomName = roomName,
                                        ScanData = data,
                                        ReceiveTime = DateTime.Now
                                    });
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[串口接收] 检测室ID:{roomId}, 数据为空或仅包含空白字符");
                                }
                            }
                            else
                            {
                                // 数据不完整，等待更多数据
                                System.Diagnostics.Debug.WriteLine($"[串口接收] 检测室ID:{roomId}, 数据不完整，缓冲区内容: '{bufferContent}', 等待更多数据...");
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
                    
                    // 将字节转换为ASCII字符串
                    if (bytes.Count > 0)
                    {
                        return Encoding.ASCII.GetString(bytes.ToArray()).Trim();
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
    }
}

