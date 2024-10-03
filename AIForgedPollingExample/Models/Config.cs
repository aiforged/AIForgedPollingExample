using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIForgedPollingExample.Models
{
    public interface IConfig
    {
        string EndPoint { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        string ApiKey { get; set; }
        int ProjectId { get; set; }
        int ServiceId { get; set; }
        TimeSpan StartDateTimeSpan { get; set; }
        TimeSpan Interval { get; set; }
    }

    public partial class Config : IConfig
    {
        public string EndPoint { get; set; } = "https://portal.aiforged.com";
        public string Username { get; set; }
        public string Password { get; set; }
        public string ApiKey { get; set; }
        public int ProjectId { get; set; }
        public int ServiceId { get; set; }
        public TimeSpan StartDateTimeSpan { get; set; }
        public TimeSpan Interval { get; set; }
    }
}
