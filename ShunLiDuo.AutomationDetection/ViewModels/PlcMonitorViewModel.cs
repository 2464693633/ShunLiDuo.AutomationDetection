using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Services;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class PlcMonitorViewModel : BindableBase
    {
        private readonly IDetectionRoomService _detectionRoomService;
        private readonly IS7CommunicationService _s7Service;
        private ObservableCollection<PlcMonitorConfigItem> _configs;
        private PlcMonitorConfigItem _selectedConfig;
        private bool _isLoading;
        private bool _isMonitoring;
        private DispatcherTimer _monitorTimer;
        private string _monitorStatus = "未启动";

        public PlcMonitorViewModel(
            IDetectionRoomService detectionRoomService,
            IS7CommunicationService s7Service)
        {
            _detectionRoomService = detectionRoomService;
            _s7Service = s7Service;
            
            Configs = new ObservableCollection<PlcMonitorConfigItem>();
            
            AddCommand = new DelegateCommand(OnAdd, () => !IsLoading);
            EditCommand = new DelegateCommand(OnEdit, () => !IsLoading && SelectedConfig != null);
            DeleteCommand = new DelegateCommand(OnDelete, () => !IsLoading && SelectedConfig != null);
            ViewCommand = new DelegateCommand(OnView, () => !IsLoading && SelectedConfig != null);
            StartMonitorCommand = new DelegateCommand(OnStartMonitor, () => !IsLoading && _s7Service.IsConnected);
            StopMonitorCommand = new DelegateCommand(OnStopMonitor, () => !IsLoading && IsMonitoring);
            
            // 监听PLC连接状态变化
            _s7Service.ConnectionStatusChanged += S7Service_ConnectionStatusChanged;
            
            // 初始化监控定时器
            _monitorTimer = new DispatcherTimer();
            _monitorTimer.Interval = TimeSpan.FromMilliseconds(500); // 500ms刷新一次
            _monitorTimer.Tick += MonitorTimer_Tick;
            
            LoadConfigsAsync();
        }

        private void S7Service_ConnectionStatusChanged(object sender, bool isConnected)
        {
            StartMonitorCommand.RaiseCanExecuteChanged();
            if (!isConnected && IsMonitoring)
            {
                OnStopMonitor();
            }
        }

        private async void MonitorTimer_Tick(object sender, EventArgs e)
        {
            if (!IsMonitoring || !_s7Service.IsConnected)
            {
                return;
            }

            await ReadAllMonitorValuesAsync();
        }

        private async Task ReadAllMonitorValuesAsync()
        {
            if (!_s7Service.IsConnected)
            {
                MonitorStatus = "PLC未连接";
                return;
            }

            try
            {
                foreach (var config in Configs)
                {
                    await ReadMonitorValueAsync(config);
                }
                MonitorStatus = $"监控中 - {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MonitorStatus = $"监控错误: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[PLC监控] 读取数据失败: {ex.Message}");
            }
        }

        private async Task ReadMonitorValueAsync(PlcMonitorConfigItem config)
        {
            try
            {
                // 读取气缸1的所有地址
                var cylinder1Errors = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(config.Cylinder1ExtendAddress))
                {
                    try
                    {
                        config.Cylinder1ExtendValue = await _s7Service.ReadBoolAsync(config.Cylinder1ExtendAddress);
                    }
                    catch (Exception ex)
                    {
                        cylinder1Errors.Add($"伸出控制: {ex.Message}");
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(config.Cylinder1RetractAddress))
                {
                    try
                    {
                        config.Cylinder1RetractValue = await _s7Service.ReadBoolAsync(config.Cylinder1RetractAddress);
                    }
                    catch (Exception ex)
                    {
                        cylinder1Errors.Add($"缩回控制: {ex.Message}");
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(config.Cylinder1ExtendFeedbackAddress))
                {
                    try
                    {
                        config.Cylinder1ExtendFeedbackValue = await _s7Service.ReadBoolAsync(config.Cylinder1ExtendFeedbackAddress);
                    }
                    catch (Exception ex)
                    {
                        cylinder1Errors.Add($"伸出到位: {ex.Message}");
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(config.Cylinder1RetractFeedbackAddress))
                {
                    try
                    {
                        config.Cylinder1RetractFeedbackValue = await _s7Service.ReadBoolAsync(config.Cylinder1RetractFeedbackAddress);
                    }
                    catch (Exception ex)
                    {
                        cylinder1Errors.Add($"缩回到位: {ex.Message}");
                    }
                }
                
                config.Cylinder1Status = cylinder1Errors.Count > 0 ? string.Join("; ", cylinder1Errors) : "正常";

                // 读取气缸2的所有地址
                var cylinder2Errors = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(config.Cylinder2ExtendAddress))
                {
                    try
                    {
                        config.Cylinder2ExtendValue = await _s7Service.ReadBoolAsync(config.Cylinder2ExtendAddress);
                    }
                    catch (Exception ex)
                    {
                        cylinder2Errors.Add($"伸出控制: {ex.Message}");
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(config.Cylinder2RetractAddress))
                {
                    try
                    {
                        config.Cylinder2RetractValue = await _s7Service.ReadBoolAsync(config.Cylinder2RetractAddress);
                    }
                    catch (Exception ex)
                    {
                        cylinder2Errors.Add($"缩回控制: {ex.Message}");
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(config.Cylinder2ExtendFeedbackAddress))
                {
                    try
                    {
                        config.Cylinder2ExtendFeedbackValue = await _s7Service.ReadBoolAsync(config.Cylinder2ExtendFeedbackAddress);
                    }
                    catch (Exception ex)
                    {
                        cylinder2Errors.Add($"伸出到位: {ex.Message}");
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(config.Cylinder2RetractFeedbackAddress))
                {
                    try
                    {
                        config.Cylinder2RetractFeedbackValue = await _s7Service.ReadBoolAsync(config.Cylinder2RetractFeedbackAddress);
                    }
                    catch (Exception ex)
                    {
                        cylinder2Errors.Add($"缩回到位: {ex.Message}");
                    }
                }
                
                config.Cylinder2Status = cylinder2Errors.Count > 0 ? string.Join("; ", cylinder2Errors) : "正常";

                // 读取传感器
                if (!string.IsNullOrWhiteSpace(config.SensorAddress))
                {
                    try
                    {
                        config.SensorValue = await _s7Service.ReadBoolAsync(config.SensorAddress);
                        config.SensorStatus = "正常";
                    }
                    catch (Exception ex)
                    {
                        config.SensorStatus = $"读取错误: {ex.Message}";
                    }
                }
                else
                {
                    config.SensorStatus = "未配置";
                }

                config.LastUpdateTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                config.Cylinder1Status = $"错误: {ex.Message}";
                config.Cylinder2Status = $"错误: {ex.Message}";
                config.SensorStatus = $"错误: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[PLC监控] 读取配置失败 (ID:{config.Id}): {ex.Message}");
            }
        }

        private async Task<object> ReadValueByDataTypeAsync(string address, string dataType)
        {
            if (string.IsNullOrWhiteSpace(dataType))
            {
                dataType = "Bool";
            }

            switch (dataType.ToUpper())
            {
                case "BOOL":
                    return await _s7Service.ReadBoolAsync(address);
                case "BYTE":
                    return await _s7Service.ReadByteAsync(address);
                case "SHORT":
                case "INT16":
                    return await _s7Service.ReadShortAsync(address);
                case "INT":
                case "INT32":
                    return await _s7Service.ReadIntAsync(address);
                case "FLOAT":
                case "REAL":
                    return await _s7Service.ReadFloatAsync(address);
                default:
                    return await _s7Service.ReadBoolAsync(address);
            }
        }

        private async void OnStartMonitor()
        {
            if (!_s7Service.IsConnected)
            {
                MessageBox.Show("PLC未连接，无法启动监控", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsMonitoring = true;
            MonitorStatus = "监控已启动";
            _monitorTimer.Start();
            await ReadAllMonitorValuesAsync();
        }

        private void OnStopMonitor()
        {
            IsMonitoring = false;
            MonitorStatus = "监控已停止";
            _monitorTimer.Stop();
        }

        public async void LoadConfigsAsync()
        {
            IsLoading = true;
            try
            {
                var rooms = await _detectionRoomService.GetAllRoomsAsync();
                Configs.Clear();
                foreach (var room in rooms)
                {
                    // 将DetectionRoomItem转换为PlcMonitorConfigItem用于监控显示
                    var config = new PlcMonitorConfigItem
                    {
                        Id = room.Id,
                        RoomId = room.Id,
                        RoomNo = room.RoomNo,
                        RoomName = room.RoomName,
                        Cylinder1Name = "气缸1",  // 固定显示名称
                        Cylinder1ExtendAddress = room.Cylinder1ExtendAddress ?? string.Empty,
                        Cylinder1RetractAddress = room.Cylinder1RetractAddress ?? string.Empty,
                        Cylinder1ExtendFeedbackAddress = room.Cylinder1ExtendFeedbackAddress ?? string.Empty,
                        Cylinder1RetractFeedbackAddress = room.Cylinder1RetractFeedbackAddress ?? string.Empty,
                        Cylinder1DataType = room.Cylinder1DataType ?? "Bool",
                        Cylinder2Name = "气缸2",  // 固定显示名称
                        Cylinder2ExtendAddress = room.Cylinder2ExtendAddress ?? string.Empty,
                        Cylinder2RetractAddress = room.Cylinder2RetractAddress ?? string.Empty,
                        Cylinder2ExtendFeedbackAddress = room.Cylinder2ExtendFeedbackAddress ?? string.Empty,
                        Cylinder2RetractFeedbackAddress = room.Cylinder2RetractFeedbackAddress ?? string.Empty,
                        Cylinder2DataType = room.Cylinder2DataType ?? "Bool",
                        SensorName = "传感器",  // 固定显示名称
                        SensorAddress = room.SensorAddress ?? string.Empty,
                        SensorDataType = room.SensorDataType ?? "Bool",
                        Remark = room.Remark
                    };
                    Configs.Add(config);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载监控配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                // 加载完成后更新命令状态
                StartMonitorCommand.RaiseCanExecuteChanged();
                StopMonitorCommand.RaiseCanExecuteChanged();
            }
        }

        private void OnAdd()
        {
            MessageBox.Show("请在检测室管理中配置PLC参数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnEdit()
        {
            if (SelectedConfig == null) return;

            MessageBox.Show("请在检测室管理中编辑PLC参数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnDelete()
        {
            if (SelectedConfig == null)
            {
                return;
            }

            MessageBox.Show("PLC配置已合并到检测室设置中，请在检测室管理中编辑", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnView()
        {
            if (SelectedConfig == null) return;

            var dialog = new Views.ViewPlcMonitorConfigDialog(SelectedConfig);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        public ObservableCollection<PlcMonitorConfigItem> Configs
        {
            get => _configs;
            set => SetProperty(ref _configs, value);
        }

        public PlcMonitorConfigItem SelectedConfig
        {
            get => _selectedConfig;
            set
            {
                SetProperty(ref _selectedConfig, value);
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                ViewCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                AddCommand.RaiseCanExecuteChanged();
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                ViewCommand.RaiseCanExecuteChanged();
                StartMonitorCommand.RaiseCanExecuteChanged();
                StopMonitorCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsMonitoring
        {
            get => _isMonitoring;
            set
            {
                SetProperty(ref _isMonitoring, value);
                StartMonitorCommand.RaiseCanExecuteChanged();
                StopMonitorCommand.RaiseCanExecuteChanged();
            }
        }

        public string MonitorStatus
        {
            get => _monitorStatus;
            set => SetProperty(ref _monitorStatus, value);
        }

        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand EditCommand { get; private set; }
        public DelegateCommand DeleteCommand { get; private set; }
        public DelegateCommand ViewCommand { get; private set; }
        public DelegateCommand StartMonitorCommand { get; private set; }
        public DelegateCommand StopMonitorCommand { get; private set; }
    }
}

