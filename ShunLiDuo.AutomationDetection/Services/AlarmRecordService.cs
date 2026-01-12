using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Data;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public class AlarmRecordService : IAlarmRecordService
    {
        private readonly IAlarmRecordRepository _alarmRecordRepository;

        public AlarmRecordService(IAlarmRecordRepository alarmRecordRepository)
        {
            _alarmRecordRepository = alarmRecordRepository;
        }

        public async Task<List<AlarmRecord>> GetAllAlarmsAsync()
        {
            return await _alarmRecordRepository.GetAllAlarmsAsync();
        }

        public async Task<List<AlarmRecord>> SearchAlarmsAsync(string keyword, int? roomId, DateTime? startTime, DateTime? endTime)
        {
            return await _alarmRecordRepository.SearchAlarmsAsync(keyword, roomId, startTime, endTime);
        }

        public async Task<List<AlarmRecord>> GetUnhandledAlarmsAsync()
        {
            return await _alarmRecordRepository.GetUnhandledAlarmsAsync();
        }

        public async Task<AlarmRecord> GetAlarmByIdAsync(int id)
        {
            return await _alarmRecordRepository.GetAlarmByIdAsync(id);
        }

        public async Task<bool> AddAlarmAsync(AlarmRecord alarm)
        {
            if (alarm == null)
                return false;

            if (string.IsNullOrWhiteSpace(alarm.AlarmCode))
            {
                alarm.AlarmCode = $"AL{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
            }

            if (string.IsNullOrWhiteSpace(alarm.Status))
            {
                alarm.Status = "未处理";
            }

            if (alarm.CreateTime == default(DateTime))
            {
                alarm.CreateTime = DateTime.Now;
            }

            try
            {
                await _alarmRecordRepository.InsertAlarmAsync(alarm);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[报警记录服务] 添加报警记录失败: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAlarmAsync(AlarmRecord alarm)
        {
            if (alarm == null)
                return false;

            try
            {
                return await _alarmRecordRepository.UpdateAlarmAsync(alarm);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[报警记录服务] 更新报警记录失败: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteAlarmAsync(int id)
        {
            try
            {
                return await _alarmRecordRepository.DeleteAlarmAsync(id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[报警记录服务] 删除报警记录失败: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> HandleAlarmAsync(int id, string handler, string remark)
        {
            try
            {
                return await _alarmRecordRepository.HandleAlarmAsync(id, handler, remark);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[报警记录服务] 处理报警记录失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 统一报警记录方法 - 供各个报警点调用
        /// </summary>
        public async Task RecordAlarmAsync(string alarmTitle, string alarmMessage,
            int? roomId = null, string roomName = null, string deviceName = null, string remark = null)
        {
            // 如果 roomName 为空，尝试从报警信息中提取检测室名称
            string extractedRoomName = roomName;
            if (string.IsNullOrWhiteSpace(extractedRoomName) && !string.IsNullOrWhiteSpace(alarmMessage))
            {
                // 尝试匹配"检测室X"或"检测室 X"的格式
                var match = Regex.Match(alarmMessage, @"检测室\s*(\d+|[一二三四五六七八九十]+|[A-Za-z]+)");
                if (match.Success)
                {
                    extractedRoomName = match.Value; // 提取完整的"检测室X"字符串
                }
                else
                {
                    // 尝试匹配其他可能的检测室名称格式（如"检测室1"、"检测室一"等）
                    var roomMatch = Regex.Match(alarmMessage, @"(检测室[^\s，,。]+)");
                    if (roomMatch.Success)
                    {
                        extractedRoomName = roomMatch.Groups[1].Value;
                    }
                }
            }

            // 根据报警标题自动判断报警类型
            string alarmType = "系统报警"; // 默认类型
            string alarmLevel = "警告"; // 默认级别
            if (!string.IsNullOrWhiteSpace(alarmTitle))
            {
                if (alarmTitle.Contains("超时"))
                {
                    alarmType = "超时报警";
                    alarmLevel = "警告";
                }
                else if (alarmTitle.Contains("失败") || alarmTitle.Contains("错误"))
                {
                    alarmType = "设备故障";
                    alarmLevel = "严重";
                }
                else if (alarmTitle.Contains("配置") || alarmTitle.Contains("未配置"))
                {
                    alarmType = "配置错误";
                    alarmLevel = "严重";
                }
                else if (alarmTitle.Contains("阻止") || alarmTitle.Contains("未找到"))
                {
                    alarmType = "业务异常";
                    alarmLevel = "警告";
                }
            }

            var alarm = new AlarmRecord
            {
                AlarmTitle = alarmTitle ?? "报警",
                AlarmMessage = alarmMessage ?? "",
                AlarmType = alarmType,
                AlarmLevel = alarmLevel,
                RoomId = roomId,
                RoomName = extractedRoomName ?? "",
                DeviceName = "", // 不再使用设备名称字段
                Status = "未处理",
                CreateTime = DateTime.Now,
                Remark = remark ?? ""
            };

            // 异步记录，不阻塞调用者
            _ = Task.Run(async () =>
            {
                try
                {
                    await AddAlarmAsync(alarm);
                    System.Diagnostics.Debug.WriteLine($"[报警记录] 已记录报警: {alarmTitle} - {alarmMessage}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[报警记录] 记录报警失败: {ex.Message}");
                }
            });
        }
    }
}

