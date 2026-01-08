using Prism.Mvvm;
using System.IO.Ports;
using System.Linq;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class AddDetectionRoomDialogViewModel : BindableBase
    {
        private string _roomNo;
        private string _roomName;
        private string _remark;
        private string _scannerPortName;
        private int _scannerBaudRate = 9600;
        private int _scannerDataBits = 8;
        private int _scannerStopBits = 1;
        private string _scannerParity = "None";
        private bool _scannerIsEnabled = false;
        
        // PLC配置 - 气缸1（阻挡气缸）
        private string _cylinder1ExtendAddress;
        private string _cylinder1RetractAddress;
        private string _cylinder1ExtendFeedbackAddress;
        private string _cylinder1RetractFeedbackAddress;
        private string _cylinder1DataType = "Bool";
        
        // PLC配置 - 气缸2（推箱气缸）
        private string _cylinder2ExtendAddress;
        private string _cylinder2RetractAddress;
        private string _cylinder2ExtendFeedbackAddress;
        private string _cylinder2RetractFeedbackAddress;
        private string _cylinder2DataType = "Bool";
        
        // PLC配置 - 传感器
        private string _sensorAddress;
        private string _sensorDataType = "Bool";
        
        // 反馈报警延时时间设置（单位：毫秒）
        private int _pushCylinderRetractTimeout = 30000;      // 推箱气缸收缩超时（匹配流程）
        private int _pushCylinderExtendTimeout = 30000;      // 推箱气缸伸出超时（不匹配流程）
        private int _blockingCylinderRetractTimeout = 30000;  // 阻挡气缸收缩超时
        private int _blockingCylinderExtendTimeout = 30000;   // 阻挡气缸伸出超时
        private int _sensorDetectTimeout = 15000;            // 传感器检测超时
        private int _passageDelayTime = 5000;                // 放行等待时间（不匹配流程）
        private int _sensorConfirmDelayTime = 3000;          // 传感器确认延时（匹配流程）

        public AddDetectionRoomDialogViewModel(Models.DetectionRoomItem room = null)
        {
            if (room != null)
            {
                RoomNo = room.RoomNo ?? string.Empty;
                RoomName = room.RoomName ?? string.Empty;
                Remark = room.Remark ?? string.Empty;
                ScannerPortName = room.ScannerPortName ?? string.Empty;
                ScannerBaudRate = room.ScannerBaudRate;
                ScannerDataBits = room.ScannerDataBits;
                ScannerStopBits = room.ScannerStopBits;
                ScannerParity = room.ScannerParity ?? "None";
                ScannerIsEnabled = room.ScannerIsEnabled;
                
                // 加载PLC配置
                Cylinder1ExtendAddress = room.Cylinder1ExtendAddress ?? string.Empty;
                Cylinder1RetractAddress = room.Cylinder1RetractAddress ?? string.Empty;
                Cylinder1ExtendFeedbackAddress = room.Cylinder1ExtendFeedbackAddress ?? string.Empty;
                Cylinder1RetractFeedbackAddress = room.Cylinder1RetractFeedbackAddress ?? string.Empty;
                Cylinder1DataType = room.Cylinder1DataType ?? "Bool";
                Cylinder2ExtendAddress = room.Cylinder2ExtendAddress ?? string.Empty;
                Cylinder2RetractAddress = room.Cylinder2RetractAddress ?? string.Empty;
                Cylinder2ExtendFeedbackAddress = room.Cylinder2ExtendFeedbackAddress ?? string.Empty;
                Cylinder2RetractFeedbackAddress = room.Cylinder2RetractFeedbackAddress ?? string.Empty;
                Cylinder2DataType = room.Cylinder2DataType ?? "Bool";
                SensorAddress = room.SensorAddress ?? string.Empty;
                SensorDataType = room.SensorDataType ?? "Bool";
                
                // 加载超时时间配置（直接使用数据库中的值，不进行默认值替换）
                PushCylinderRetractTimeout = room.PushCylinderRetractTimeout;
                PushCylinderExtendTimeout = room.PushCylinderExtendTimeout;
                BlockingCylinderRetractTimeout = room.BlockingCylinderRetractTimeout;
                BlockingCylinderExtendTimeout = room.BlockingCylinderExtendTimeout;
                SensorDetectTimeout = room.SensorDetectTimeout;
                PassageDelayTime = room.PassageDelayTime;
                SensorConfirmDelayTime = room.SensorConfirmDelayTime;
            }

            // 初始化可用串口列表
            AvailablePorts = SerialPort.GetPortNames().OrderBy(p => p).ToList();
            AvailableBaudRates = new int[] { 9600, 19200, 38400, 57600, 115200 };
            AvailableDataBits = new int[] { 7, 8 };
            AvailableStopBits = new int[] { 1, 2 };
            AvailableParities = new string[] { "None", "Odd", "Even", "Mark", "Space" };
        }

        public string RoomNo
        {
            get => _roomNo;
            set => SetProperty(ref _roomNo, value);
        }

        public string RoomName
        {
            get => _roomName;
            set => SetProperty(ref _roomName, value);
        }

        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        public string ScannerPortName
        {
            get => _scannerPortName;
            set => SetProperty(ref _scannerPortName, value);
        }

        public int ScannerBaudRate
        {
            get => _scannerBaudRate;
            set => SetProperty(ref _scannerBaudRate, value);
        }

        public int ScannerDataBits
        {
            get => _scannerDataBits;
            set => SetProperty(ref _scannerDataBits, value);
        }

        public int ScannerStopBits
        {
            get => _scannerStopBits;
            set => SetProperty(ref _scannerStopBits, value);
        }

        public string ScannerParity
        {
            get => _scannerParity;
            set => SetProperty(ref _scannerParity, value);
        }

        public bool ScannerIsEnabled
        {
            get => _scannerIsEnabled;
            set => SetProperty(ref _scannerIsEnabled, value);
        }

        public System.Collections.Generic.List<string> AvailablePorts { get; private set; }
        public int[] AvailableBaudRates { get; private set; }
        public int[] AvailableDataBits { get; private set; }
        public int[] AvailableStopBits { get; private set; }
        public string[] AvailableParities { get; private set; }
        
        // PLC配置 - 气缸1
        public string Cylinder1ExtendAddress
        {
            get => _cylinder1ExtendAddress;
            set => SetProperty(ref _cylinder1ExtendAddress, value);
        }

        public string Cylinder1RetractAddress
        {
            get => _cylinder1RetractAddress;
            set => SetProperty(ref _cylinder1RetractAddress, value);
        }

        public string Cylinder1ExtendFeedbackAddress
        {
            get => _cylinder1ExtendFeedbackAddress;
            set => SetProperty(ref _cylinder1ExtendFeedbackAddress, value);
        }

        public string Cylinder1RetractFeedbackAddress
        {
            get => _cylinder1RetractFeedbackAddress;
            set => SetProperty(ref _cylinder1RetractFeedbackAddress, value);
        }

        public string Cylinder1DataType
        {
            get => _cylinder1DataType;
            set => SetProperty(ref _cylinder1DataType, value);
        }
        
        // PLC配置 - 气缸2
        public string Cylinder2ExtendAddress
        {
            get => _cylinder2ExtendAddress;
            set => SetProperty(ref _cylinder2ExtendAddress, value);
        }

        public string Cylinder2RetractAddress
        {
            get => _cylinder2RetractAddress;
            set => SetProperty(ref _cylinder2RetractAddress, value);
        }

        public string Cylinder2ExtendFeedbackAddress
        {
            get => _cylinder2ExtendFeedbackAddress;
            set => SetProperty(ref _cylinder2ExtendFeedbackAddress, value);
        }

        public string Cylinder2RetractFeedbackAddress
        {
            get => _cylinder2RetractFeedbackAddress;
            set => SetProperty(ref _cylinder2RetractFeedbackAddress, value);
        }

        public string Cylinder2DataType
        {
            get => _cylinder2DataType;
            set => SetProperty(ref _cylinder2DataType, value);
        }
        
        // PLC配置 - 传感器
        public string SensorAddress
        {
            get => _sensorAddress;
            set => SetProperty(ref _sensorAddress, value);
        }

        public string SensorDataType
        {
            get => _sensorDataType;
            set => SetProperty(ref _sensorDataType, value);
        }
        
        // 反馈报警延时时间设置
        public int PushCylinderRetractTimeout
        {
            get => _pushCylinderRetractTimeout;
            set => SetProperty(ref _pushCylinderRetractTimeout, value);
        }

        public int PushCylinderExtendTimeout
        {
            get => _pushCylinderExtendTimeout;
            set => SetProperty(ref _pushCylinderExtendTimeout, value);
        }

        public int BlockingCylinderRetractTimeout
        {
            get => _blockingCylinderRetractTimeout;
            set
            {
                System.Diagnostics.Debug.WriteLine($"[ViewModel] BlockingCylinderRetractTimeout 设置: {_blockingCylinderRetractTimeout} -> {value}");
                SetProperty(ref _blockingCylinderRetractTimeout, value);
            }
        }

        public int BlockingCylinderExtendTimeout
        {
            get => _blockingCylinderExtendTimeout;
            set
            {
                System.Diagnostics.Debug.WriteLine($"[ViewModel] BlockingCylinderExtendTimeout 设置: {_blockingCylinderExtendTimeout} -> {value}");
                SetProperty(ref _blockingCylinderExtendTimeout, value);
            }
        }

        public int SensorDetectTimeout
        {
            get => _sensorDetectTimeout;
            set => SetProperty(ref _sensorDetectTimeout, value);
        }

        public int PassageDelayTime
        {
            get => _passageDelayTime;
            set => SetProperty(ref _passageDelayTime, value);
        }

        public int SensorConfirmDelayTime
        {
            get => _sensorConfirmDelayTime;
            set => SetProperty(ref _sensorConfirmDelayTime, value);
        }
    }
}

