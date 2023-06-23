using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FTOptix.Modbus;
using FTOptix.CommunicationDriver;
using FTOptix.AuditSigning;
using FTOptix.Recipe;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.UI;
using FTOptix.OPCUAServer;
using FTOptix.WebUI;
using FTOptix.RAEtherNetIP;
using FTOptix.SQLiteStore;
using FTOptix.OPCUAClient;

namespace NetZero
{
    internal class TimeDuration
    {
        private TimeOnly Start { get; }
        private TimeOnly End { get; }

        public TimeDuration(TimeOnly start,TimeOnly end) 
        {
            Start = start;
            End = end;
        }
        public bool IsInTimeRange(TimeOnly time)
        {
            if (Start < End)
            {
                return time >= Start && time <= End;
            }
            else
            {
                return false;
            }
        }
    }
}
