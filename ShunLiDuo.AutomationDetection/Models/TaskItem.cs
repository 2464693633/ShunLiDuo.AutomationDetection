using System;
using System.Collections.Generic;
using Prism.Mvvm;

namespace ShunLiDuo.AutomationDetection.Models
{
    public class TaskItem : BindableBase
    {
        private int _id;
        private string _logisticsBoxCode;
        private string _workOrderNo;
        private string _inspectorName;
        private DateTime? _startTime;
        private string _room1Status;
        private string _room2Status;
        private string _room3Status;
        private string _room4Status;
        private string _room5Status;
        private DateTime? _endTime;
        private int? _assignedRoomId;
        private Dictionary<int, int> _roomScanCounts;

        public TaskItem()
        {
            RoomScanCounts = new Dictionary<int, int>();
        }

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string LogisticsBoxCode
        {
            get => _logisticsBoxCode;
            set => SetProperty(ref _logisticsBoxCode, value);
        }

        public string WorkOrderNo
        {
            get => _workOrderNo;
            set => SetProperty(ref _workOrderNo, value);
        }

        public string InspectorName
        {
            get => _inspectorName;
            set => SetProperty(ref _inspectorName, value);
        }

        public DateTime? StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public string Room1Status
        {
            get => _room1Status;
            set => SetProperty(ref _room1Status, value);
        }

        public string Room2Status
        {
            get => _room2Status;
            set => SetProperty(ref _room2Status, value);
        }

        public string Room3Status
        {
            get => _room3Status;
            set => SetProperty(ref _room3Status, value);
        }

        public string Room4Status
        {
            get => _room4Status;
            set => SetProperty(ref _room4Status, value);
        }

        public string Room5Status
        {
            get => _room5Status;
            set => SetProperty(ref _room5Status, value);
        }

        public DateTime? EndTime
        {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        public int? AssignedRoomId
        {
            get => _assignedRoomId;
            set => SetProperty(ref _assignedRoomId, value);
        }

        public Dictionary<int, int> RoomScanCounts
        {
            get => _roomScanCounts;
            set => SetProperty(ref _roomScanCounts, value);
        }
    }
}

