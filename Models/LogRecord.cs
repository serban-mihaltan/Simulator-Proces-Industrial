using System;
namespace MonitorApi.Models
{
    public class LogRecord
    {
        public int Id { get; set; }
        public string State { get; set; }
        public DateTime Timestamp { get; set; }
    }
}