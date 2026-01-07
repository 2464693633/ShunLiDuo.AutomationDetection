using System;
using Prism.Mvvm;

namespace ShunLiDuo.AutomationDetection.Models
{
    public class PlcMonitorConfigItem : BindableBase
    {
        private int _id;
        private int _roomId;
        private string _roomName;
        private string _roomNo;
        
        // 气缸1配置
        private string _cylinder1Name;
        private string _cylinder1ExtendAddress;
        private string _cylinder1RetractAddress;
        private string _cylinder1ExtendFeedbackAddress;
        private string _cylinder1RetractFeedbackAddress;
        private string _cylinder1DataType;
        
        // 气缸2配置
        private string _cylinder2Name;
        private string _cylinder2ExtendAddress;
        private string _cylinder2RetractAddress;
        private string _cylinder2ExtendFeedbackAddress;
        private string _cylinder2RetractFeedbackAddress;
        private string _cylinder2DataType;
        
        // 传感器配置
        private string _sensorName;
        private string _sensorAddress;
        private string _sensorDataType;
        
        private string _remark;
        private DateTime _createTime;
        private DateTime? _updateTime;
        
        // 实时监控值（不在数据库中）
        private object _cylinder1ExtendValue;
        private object _cylinder1RetractValue;
        private object _cylinder1ExtendFeedbackValue;
        private object _cylinder1RetractFeedbackValue;
        private string _cylinder1Status;
        
        private object _cylinder2ExtendValue;
        private object _cylinder2RetractValue;
        private object _cylinder2ExtendFeedbackValue;
        private object _cylinder2RetractFeedbackValue;
        private string _cylinder2Status;
        
        private object _sensorValue;
        private string _sensorStatus;
        private DateTime? _lastUpdateTime;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public int RoomId
        {
            get => _roomId;
            set => SetProperty(ref _roomId, value);
        }

        public string RoomName
        {
            get => _roomName;
            set => SetProperty(ref _roomName, value);
        }

        public string RoomNo
        {
            get => _roomNo;
            set => SetProperty(ref _roomNo, value);
        }

        public string Cylinder1Name
        {
            get => _cylinder1Name;
            set => SetProperty(ref _cylinder1Name, value);
        }

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

        public string Cylinder2Name
        {
            get => _cylinder2Name;
            set => SetProperty(ref _cylinder2Name, value);
        }

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

        public string SensorName
        {
            get => _sensorName;
            set => SetProperty(ref _sensorName, value);
        }

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

        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        public DateTime CreateTime
        {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }

        public DateTime? UpdateTime
        {
            get => _updateTime;
            set => SetProperty(ref _updateTime, value);
        }

        public object Cylinder1ExtendValue
        {
            get => _cylinder1ExtendValue;
            set => SetProperty(ref _cylinder1ExtendValue, value);
        }

        public object Cylinder1RetractValue
        {
            get => _cylinder1RetractValue;
            set => SetProperty(ref _cylinder1RetractValue, value);
        }

        public object Cylinder1ExtendFeedbackValue
        {
            get => _cylinder1ExtendFeedbackValue;
            set => SetProperty(ref _cylinder1ExtendFeedbackValue, value);
        }

        public object Cylinder1RetractFeedbackValue
        {
            get => _cylinder1RetractFeedbackValue;
            set => SetProperty(ref _cylinder1RetractFeedbackValue, value);
        }

        public string Cylinder1Status
        {
            get => _cylinder1Status;
            set => SetProperty(ref _cylinder1Status, value);
        }

        public object Cylinder2ExtendValue
        {
            get => _cylinder2ExtendValue;
            set => SetProperty(ref _cylinder2ExtendValue, value);
        }

        public object Cylinder2RetractValue
        {
            get => _cylinder2RetractValue;
            set => SetProperty(ref _cylinder2RetractValue, value);
        }

        public object Cylinder2ExtendFeedbackValue
        {
            get => _cylinder2ExtendFeedbackValue;
            set => SetProperty(ref _cylinder2ExtendFeedbackValue, value);
        }

        public object Cylinder2RetractFeedbackValue
        {
            get => _cylinder2RetractFeedbackValue;
            set => SetProperty(ref _cylinder2RetractFeedbackValue, value);
        }

        public string Cylinder2Status
        {
            get => _cylinder2Status;
            set => SetProperty(ref _cylinder2Status, value);
        }

        public object SensorValue
        {
            get => _sensorValue;
            set => SetProperty(ref _sensorValue, value);
        }

        public string SensorStatus
        {
            get => _sensorStatus;
            set => SetProperty(ref _sensorStatus, value);
        }

        public DateTime? LastUpdateTime
        {
            get => _lastUpdateTime;
            set => SetProperty(ref _lastUpdateTime, value);
        }
    }
}

