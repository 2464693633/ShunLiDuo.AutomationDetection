using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Services;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class DetectionRoomManagementViewModel : BindableBase
    {
        private string _searchKeyword;
        private ObservableCollection<DetectionRoomItem> _detectionRooms;
        private int _totalCount;
        private bool _isLoading;
        private readonly IDetectionRoomService _detectionRoomService;

        public DetectionRoomManagementViewModel(IDetectionRoomService detectionRoomService)
        {
            _detectionRoomService = detectionRoomService;
            DetectionRooms = new ObservableCollection<DetectionRoomItem>();
            SearchCommand = new DelegateCommand(OnSearch, () => !IsLoading);
            AddCommand = new DelegateCommand(OnAdd, () => !IsLoading);
            LoadRoomsAsync();
        }

        private async void LoadRoomsAsync()
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
                MessageBox.Show($"加载检测室失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        IsSelected = false
                    };
                    
                    var success = await _detectionRoomService.AddRoomAsync(newRoom);
                    if (success)
                    {
                        MessageBox.Show("检测室添加成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadRoomsAsync();
                    }
                    else
                    {
                        MessageBox.Show("检测室添加失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"添加检测室失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            }
        }

        public DelegateCommand SearchCommand { get; private set; }
        public DelegateCommand AddCommand { get; private set; }
    }
}

