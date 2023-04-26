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
#endregion

public class RunCounterMeter : BaseNetLogic
{
    private PeriodicTask timer1;
    private string metername;
    public override void Start()
    {
        timer1 = new PeriodicTask(GeneratorValue, 5000, LogicObject);
        timer1.Start();
        metername = Owner.BrowseName;
    }

    public override void Stop()
    {
        timer1.Dispose();
    }

    private void GeneratorValue()
    {
        var UAcounterPer10  = Owner.GetVariable("CounterPerTenSecond");
        var UAcounterTotal = Owner.GetVariable("Counter");
        string metername = Owner.BrowseName;

        int counterPer10 = UAcounterPer10.Value;
        long counterTotal = UAcounterTotal.Value;
        counterTotal += counterPer10;
        UAcounterTotal.Value = counterTotal;
        Log.Info(this.ToString() + "and total is" + counterTotal);
        Thread.Sleep(1);
    }
    public override string ToString()
    {
        return "this is " + metername ;
    }
}
