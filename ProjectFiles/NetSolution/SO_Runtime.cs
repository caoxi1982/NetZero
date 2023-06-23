#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Alarm;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.DataLogger;
using FTOptix.EventLogger;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.SQLiteStore;
using FTOptix.Report;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.UI;
using FTOptix.Core;
using FTOptix.OPCUAClient;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
#endregion

public class SO_Runtime : BaseNetLogic
{
    private NetZero.SO.Base runtimeBase;
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        runtimeBase = new NetZero.SO.Base(Owner);
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        runtimeBase.Stop();
    }
}
