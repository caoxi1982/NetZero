using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FTOptix.AuditSigning;
using FTOptix.Recipe;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.UI;
using FTOptix.OPCUAServer;
using FTOptix.WebUI;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;

namespace NetZero
{
    enum ShiftEnum : uint
    {
     Shift1 = 1,
     Shift2 = 2,
     Shift3 = 3,
     Valley = 10, //* 10:  0:00-8:00 
     Normal1 = 20, //* 20:  11:00-14:00 
     Normal2 = 21,  //* 21   15:00-18:00 
     Normal3 = 22,//* 22   22:00-24:00 
     High1 = 30,//* 30:  8:00-10:00 
     High2 = 31,//* 31   18:00-22:00 
     Peak1 = 40,//* 40:  10:00-11:00 
     Peak2 = 41//* 41:  14:00-15:00 
    }
}
