#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.Retentivity;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.DataLogger;
using FTOptix.Store;
using System.Threading;
using System.Timers;
using FTOptix.ODBCStore;
using FTOptix.Alarm;
using FTOptix.System;
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

public class RunCounterMeter : BaseNetLogic
{
    private PeriodicTask timer1;
    private string metername;
    public override void Start()
    {
        timer1 = new PeriodicTask(GeneratorValue, 10000, LogicObject);
        timer1.Start();
        metername = Owner.BrowseName;
    }

    public override void Stop()
    {
        timer1.Dispose();
    }

    private void GeneratorValue()
    {
        Random r =new Random();

        var UAcounterSetting = Owner.GetVariable("CounterSetting");
        var UAcounterPer10  = Owner.GetVariable("CounterPerTenSecond");
        var UAcounterTotal = Owner.GetVariable("Counter");
        var UAProductSpeedSetting = Owner.GetVariable("ProductOutPutRateSetting");
        var UAProductSpeed = Owner.GetVariable("ProductOutPutRate");
        var UAProductVolume = Owner.GetVariable("ProductOutPutTotal");
        string metername = Owner.BrowseName;

        int counterPer10 = UAcounterSetting.Value + (int)r.NextInt64(3);
        long counterTotal = UAcounterTotal.Value;
        counterTotal += counterPer10 ;
        UAcounterPer10.Value = counterPer10;
        UAcounterTotal.Value = counterTotal;

        int productSpeed = UAProductSpeedSetting.Value + (int)r.NextInt64(5);
        long productVolume = UAProductVolume.Value;
        productVolume += productSpeed;
        UAProductSpeed.Value = productSpeed;
        UAProductVolume.Value = productVolume;

        Log.Info($"{metername} and total is {counterTotal} ,and product volume is {productVolume}");
        Thread.Sleep(1);
    }
    public override string ToString()
    {
        return "this is " + metername ;
    }
}
