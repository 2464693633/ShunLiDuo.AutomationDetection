using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.Views;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class DetectionRoomManagementViewModel : BindableBase
    {
        private string _searchKeyword;
        private ObservableCollection<DetectionRoomItem> _detectionRooms;
        private DetectionRoomItem _selectedItem;
        private int _totalCount;
        private bool _isLoading;
        private readonly IDetectionRoomService _detectionRoomService;
        private readonly IAccountService _accountService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IScannerCommunicationService _scannerService;
        
        // 权限属性
        private bool _canAdd;
        private bool _canEdit;
        private bool _canDelete;
        private bool _canView;
        private HashSet<string> _userPermissions = new HashSet<string>();

        public DetectionRoomManagementViewModel(
            IDetectionRoomService detectionRoomService,
            IAccountService accountService,
            ICurrentUserService currentUserService,
            IScannerCommunicationService scannerService)
        {
            _detectionRoomService = detectionRoomService;
            _accountService = accountService;
            _currentUserService = currentUserService;
            _scannerService = scannerService;
            DetectionRooms = new ObservableCollection<DetectionRoomItem>();
            SearchCommand = new DelegateCommand(OnSearch, () => !IsLoading);
            AddCommand = new DelegateCommand(OnAdd, () => !IsLoading);
            EditCommand = new DelegateCommand(OnEdit, () => !IsLoading && SelectedItem != null);
            DeleteCommand = new DelegateCommand(OnDelete, () => !IsLoading && SelectedItem != null);
            ViewCommand = new DelegateCommand(OnView, () => !IsLoading && SelectedItem != null);
            TestConnectionCommand = new DelegateCommand(OnTestConnection, () => !IsLoading && SelectedItem != null);
            LoadPermissionsAsync();
            LoadRoomsAsync();
        }

        private async void LoadPermissionsAsync()
        {
            if (_currentUserService?.CurrentUser == null)
            {
                CanAdd = false;
                CanEdit = false;
                CanDelete = false;
                CanView = false;
                return;
            }

            try
            {
                var permissionsString = await _accountService.GetAccountPermissionsAsync(_currentUserService.CurrentUser.Id);
                _userPermissions.Clear();

                if (!string.IsNullOrWhiteSpace(permissionsString))
                {
                    var permissions = permissionsString.Split(',');
                    foreach (var perm in permissions)
                    {
                        var trimmedPerm = perm.Trim();
                        if (!string.IsNullOrEmpty(trimmedPerm))
                        {
                            _userPermissions.Add(trimmedPerm);
                        }
                    }
                }

                // 检查权限
                CanAdd = HasPermission("DetectionRoomManagement.Add");
                CanEdit = HasPermission("DetectionRoomManagement.Edit");
                CanDelete = HasPermission("DetectionRoomManagement.Delete");
                CanView = HasPermission("DetectionRoomManagement.View");
            }
            catch
            {
                CanAdd = false;
                CanEdit = false;
                CanDelete = false;
                CanView = false;
            }
        }

        private bool HasPermission(string permissionCode)
        {
            if (string.IsNullOrEmpty(permissionCode))
                return false;

            // 管理员拥有全部权限
            if (_currentUserService?.CurrentUser != null && _currentUserService.CurrentUser.Role == "管理员")
                return true;

            // 检查是否有精确匹配的权限
            if (_userPermissions.Contains(permissionCode))
                return true;

            // 检查是否有模块权限（例如：DetectionRoomManagement 包含 DetectionRoomManagement.Add）
            return _userPermissions.Any(p => p.StartsWith(permissionCode + ".") || p == permissionCode);
        }

        public async void LoadRoomsAsync()
        {
            IsLoading = true;
            try
            {
                var rooms = await _detectionRoomService.GetAllRoomsAsync();
                DetectionRooms.Clear();
                foreach (var room in rooms)
                {
                    DetectionRooms.Add(room);
                }
                TotalCount = DetectionRooms.Count;
            }
            catch (System.Exception ex)
            {
                CustomMessageBox.ShowError($"加载检测室失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnSearch()
        {
            // 搜索逻辑
            LoadRoomsAsync();
        }

        private async void OnAdd()
        {
            var dialog = new Views.AddDetectionRoomDialog();
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                try
                {
                    // 创建新的检测室
                    var newRoom = new DetectionRoomItem
                    {
                        RoomNo = dialog.RoomNo ?? string.Empty,
                        RoomName = dialog.RoomName ?? string.Empty,
                        Remark = dialog.Remark ?? string.Empty,
                        ScannerPortName = dialog.ScannerPortName ?? string.Empty,
                        ScannerBaudRate = dialog.ScannerBaudRate,
                        ScannerDataBits = dialog.ScannerDataBits,
                        ScannerStopBits = dialog.ScannerStopBits,
                        ScannerParity = dialog.ScannerParity ?? "None",
                        ScannerIsEnabled = dialog.ScannerIsEnabled,
                        // PLC配置 - 气缸1
                        Cylinder1ExtendAddress = dialog.Cylinder1ExtendAddress ?? string.Empty,
                        Cylinder1RetractAddress = dialog.Cylinder1RetractAddress ?? string.Empty,
                        Cylinder1ExtendFeedbackAddress = dialog.Cylinder1ExtendFeedbackAddress ?? string.Empty,
                        Cylinder1RetractFeedbackAddress = dialog.Cylinder1RetractFeedbackAddress ?? string.Empty,
                        Cylinder1DataType = dialog.Cylinder1DataType ?? "Bool",
                        // PLC配置 - 气缸2
                        Cylinder2ExtendAddress = dialog.Cylinder2ExtendAddress ?? string.Empty,
                        Cylinder2RetractAddress = dialog.Cylinder2RetractAddress ?? string.Empty,
                        Cylinder2ExtendFeedbackAddress = dialog.Cylinder2ExtendFeedbackAddress ?? string.Empty,
                        Cylinder2RetractFeedbackAddress = dialog.Cylinder2RetractFeedbackAddress ?? string.Empty,
                        Cylinder2DataType = dialog.Cylinder2DataType ?? "Bool",
                        // PLC配置 - 传感器
                        SensorAddress = dialog.SensorAddress ?? string.Empty,
                        SensorDataType = dialog.SensorDataType ?? "Bool",
                        // 反馈报警延时时间设置（直接使用对话框的值，不进行默认值替换）
                        PushCylinderRetractTimeout = dialog.PushCylinderRetractTimeout,
                        PushCylinderExtendTimeout = dialog.PushCylinderExtendTimeout,
                        BlockingCylinderRetractTimeout = dialog.BlockingCylinderRetractTimeout,
                        BlockingCylinderExtendTimeout = dialog.BlockingCylinderExtendTimeout,
                        SensorDetectTimeout = dialog.SensorDetectTimeout,
                        PassageDelayTime = dialog.PassageDelayTime,
                        SensorConfirmDelayTime = dialog.SensorConfirmDelayTime,
                        IsSelected = false
                    };
                    
                    var success = await _detectionRoomService.AddRoomAsync(newRoom);
                    if (success)
                    {
                        CustomMessageBox.ShowInformation("检测室添加成功");
                        LoadRoomsAsync();
                    }
                    else
                    {
                        CustomMessageBox.ShowError("检测室添加失败");
                    }
                }
                catch (System.Exception ex)
                {
                    CustomMessageBox.ShowError($"添加检测室失败: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        public ObservableCollection<DetectionRoomItem> DetectionRooms
        {
            get => _detectionRooms;
            set => SetProperty(ref _detectionRooms, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                SearchCommand.RaiseCanExecuteChanged();
                AddCommand.RaiseCanExecuteChanged();
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                ViewCommand.RaiseCanExecuteChanged();
                TestConnectionCommand.RaiseCanExecuteChanged();
            }
        }

        public DetectionRoomItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                ViewCommand.RaiseCanExecuteChanged();
                TestConnectionCommand.RaiseCanExecuteChanged();
            }
        }

        public event EventHandler EditRequested;
        public event EventHandler DeleteRequested;
        public event EventHandler ViewRequested;

        private void OnEdit()
        {
            if (SelectedItem != null)
            {
                EditRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnDelete()
        {
            if (SelectedItem != null)
            {
                DeleteRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnView()
        {
            if (SelectedItem != null)
            {
                ViewRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        // 权限可见性属性
        public bool CanAdd
        {
            get => _canAdd;
            set => SetProperty(ref _canAdd, value);
        }

        public bool CanEdit
        {
            get => _canEdit;
            set => SetProperty(ref _canEdit, value);
        }

        public bool CanDelete
        {
            get => _canDelete;
            set => SetProperty(ref _canDelete, value);
        }

        public bool CanView
        {
            get => _canView;
            set => SetProperty(ref _canView, value);
        }

        public DelegateCommand SearchCommand { get; private set; }
        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand EditCommand { get; private set; }
        public DelegateCommand DeleteCommand { get; private set; }
        public DelegateCommand ViewCommand { get; private set; }
        public DelegateCommand TestConnectionCommand { get; private set; }

        private async void OnTestConnection()
        {
            if (SelectedItem == null)
            {
                CustomMessageBox.ShowWarning("请选择要测试的检测室");
                return;
            }

            var room = await _detectionRoomService.GetRoomByIdAsync(SelectedItem.Id);
            if (room == null)
            {
                CustomMessageBox.ShowError("检测室不存在");
                return;
            }

            if (!room.ScannerIsEnabled || string.IsNullOrWhiteSpace(room.ScannerPortName))
            {
                CustomMessageBox.ShowWarning("该检测室未启用扫码器或未配置串口");
                return;
            }

            IsLoading = true;
            try
            {
                var (success, errorMessage) = await _scannerService.TestConnectionAsync(room);
                if (success)
                {
                    // 测试成功后，真正建立连接
                    System.Diagnostics.Debug.WriteLine($"[检测室管理] 测试连接成功，开始建立实际连接 - 检测室ID:{room.Id}");
                    var connectResult = await _scannerService.OpenConnectionAsync(room);
                    if (connectResult)
                    {
                        CustomMessageBox.ShowInformation("串口连接成功！");
                        // 更新连接状态（事件会自动触发，这里也手动更新一下确保同步）
                        SelectedItem.IsScannerConnected = true;
                        System.Diagnostics.Debug.WriteLine($"[检测室管理] 实际连接成功 - 检测室ID:{room.Id}");
                    }
                    else
                    {
                        CustomMessageBox.ShowWarning("串口连接测试成功，但建立连接失败。请检查串口是否被占用。");
                        SelectedItem.IsScannerConnected = false;
                        System.Diagnostics.Debug.WriteLine($"[检测室管理] 实际连接失败 - 检测室ID:{room.Id}");
                    }
                }
                else
                {
                    CustomMessageBox.ShowError($"串口连接测试失败\n\n{errorMessage}");
                    SelectedItem.IsScannerConnected = false;
                }
            }
            catch (System.Exception ex)
            {
                CustomMessageBox.ShowError($"测试连接失败: {ex.Message}");
                SelectedItem.IsScannerConnected = false;
                System.Diagnostics.Debug.WriteLine($"[检测室管理] 测试连接异常 - 检测室ID:{room?.Id}, 错误:{ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}

