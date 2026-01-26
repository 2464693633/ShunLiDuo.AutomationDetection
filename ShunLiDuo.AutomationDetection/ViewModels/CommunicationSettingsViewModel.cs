using Prism.Mvvm;
using Prism.Commands;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using System.Linq;
using ShunLiDuo.AutomationDetection.Services;
using S7.Net;
using System;
using ShunLiDuo.AutomationDetection.Views;

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
        private readonly IScannerCommunicationService _scannerService;
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
        public ObservableCollection<string> ScannerPortNames { get; private set; }
        public ObservableCollection<int> ScannerBaudRates { get; private set; }
        public ObservableCollection<int> ScannerDataBitsList { get; private set; }
        public ObservableCollection<int> ScannerStopBitsList { get; private set; }
        public ObservableCollection<string> ScannerParities { get; private set; }
        
        // 工作模式
        public ObservableCollection<WorkModeItem> WorkModes { get; private set; }
        private WorkMode _selectedWorkMode = WorkMode.Standard;
        public WorkMode SelectedWorkMode
        {
            get => _selectedWorkMode;
            set
            {
                if (SetProperty(ref _selectedWorkMode, value, () => _ = SaveConfigAsync()))
                {
                    // [修复] 确保描述也会更新
                    RaisePropertyChanged(nameof(SelectedWorkModeDescription));
                }
            }
        }
        
        public string SelectedWorkModeDescription
        {
            get
            {
                return _selectedWorkMode == WorkMode.Standard 
                    ? "支持传送带自动控制、检测室分配、气缸控制" 
                    : "仅记录扫码数据，适用于无传送带环境";
            }
        }
        
        public DelegateCommand RefreshPortsCommand { get; private set; }
        public DelegateCommand TestLoadingScannerConnectionCommand { get; private set; }
        public DelegateCommand TestUnloadingScannerConnectionCommand { get; private set; }

        // --- Preview Properties ---
        private string _loadingScannerPreview;
        public string LoadingScannerPreview
        {
            get => _loadingScannerPreview;
            set => SetProperty(ref _loadingScannerPreview, value);
        }

        private string _unloadingScannerPreview;
        public string UnloadingScannerPreview
        {
            get => _unloadingScannerPreview;
            set => SetProperty(ref _unloadingScannerPreview, value);
        }

        public CommunicationSettingsViewModel(IS7CommunicationService s7Service, IScannerCommunicationService scannerService, ICommunicationConfigService configService)
        {
            _s7Service = s7Service;
            _scannerService = scannerService;
            _configService = configService;
            ConnectCommand = new DelegateCommand(OnConnect, () => !IsConnecting);
            RefreshPortsCommand = new DelegateCommand(InitializeScannerOptions);
            TestLoadingScannerConnectionCommand = new DelegateCommand(OnTestLoadingScannerConnection);
            TestUnloadingScannerConnectionCommand = new DelegateCommand(OnTestUnloadingScannerConnection);
            
            // 初始化PLC型号列表
            InitializeCpuTypes();
            // 初始化扫码器选项
            InitializeScannerOptions();
            // 初始化工作模式
            InitializeWorkModes();
            
            // 监听服务连接状态变化
            _s7Service.ConnectionStatusChanged += S7Service_ConnectionStatusChanged;
            UpdateConnectionStatus();
            
            // 监听扫码数据
            _scannerService.DataReceived += OnScannerDataReceived;
            
            // 加载保存的配置并自动连接
            LoadConfigAndAutoConnectAsync();
        }

        private void OnScannerDataReceived(object sender, ScannerDataReceivedEventArgs e)
        {
            // 在UI线程更新
            Application.Current.Dispatcher.Invoke(() =>
            {
                string hexString = "";
                if (e.RawBytes != null && e.RawBytes.Length > 0)
                {
                    hexString = $"[HEX: {string.Join(" ", e.RawBytes.Select(b => b.ToString("X2")))}] ";
                }

                if (e.RoomId == -1) // 上料扫码枪
                {
                    LoadingScannerPreview = $"{DateTime.Now:HH:mm:ss} - {hexString}{e.ScanData}";
                }
                else if (e.RoomId == -2) // 下料扫码枪
                {
                    UnloadingScannerPreview = $"{DateTime.Now:HH:mm:ss} - {hexString}{e.ScanData}";
                }
            });
        }
// ... (Keep existing methods)

        private async Task TestScannerConnection(Models.DetectionRoomItem room)
        {
            try
            {
                // 使用 OpenConnectionAsync 替代 TestConnectionAsync
                // 这样连接会保持打开，允许用户扫码测试
                
                // 先尝试关闭可能存在的连接
                if (_scannerService.IsConnected(room.Id))
                {
                    await _scannerService.CloseConnectionAsync(room.Id);
                }

                // 尝试打开连接
                var success = await _scannerService.OpenConnectionAsync(room);
                
                if (success)
                {
                    CustomMessageBox.ShowInformation($"连接成功！\n串口: {room.ScannerPortName}\n\n请尝试扫码，结果将显示在预览框中。", "测试成功");
                }
                else
                {
                    CustomMessageBox.ShowError($"连接失败，请检查串口是否被占用或参数是否正确。", "测试失败");
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"测试过程中发生异常：{ex.Message}");
            }
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
                
                // 加载上料扫码枪配置
                LoadingScannerPort = config.LoadingScannerPort;
                LoadingScannerBaudRate = config.LoadingScannerBaudRate;
                LoadingScannerDataBits = config.LoadingScannerDataBits;
                LoadingScannerStopBits = config.LoadingScannerStopBits;
                LoadingScannerParity = config.LoadingScannerParity;
                LoadingScannerIsEnabled = config.LoadingScannerIsEnabled;
                
                // 加载下料扫码枪配置
                UnloadingScannerPort = config.UnloadingScannerPort;
                UnloadingScannerBaudRate = config.UnloadingScannerBaudRate;
                UnloadingScannerDataBits = config.UnloadingScannerDataBits;
                UnloadingScannerStopBits = config.UnloadingScannerStopBits;
                UnloadingScannerParity = config.UnloadingScannerParity;
                UnloadingScannerIsEnabled = config.UnloadingScannerIsEnabled;
                
                // 加载气缸参数
                LoadingCylinderExtendDelay = config.LoadingCylinderExtendDelay;
                LoadingCylinderRetractDelay = config.LoadingCylinderRetractDelay;
                LoadingCylinderInterlockDelay = config.LoadingCylinderInterlockDelay;
                LoadingCylinderCooldown = config.LoadingCylinderCooldown;
                LoadingCylinderLoopInterval = config.LoadingCylinderLoopInterval;
                
                UnloadingCylinderExtendDelay = config.UnloadingCylinderExtendDelay;
                UnloadingCylinderRetractDelay = config.UnloadingCylinderRetractDelay;
                UnloadingCylinderInterlockDelay = config.UnloadingCylinderInterlockDelay;
                UnloadingCylinderCooldown = config.UnloadingCylinderCooldown;
                UnloadingCylinderLoopInterval = config.UnloadingCylinderLoopInterval;
                
                // 加载工作模式
                SelectedWorkMode = config.Mode;
                RaisePropertyChanged(nameof(SelectedWorkModeDescription));
                
                AutoConnect = config.AutoConnect; // 使用属性设置，会触发保存（这里可能会多保存一次，但问题不大）
                
                // 如果启用了自动连接，则自动连接PLC
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
        
        private void InitializeWorkModes()
        {
            WorkModes = new ObservableCollection<WorkModeItem>
            {
                new WorkModeItem { Mode = WorkMode.Standard, DisplayName = "标准模式" },
                new WorkModeItem { Mode = WorkMode.Simple, DisplayName = "简易模式" }
            };
        }
        
        private void InitializeScannerOptions()
        {
            // 初始化串口列表
            ScannerPortNames = new ObservableCollection<string>(System.IO.Ports.SerialPort.GetPortNames());
            
            // 初始化波特率
            ScannerBaudRates = new ObservableCollection<int> { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
            
            // 初始化数据位
            ScannerDataBitsList = new ObservableCollection<int> { 5, 6, 7, 8 };
            
            // 初始化停止位
            ScannerStopBitsList = new ObservableCollection<int> { 1, 2 };
            
            // 初始化校验位
            ScannerParities = new ObservableCollection<string> { "None", "Odd", "Even", "Mark", "Space" };
            
            RaisePropertyChanged(nameof(ScannerPortNames));
            RaisePropertyChanged(nameof(ScannerBaudRates));
            RaisePropertyChanged(nameof(ScannerDataBitsList));
            RaisePropertyChanged(nameof(ScannerStopBitsList));
            RaisePropertyChanged(nameof(ScannerParities));
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
        
        // --- PLC Properties ---
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
        
        // --- Loading Scanner Properties ---
        private string _loadingScannerPort;
        private int _loadingScannerBaudRate = 9600;
        private int _loadingScannerDataBits = 8;
        private int _loadingScannerStopBits = 1;
        private string _loadingScannerParity = "None";
        private bool _loadingScannerIsEnabled = false;

        public string LoadingScannerPort
        {
            get => _loadingScannerPort;
            set => SetProperty(ref _loadingScannerPort, value, () => _ = SaveConfigAsync());
        }

        public int LoadingScannerBaudRate
        {
            get => _loadingScannerBaudRate;
            set => SetProperty(ref _loadingScannerBaudRate, value, () => _ = SaveConfigAsync());
        }

        public int LoadingScannerDataBits
        {
            get => _loadingScannerDataBits;
            set => SetProperty(ref _loadingScannerDataBits, value, () => _ = SaveConfigAsync());
        }

        public int LoadingScannerStopBits
        {
            get => _loadingScannerStopBits;
            set => SetProperty(ref _loadingScannerStopBits, value, () => _ = SaveConfigAsync());
        }

        public string LoadingScannerParity
        {
            get => _loadingScannerParity;
            set => SetProperty(ref _loadingScannerParity, value, () => _ = SaveConfigAsync());
        }

        public bool LoadingScannerIsEnabled
        {
            get => _loadingScannerIsEnabled;
            set => SetProperty(ref _loadingScannerIsEnabled, value, () => _ = SaveConfigAsync());
        }

        // --- Unloading Scanner Properties ---
        private string _unloadingScannerPort;
        private int _unloadingScannerBaudRate = 9600;
        private int _unloadingScannerDataBits = 8;
        private int _unloadingScannerStopBits = 1;
        private string _unloadingScannerParity = "None";
        private bool _unloadingScannerIsEnabled = false;

        public string UnloadingScannerPort
        {
            get => _unloadingScannerPort;
            set => SetProperty(ref _unloadingScannerPort, value, () => _ = SaveConfigAsync());
        }

        public int UnloadingScannerBaudRate
        {
            get => _unloadingScannerBaudRate;
            set => SetProperty(ref _unloadingScannerBaudRate, value, () => _ = SaveConfigAsync());
        }

        public int UnloadingScannerDataBits
        {
            get => _unloadingScannerDataBits;
            set => SetProperty(ref _unloadingScannerDataBits, value, () => _ = SaveConfigAsync());
        }

        public int UnloadingScannerStopBits
        {
            get => _unloadingScannerStopBits;
            set => SetProperty(ref _unloadingScannerStopBits, value, () => _ = SaveConfigAsync());
        }

        public string UnloadingScannerParity
        {
            get => _unloadingScannerParity;
            set => SetProperty(ref _unloadingScannerParity, value, () => _ = SaveConfigAsync());
        }

        public bool UnloadingScannerIsEnabled
        {
            get => _unloadingScannerIsEnabled;
            set => SetProperty(ref _unloadingScannerIsEnabled, value, () => _ = SaveConfigAsync());
        }
        
        // --- 上料弯道气缸参数 ---
        private int _loadingCylinderExtendDelay = 3000;
        private int _loadingCylinderRetractDelay = 2000;
        private int _loadingCylinderInterlockDelay = 50;
        private int _loadingCylinderCooldown = 500;
        private int _loadingCylinderLoopInterval = 50;
        
        public int LoadingCylinderExtendDelay
        {
            get => _loadingCylinderExtendDelay;
            set => SetProperty(ref _loadingCylinderExtendDelay, value, () => _ = SaveConfigAsync());
        }
        
        public int LoadingCylinderRetractDelay
        {
            get => _loadingCylinderRetractDelay;
            set => SetProperty(ref _loadingCylinderRetractDelay, value, () => _ = SaveConfigAsync());
        }
        
        public int LoadingCylinderInterlockDelay
        {
            get => _loadingCylinderInterlockDelay;
            set => SetProperty(ref _loadingCylinderInterlockDelay, value, () => _ = SaveConfigAsync());
        }
        
        public int LoadingCylinderCooldown
        {
            get => _loadingCylinderCooldown;
            set => SetProperty(ref _loadingCylinderCooldown, value, () => _ = SaveConfigAsync());
        }
        
        public int LoadingCylinderLoopInterval
        {
            get => _loadingCylinderLoopInterval;
            set => SetProperty(ref _loadingCylinderLoopInterval, value, () => _ = SaveConfigAsync());
        }
        
        // --- 下料弯道气缸参数 ---
        private int _unloadingCylinderExtendDelay = 3000;
        private int _unloadingCylinderRetractDelay = 2000;
        private int _unloadingCylinderInterlockDelay = 50;
        private int _unloadingCylinderCooldown = 500;
        private int _unloadingCylinderLoopInterval = 50;
        
        public int UnloadingCylinderExtendDelay
        {
            get => _unloadingCylinderExtendDelay;
            set => SetProperty(ref _unloadingCylinderExtendDelay, value, () => _ = SaveConfigAsync());
        }
        
        public int UnloadingCylinderRetractDelay
        {
            get => _unloadingCylinderRetractDelay;
            set => SetProperty(ref _unloadingCylinderRetractDelay, value, () => _ = SaveConfigAsync());
        }
        
        public int UnloadingCylinderInterlockDelay
        {
            get => _unloadingCylinderInterlockDelay;
            set => SetProperty(ref _unloadingCylinderInterlockDelay, value, () => _ = SaveConfigAsync());
        }
        
        public int UnloadingCylinderCooldown
        {
            get => _unloadingCylinderCooldown;
            set => SetProperty(ref _unloadingCylinderCooldown, value, () => _ = SaveConfigAsync());
        }
        
        public int UnloadingCylinderLoopInterval
        {
            get => _unloadingCylinderLoopInterval;
            set => SetProperty(ref _unloadingCylinderLoopInterval, value, () => _ = SaveConfigAsync());
        }

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
                CustomMessageBox.ShowWarning("请输入IP地址");
                return;
            }

            IsConnecting = true;
            ConnectionStatus = "正在连接...";

            // 验证机架号和槽号
            if (string.IsNullOrWhiteSpace(Rack) || !short.TryParse(Rack, out short rack) || rack < 0)
            {
                CustomMessageBox.ShowWarning("请输入有效的机架号（≥0）");
                return;
            }

            if (string.IsNullOrWhiteSpace(Slot) || !short.TryParse(Slot, out short slot) || slot < 0)
            {
                CustomMessageBox.ShowWarning("请输入有效的槽号（≥0）");
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
                        CustomMessageBox.ShowInformation($"S7通讯连接成功\n\nIP地址: {IpAddress}\nPLC型号: {GetCpuTypeName(SelectedCpuType)}\n机架号: {Rack}\n槽号: {Slot}", "连接成功");
                    }
                }
                else
                {
                    // 连接失败，只在手动连接时显示错误信息（自动连接时静默处理）
                    if (!AutoConnect)
                    {
                        string errorMessage = _s7Service.ConnectionStatus;
                        CustomMessageBox.ShowError($"S7通讯连接失败\n\n{errorMessage}", "连接失败");
                    }
                }
            }
            catch (System.Exception ex)
            {
                // 捕获其他未预期的异常，只在手动连接时显示（自动连接时静默处理）
                if (!AutoConnect)
                {
                    CustomMessageBox.ShowError($"连接时发生异常:\n{ex.Message}");
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
                CustomMessageBox.ShowInformation("S7通讯已断开");
            }
            catch (System.Exception ex)
            {
                CustomMessageBox.ShowError($"断开连接失败: {ex.Message}");
            }
            finally
            {
                IsConnecting = false;
                UpdateConnectionStatus();
            }
        }

        private async void OnTestLoadingScannerConnection()
        {
            if (string.IsNullOrWhiteSpace(LoadingScannerPort))
            {
                CustomMessageBox.ShowWarning("请选择上料扫码枪串口号");
                return;
            }

            var room = new Models.DetectionRoomItem
            {
                Id = -1, // 临时ID
                RoomName = "上料扫码枪测试",
                ScannerPortName = LoadingScannerPort,
                ScannerBaudRate = LoadingScannerBaudRate,
                ScannerDataBits = LoadingScannerDataBits,
                ScannerStopBits = LoadingScannerStopBits,
                ScannerParity = LoadingScannerParity
            };

            await TestScannerConnection(room);
        }

        private async void OnTestUnloadingScannerConnection()
        {
            if (string.IsNullOrWhiteSpace(UnloadingScannerPort))
            {
                CustomMessageBox.ShowWarning("请选择下料扫码枪串口号");
                return;
            }

            var room = new Models.DetectionRoomItem
            {
                Id = -2, // 临时ID
                RoomName = "下料扫码枪测试",
                ScannerPortName = UnloadingScannerPort,
                ScannerBaudRate = UnloadingScannerBaudRate,
                ScannerDataBits = UnloadingScannerDataBits,
                ScannerStopBits = UnloadingScannerStopBits,
                ScannerParity = UnloadingScannerParity
            };

            await TestScannerConnection(room);
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
                    AutoConnect = _autoConnect,
                    
                    // 保存上料扫码枪配置
                    LoadingScannerPort = _loadingScannerPort,
                    LoadingScannerBaudRate = _loadingScannerBaudRate,
                    LoadingScannerDataBits = _loadingScannerDataBits,
                    LoadingScannerStopBits = _loadingScannerStopBits,
                    LoadingScannerParity = _loadingScannerParity,
                    LoadingScannerIsEnabled = _loadingScannerIsEnabled,
                    
                    // 保存下料扫码枪配置
                    UnloadingScannerPort = _unloadingScannerPort,
                    UnloadingScannerBaudRate = _unloadingScannerBaudRate,
                    UnloadingScannerDataBits = _unloadingScannerDataBits,
                    UnloadingScannerStopBits = _unloadingScannerStopBits,
                    UnloadingScannerParity = _unloadingScannerParity,
                    UnloadingScannerIsEnabled = _unloadingScannerIsEnabled,
                    
                    // 保存气缸参数
                    LoadingCylinderExtendDelay = _loadingCylinderExtendDelay,
                    LoadingCylinderRetractDelay = _loadingCylinderRetractDelay,
                    LoadingCylinderInterlockDelay = _loadingCylinderInterlockDelay,
                    LoadingCylinderCooldown = _loadingCylinderCooldown,
                    LoadingCylinderLoopInterval = _loadingCylinderLoopInterval,
                    
                    UnloadingCylinderExtendDelay = _unloadingCylinderExtendDelay,
                    UnloadingCylinderRetractDelay = _unloadingCylinderRetractDelay,
                    UnloadingCylinderInterlockDelay = _unloadingCylinderInterlockDelay,
                    UnloadingCylinderCooldown = _unloadingCylinderCooldown,
                    UnloadingCylinderLoopInterval = _unloadingCylinderLoopInterval,
                    
                    // 保存工作模式
                    Mode = _selectedWorkMode
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
    
    public class WorkModeItem
    {
        public WorkMode Mode { get; set; }
        public string DisplayName { get; set; }
    }
}

