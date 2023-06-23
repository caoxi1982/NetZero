#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.DataLogger;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.System;
using FTOptix.UI;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.Core;
using FTOptix.Modbus;
using FTOptix.CommunicationDriver;
using FTOptix.AuditSigning;
using FTOptix.Recipe;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.OPCUAServer;
using FTOptix.WebUI;
using FTOptix.RAEtherNetIP;
using FTOptix.SQLiteStore;
using FTOptix.OPCUAClient;
#endregion

public class UpTimeLogic : BaseNetLogic
{
    private PeriodicTask myPeriodicTask;
    private Int16 status;
    public override void Start()
    {
        status = (Int16)Owner.GetVariable("Status").Value;
        myPeriodicTask = new PeriodicTask(RandomUpTime, 5000, LogicObject);
        myPeriodicTask.Start();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    private void RandomUpTime()
    {
        var r = new Random();
        var uptime = Owner.GetVariable("UpTime");
        switch (status) {
            case 6:
                uptime.Value = 80 + 5 * (float)r.NextDouble();
                break;
            case 10:
                uptime.Value = 95 + 2 * (float)r.NextDouble();
                break;
            case 9:
                uptime.Value = 92 + 2 * (float)r.NextDouble();
                break;
            case -1:
                uptime.Value = 87 + 2 * (float)r.NextDouble();
                break;
            default:
                uptime.Value = 90 + 2 * (float)r.NextDouble();
                break;
        }
    }

}
