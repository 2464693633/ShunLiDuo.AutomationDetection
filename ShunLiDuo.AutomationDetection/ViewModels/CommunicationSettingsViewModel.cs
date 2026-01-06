using Prism.Mvvm;
using Prism.Commands;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using System.Linq;
using ShunLiDuo.AutomationDetection.Services;
using S7.Net;
using System;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class CommunicationSettingsViewModel : BindableBase
    {
        private string _ipAddress = "192.168.1.100";
        private CpuType _selectedCpuType = CpuType.S71500;
        private string _rack = "0";
        private string _slot = "1";
        private bool _isConnected = false;
        private bool _isConnecting = false;
        private string _connectionStatus = "未连接";
        private readonly IS7CommunicationService _s7Service;
        private readonly ICommunicationConfigService _configService;
        private bool _autoConnect = false;

        public bool AutoConnect
        {
            get => _autoConnect;
            set
            {
                if (SetProperty(ref _autoConnect, value))
                {
                    // 自动连接选项改变时保存配置（异步，不等待）
                    _ = SaveConfigAsync();
                }
            }
        }
        
        public ObservableCollection<CpuTypeItem> CpuTypes { get; private set; }

        public CommunicationSettingsViewModel(IS7CommunicationService s7Service, ICommunicationConfigService configService)
        {
            _s7Service = s7Service;
            _configService = configService;
            ConnectCommand = new DelegateCommand(OnConnect, () => !IsConnecting);
            
            // 初始化PLC型号列表
            InitializeCpuTypes();
            
            // 监听服务连接状态变化
            _s7Service.ConnectionStatusChanged += S7Service_ConnectionStatusChanged;
            UpdateConnectionStatus();
            
            // 加载保存的配置并自动连接
            LoadConfigAndAutoConnectAsync();
        }

        private async void LoadConfigAndAutoConnectAsync()
        {
            try
            {
                var config = await _configService.GetConfigAsync();
                
                // 加载保存的配置
                IpAddress = config.IpAddress;
                SelectedCpuType = config.CpuType;
                Rack = config.Rack.ToString();
                Slot = config.Slot.ToString();
                AutoConnect = config.AutoConnect; // 使用属性设置，会触发保存
                
                // 如果启用了自动连接，则自动连接
                if (_autoConnect && !string.IsNullOrWhiteSpace(IpAddress))
                {
                    // 延迟一下，确保UI已经加载完成
                    await Task.Delay(500);
                    await ConnectAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载通讯配置失败: {ex.Message}");
            }
        }

        private void InitializeCpuTypes()
        {
            CpuTypes = new ObservableCollection<CpuTypeItem>
            {
                new CpuTypeItem { CpuType = CpuType.S71200, DisplayName = "S7-1200" },
                new CpuTypeItem { CpuType = CpuType.S71500, DisplayName = "S7-1500" },
                new CpuTypeItem { CpuType = CpuType.S7300, DisplayName = "S7-300" },
                new CpuTypeItem { CpuType = CpuType.S7400, DisplayName = "S7-400" },
                new CpuTypeItem { CpuType = CpuType.S7200, DisplayName = "S7-200" },
                new CpuTypeItem { CpuType = CpuType.S7200Smart, DisplayName = "S7-200 Smart" }
            };
        }

        private void S7Service_ConnectionStatusChanged(object sender, bool isConnected)
        {
            IsConnected = isConnected;
            UpdateConnectionStatus();
        }

        private void UpdateConnectionStatus()
        {
            ConnectionStatus = _s7Service.ConnectionStatus;
            IsConnected = _s7Service.IsConnected;
        }

        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        public CpuType SelectedCpuType
        {
            get => _selectedCpuType;
            set => SetProperty(ref _selectedCpuType, value);
        }

        public string Rack
        {
            get => _rack;
            set => SetProperty(ref _rack, value);
        }

        public string Slot
        {
            get => _slot;
            set => SetProperty(ref _slot, value);
        }

        public short RackNumber
        {
            get
            {
                if (short.TryParse(_rack, out short rack))
                {
                    return rack;
                }
                return 0; // 默认机架号
            }
        }

        public short SlotNumber
        {
            get
            {
                if (short.TryParse(_slot, out short slot))
                {
                    return slot;
                }
                return 1; // 默认槽号
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (SetProperty(ref _isConnected, value))
                {
                    ConnectionStatus = value ? "已连接" : "未连接";
                }
            }
        }

        public bool IsConnecting
        {
            get => _isConnecting;
            set
            {
                SetProperty(ref _isConnecting, value);
                ConnectCommand.RaiseCanExecuteChanged();
                if (!value)
                {
                    ConnectionStatus = IsConnected ? "已连接" : "未连接";
                }
            }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public DelegateCommand ConnectCommand { get; private set; }

        private async void OnConnect()
        {
            if (IsConnected)
            {
                // 断开连接
                await DisconnectAsync();
            }
            else
            {
                // 连接
                await ConnectAsync();
            }
        }

        private async Task ConnectAsync()
        {
            // 验证IP地址
            if (string.IsNullOrWhiteSpace(IpAddress))
            {
                MessageBox.Show("请输入IP地址", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsConnecting = true;
            ConnectionStatus = "正在连接...";

            // 验证机架号和槽号
            if (string.IsNullOrWhiteSpace(Rack) || !short.TryParse(Rack, out short rack) || rack < 0)
            {
                MessageBox.Show("请输入有效的机架号（≥0）", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Slot) || !short.TryParse(Slot, out short slot) || slot < 0)
            {
                MessageBox.Show("请输入有效的槽号（≥0）", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 使用实际的S7通讯服务连接（标准S7通讯使用默认端口102）
                bool connected = await _s7Service.ConnectAsync(IpAddress, SelectedCpuType, rack, slot);
                
                if (connected)
                {
                    // 保存连接参数
                    await SaveConfigAsync();
                    
                    // 只在非自动连接时显示消息框（避免启动时弹出）
                    if (!AutoConnect)
                    {
                        MessageBox.Show($"S7通讯连接成功\n\nIP地址: {IpAddress}\nPLC型号: {GetCpuTypeName(SelectedCpuType)}\n机架号: {Rack}\n槽号: {Slot}", 
                            "连接成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    // 连接失败，只在手动连接时显示错误信息（自动连接时静默处理）
                    if (!AutoConnect)
                    {
                        string errorMessage = _s7Service.ConnectionStatus;
                        MessageBox.Show($"S7通讯连接失败\n\n{errorMessage}", "连接失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (System.Exception ex)
            {
                // 捕获其他未预期的异常，只在手动连接时显示（自动连接时静默处理）
                if (!AutoConnect)
                {
                    MessageBox.Show($"连接时发生异常:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    // 自动连接时只记录到调试输出
                    System.Diagnostics.Debug.WriteLine($"PLC自动连接异常: {ex.Message}");
                }
            }
            finally
            {
                IsConnecting = false;
                UpdateConnectionStatus();
            }
        }

        private async Task DisconnectAsync()
        {
            IsConnecting = true;
            ConnectionStatus = "正在断开...";

            try
            {
                // 使用实际的S7通讯服务断开连接
                await _s7Service.DisconnectAsync();
                MessageBox.Show("S7通讯已断开", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"断开连接失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsConnecting = false;
                UpdateConnectionStatus();
            }
        }

        private async Task SaveConfigAsync()
        {
            try
            {
                var config = new CommunicationConfig
                {
                    IpAddress = IpAddress,
                    CpuType = SelectedCpuType,
                    Rack = RackNumber,
                    Slot = SlotNumber,
                    AutoConnect = _autoConnect
                };
                await _configService.SaveConfigAsync(config);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存通讯配置失败: {ex.Message}");
            }
        }

        private string GetCpuTypeName(CpuType cpuType)
        {
            var item = CpuTypes.FirstOrDefault(c => c.CpuType == cpuType);
            return item?.DisplayName ?? cpuType.ToString();
        }
    }

    public class CpuTypeItem
    {
        public CpuType CpuType { get; set; }
        public string DisplayName { get; set; }
    }
}

