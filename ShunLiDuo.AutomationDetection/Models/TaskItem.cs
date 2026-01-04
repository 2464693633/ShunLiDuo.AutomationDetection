using System;

namespace ShunLiDuo.AutomationDetection.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string LogisticsBoxCode { get; set; }
        public string InspectorName { get; set; }
        public DateTime? StartTime { get; set; }
        public string Room1Status { get; set; }
        public string Room2Status { get; set; }
        public string Room3Status { get; set; }
        public string Room4Status { get; set; }
        public string Room5Status { get; set; }
        public DateTime? EndTime { get; set; }
    }
}

