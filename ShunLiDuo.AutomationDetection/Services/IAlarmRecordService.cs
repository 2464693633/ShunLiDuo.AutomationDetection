using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Services
{
    public interface IAlarmRecordService
    {
        Task<List<AlarmRecord>> GetAllAlarmsAsync();
        Task<List<AlarmRecord>> SearchAlarmsAsync(string keyword, int? roomId, DateTime? startTime, DateTime? endTime);
        Task<List<AlarmRecord>> GetUnhandledAlarmsAsync();
        Task<AlarmRecord> GetAlarmByIdAsync(int id);
        Task<bool> AddAlarmAsync(AlarmRecord alarm);
        Task<bool> UpdateAlarmAsync(AlarmRecord alarm);
        Task<bool> DeleteAlarmAsync(int id);
        Task<bool> HandleAlarmAsync(int id, string handler, string remark);
        
        // 统一报警记录方法
        Task RecordAlarmAsync(string alarmTitle, string alarmMessage, 
            int? roomId = null, string roomName = null, string deviceName = null, string remark = null);
    }
}

