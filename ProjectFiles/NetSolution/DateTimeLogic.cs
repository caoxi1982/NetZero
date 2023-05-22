#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.DataLogger;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.Core;
using FTOptix.AuditSigning;
using FTOptix.Recipe;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.OPCUAServer;
using FTOptix.WebUI;
#endregion

public class DateTimeLogic : BaseNetLogic
{
    public override void Start()
    {
        getYearMonthDay();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
    [ExportMethod]
    public void getYearMonthDay()
    {
        var dt = (DateTime)Owner.GetVariable("Value").Value;
        Owner.GetVariable("Year").Value = dt.Year;
        Owner.GetVariable("Month").Value = dt.Month;
        Owner.GetVariable("Day").Value = dt.Day;
    }
}
