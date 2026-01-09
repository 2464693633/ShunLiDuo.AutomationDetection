using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShunLiDuo.AutomationDetection.Models;

namespace ShunLiDuo.AutomationDetection.Data
{
    public interface IAlarmRecordRepository
    {
        Task<List<AlarmRecord>> GetAllAlarmsAsync();
        Task<List<AlarmRecord>> SearchAlarmsAsync(string keyword, int? roomId, DateTime? startTime, DateTime? endTime);
        Task<List<AlarmRecord>> GetUnhandledAlarmsAsync();
        Task<AlarmRecord> GetAlarmByIdAsync(int id);
        Task<AlarmRecord> GetAlarmByCodeAsync(string alarmCode);
        Task<int> InsertAlarmAsync(AlarmRecord alarm);
        Task<bool> UpdateAlarmAsync(AlarmRecord alarm);
        Task<bool> DeleteAlarmAsync(int id);
        Task<bool> HandleAlarmAsync(int id, string handler, string remark);
    }
}

