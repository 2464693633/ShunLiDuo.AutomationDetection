using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class DeviceExceptionViewModel : BindableBase
    {
        private DateTime? _startTime;
        private DateTime? _endTime;
        private string _selectedRoom;
        private ObservableCollection<DeviceExceptionItem> _deviceExceptions;
        private int _totalCount;

        public DeviceExceptionViewModel()
        {
            InitializeData();
            QueryCommand = new DelegateCommand(OnQuery);
            ExportCommand = new DelegateCommand(OnExport);
        }

        private void InitializeData()
        {
            DeviceExceptions = new ObservableCollection<DeviceExceptionItem>
            {
                new DeviceExceptionItem { Id = 1 },
                new DeviceExceptionItem { Id = 2 },
                new DeviceExceptionItem { Id = 3 }
            };
            TotalCount = 5;
        }

        private void OnQuery()
        {
            // 查询逻辑
        }

        private void OnExport()
        {
            // 导出逻辑
        }

        public DateTime? StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public DateTime? EndTime
        {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        public string SelectedRoom
        {
            get => _selectedRoom;
            set => SetProperty(ref _selectedRoom, value);
        }

        public ObservableCollection<DeviceExceptionItem> DeviceExceptions
        {
            get => _deviceExceptions;
            set => SetProperty(ref _deviceExceptions, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public DelegateCommand QueryCommand { get; private set; }
        public DelegateCommand ExportCommand { get; private set; }
    }
}

