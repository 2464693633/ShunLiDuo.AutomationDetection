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
        private readonly IPlcMonitorConfigService _configService;
        private readonly IDetectionRoomService _detectionRoomService;
        private readonly IS7CommunicationService _s7Service;
        private ObservableCollection<PlcMonitorConfigItem> _configs;
        private PlcMonitorConfigItem _selectedConfig;
        private bool _isLoading;
        private bool _isMonitoring;
        private DispatcherTimer _monitorTimer;
        private string _monitorStatus = "未启动";

        public PlcMonitorViewModel(
            IPlcMonitorConfigService configService,
            IDetectionRoomService detectionRoomService,
            IS7CommunicationService s7Service)
        {
            _configService = configService;
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
                        config.Cylinder1ExtendValue = await ReadValueByDataTypeAsync(config.Cylinder1ExtendAddress, config.Cylinder1DataType);
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
                        config.Cylinder1RetractValue = await ReadValueByDataTypeAsync(config.Cylinder1RetractAddress, config.Cylinder1DataType);
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
                        config.Cylinder1ExtendFeedbackValue = await ReadValueByDataTypeAsync(config.Cylinder1ExtendFeedbackAddress, config.Cylinder1DataType);
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
                        config.Cylinder1RetractFeedbackValue = await ReadValueByDataTypeAsync(config.Cylinder1RetractFeedbackAddress, config.Cylinder1DataType);
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
                        config.Cylinder2ExtendValue = await ReadValueByDataTypeAsync(config.Cylinder2ExtendAddress, config.Cylinder2DataType);
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
                        config.Cylinder2RetractValue = await ReadValueByDataTypeAsync(config.Cylinder2RetractAddress, config.Cylinder2DataType);
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
                        config.Cylinder2ExtendFeedbackValue = await ReadValueByDataTypeAsync(config.Cylinder2ExtendFeedbackAddress, config.Cylinder2DataType);
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
                        config.Cylinder2RetractFeedbackValue = await ReadValueByDataTypeAsync(config.Cylinder2RetractFeedbackAddress, config.Cylinder2DataType);
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
                    config.SensorValue = await ReadValueByDataTypeAsync(config.SensorAddress, config.SensorDataType);
                    config.SensorStatus = "正常";
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
                var configs = await _configService.GetAllConfigsAsync();
                Configs.Clear();
                foreach (var config in configs)
                {
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
            }
        }

        private void OnAdd()
        {
            var dialog = new Views.AddPlcMonitorConfigDialog(_detectionRoomService, _configService);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                LoadConfigsAsync();
            }
        }

        private void OnEdit()
        {
            if (SelectedConfig == null) return;

            var dialog = new Views.AddPlcMonitorConfigDialog(_detectionRoomService, _configService, SelectedConfig);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                LoadConfigsAsync();
            }
        }

        private async void OnDelete()
        {
            if (SelectedConfig == null)
            {
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除检测室 '{SelectedConfig.RoomName}' 的监控配置吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                try
                {
                    var success = await _configService.DeleteConfigAsync(SelectedConfig.Id);
                    if (success)
                    {
                        MessageBox.Show("删除成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadConfigsAsync();
                    }
                    else
                    {
                        MessageBox.Show("删除失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
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

