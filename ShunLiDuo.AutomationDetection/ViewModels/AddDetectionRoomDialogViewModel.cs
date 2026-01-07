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
    }
}

