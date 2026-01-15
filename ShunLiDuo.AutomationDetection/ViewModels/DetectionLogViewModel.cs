using Prism.Mvvm;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using Microsoft.Win32;
using ClosedXML.Excel;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Services;
using ShunLiDuo.AutomationDetection.Views;

namespace ShunLiDuo.AutomationDetection.ViewModels
{
    public class DetectionLogViewModel : BindableBase
    {
        private readonly IDetectionLogService _detectionLogService;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private string _selectedRoom;
        private string _searchKeyword;
        private ObservableCollection<DetectionLogItem> _detectionLogs;
        private ObservableCollection<string> _roomNames;
        private int _totalCount;
        private bool _isLoading;

        public DetectionLogViewModel(IDetectionLogService detectionLogService)
        {
            _detectionLogService = detectionLogService;
            DetectionLogs = new ObservableCollection<DetectionLogItem>();
            RoomNames = new ObservableCollection<string>();
            QueryCommand = new DelegateCommand(OnQuery, () => !_isLoading);
            ExportCommand = new DelegateCommand(OnExport, () => !_isLoading);
            RefreshCommand = new DelegateCommand(OnRefresh, () => !_isLoading);
            
            // 初始化时加载数据
            LoadLogsAsync();
        }

        private async void LoadLogsAsync()
        {
            IsLoading = true;
            try
            {
                var logs = await _detectionLogService.GetAllLogsAsync();
                DetectionLogs.Clear();
                foreach (var log in logs)
                {
                    DetectionLogs.Add(log);
                }
                TotalCount = DetectionLogs.Count;
                
                // 更新检测室名称列表
                UpdateRoomNames();
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"加载检测历史记录失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateRoomNames()
        {
            var rooms = DetectionLogs
                .Where(log => !string.IsNullOrWhiteSpace(log.RoomName))
                .Select(log => log.RoomName)
                .Distinct()
                .OrderBy(r => r)
                .ToList();
            
            RoomNames.Clear();
            RoomNames.Add("全部");
            foreach (var room in rooms)
            {
                RoomNames.Add(room);
            }
        }

        private async void OnQuery()
        {
            IsLoading = true;
            try
            {
                var logs = await _detectionLogService.GetAllLogsAsync();
                
                // 应用筛选条件
                var filteredLogs = logs.AsEnumerable();
                
                if (StartTime.HasValue)
                {
                    filteredLogs = filteredLogs.Where(log => 
                        log.CreateTime >= StartTime.Value.Date);
                }
                
                if (EndTime.HasValue)
                {
                    filteredLogs = filteredLogs.Where(log => 
                        log.CreateTime <= EndTime.Value.Date.AddDays(1).AddSeconds(-1));
                }
                
                if (!string.IsNullOrWhiteSpace(SelectedRoom) && SelectedRoom != "全部")
                {
                    filteredLogs = filteredLogs.Where(log => log.RoomName == SelectedRoom);
                }
                
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    filteredLogs = filteredLogs.Where(log => 
                        (log.LogisticsBoxCode != null && log.LogisticsBoxCode.Contains(SearchKeyword)) ||
                        (log.WorkOrderNo != null && log.WorkOrderNo.Contains(SearchKeyword)) ||
                        (log.RoomName != null && log.RoomName.Contains(SearchKeyword)));
                }
                
                DetectionLogs.Clear();
                foreach (var log in filteredLogs.OrderByDescending(l => l.CreateTime))
                {
                    DetectionLogs.Add(log);
                }
                TotalCount = DetectionLogs.Count;
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"查询检测历史记录失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnExport()
        {
            try
            {
                if (DetectionLogs == null || DetectionLogs.Count == 0)
                {
                    CustomMessageBox.ShowWarning("没有数据可导出");
                    return;
                }

                // 打开保存文件对话框
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
                    FileName = $"检测记录_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    DefaultExt = "xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    IsLoading = true;
                    
                    // 在后台线程执行导出操作
                    Task.Run(() =>
                    {
                        try
                        {
                            ExportToExcel(DetectionLogs.ToList(), saveFileDialog.FileName);
                            
                            // 在UI线程显示成功消息
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                CustomMessageBox.ShowInformation($"导出成功！\n文件已保存至：{saveFileDialog.FileName}", "导出完成");
                            });
                        }
                        catch (Exception ex)
                        {
                            // 在UI线程显示错误消息
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                CustomMessageBox.ShowError($"导出失败：{ex.Message}");
                            });
                        }
                        finally
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                IsLoading = false;
                            });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"导出失败：{ex.Message}");
                IsLoading = false;
            }
        }

        private void ExportToExcel(System.Collections.Generic.List<DetectionLogItem> logs, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("检测记录");

                // 设置标题行
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRow.Height = 25;

                // 设置列标题
                worksheet.Cell(1, 1).Value = "序号";
                worksheet.Cell(1, 2).Value = "物流盒编码";
                worksheet.Cell(1, 3).Value = "报工单编号";
                worksheet.Cell(1, 4).Value = "检测室";
                worksheet.Cell(1, 5).Value = "状态";
                worksheet.Cell(1, 6).Value = "检测开始时间";
                worksheet.Cell(1, 7).Value = "检测完成时间";
                worksheet.Cell(1, 8).Value = "创建时间";
                worksheet.Cell(1, 9).Value = "备注";

                // 设置列宽
                worksheet.Column(1).Width = 10;  // 序号
                worksheet.Column(2).Width = 25; // 物流盒编码
                worksheet.Column(3).Width = 20; // 报工单编号
                worksheet.Column(4).Width = 15;  // 检测室
                worksheet.Column(5).Width = 15; // 状态
                worksheet.Column(6).Width = 20; // 检测开始时间
                worksheet.Column(7).Width = 20; // 检测完成时间
                worksheet.Column(8).Width = 20; // 创建时间
                worksheet.Column(9).Width = 30; // 备注

                // 填充数据
                for (int i = 0; i < logs.Count; i++)
                {
                    var log = logs[i];
                    var row = i + 2; // 从第2行开始（第1行是标题）

                    worksheet.Cell(row, 1).Value = log.Id;
                    worksheet.Cell(row, 2).Value = log.LogisticsBoxCode ?? "";
                    worksheet.Cell(row, 3).Value = log.WorkOrderNo ?? "";
                    worksheet.Cell(row, 4).Value = log.RoomName ?? "";
                    worksheet.Cell(row, 5).Value = log.Status ?? "";
                    worksheet.Cell(row, 6).Value = log.StartTime.HasValue ? log.StartTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
                    worksheet.Cell(row, 7).Value = log.EndTime.HasValue ? log.EndTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
                    worksheet.Cell(row, 8).Value = log.CreateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cell(row, 9).Value = log.Remark ?? "";

                    // 设置数据行样式
                    var dataRow = worksheet.Row(row);
                    dataRow.Height = 20;
                    dataRow.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    
                    // 根据状态设置不同的背景色
                    if (log.Status == "检测完成")
                    {
                        dataRow.Style.Fill.BackgroundColor = XLColor.LightGreen;
                    }
                    else if (log.Status == "检测中")
                    {
                        dataRow.Style.Fill.BackgroundColor = XLColor.LightYellow;
                    }
                    else if (log.Status == "未检测")
                    {
                        dataRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                    }
                }

                // 设置边框
                var range = worksheet.Range(1, 1, logs.Count + 1, 8);
                range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                range.Style.Border.OutsideBorderColor = XLColor.Black;
                range.Style.Border.InsideBorderColor = XLColor.Gray;

                // 冻结首行
                worksheet.SheetView.FreezeRows(1);

                // 保存文件
                workbook.SaveAs(filePath);
            }
        }

        private void OnRefresh()
        {
            LoadLogsAsync();
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

        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        public ObservableCollection<DetectionLogItem> DetectionLogs
        {
            get => _detectionLogs;
            set => SetProperty(ref _detectionLogs, value);
        }

        public ObservableCollection<string> RoomNames
        {
            get => _roomNames;
            set => SetProperty(ref _roomNames, value);
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
                QueryCommand?.RaiseCanExecuteChanged();
                ExportCommand?.RaiseCanExecuteChanged();
                RefreshCommand?.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand QueryCommand { get; private set; }
        public DelegateCommand ExportCommand { get; private set; }
        public DelegateCommand RefreshCommand { get; private set; }
    }
}

