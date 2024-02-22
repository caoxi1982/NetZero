#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.DataLogger;
using FTOptix.EventLogger;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.SQLiteStore;
using FTOptix.Report;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CommunicationDriver;
using FTOptix.UI;
using FTOptix.Core;
#endregion

public class StartUpDemo : BaseNetLogic
{
    public override void Start()
    {
        myDelayedTask = new DelayedTask(ResetLabelText, 5000, LogicObject);
        myDelayedTask.Start();
    }

    public override void Stop()
    {
        myDelayedTask.Dispose();
    }
    private void ResetLabelText()
    {
        var appstarted5seconds = Project.Current.GetVariable("Model/SystemVariables/AppStarted5Seconds");
        appstarted5seconds.Value = true;
        Project.Current.GetVariable("Model/SystemVariables/Yesterday").Value = DateTime.Now.AddDays(-1);
    }

    private DelayedTask myDelayedTask;
}
