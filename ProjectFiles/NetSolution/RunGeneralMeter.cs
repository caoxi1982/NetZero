#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.DataLogger;
using FTOptix.UI;
using FTOptix.Retentivity;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Store;
using FTOptix.Core;
using System.Threading;
using System.Timers;
using FTOptix.ODBCStore;
using FTOptix.Alarm;
using FTOptix.System;
#endregion

public class RunGeneralMeter : BaseNetLogic
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
        Random r = new Random();

        var UAspeed = Owner.GetVariable("SpeedSetting");
        var UAmeterspeed = Owner.GetVariable("MeterSpeed");
        var UAtotal = Owner.GetVariable("MeterTotal");
        string metername = Owner.BrowseName;

        float speed = (float)(UAspeed.Value + r.NextDouble());// from 0.0 to 1.0
        float total = UAtotal.Value;
        total += speed;
        UAtotal.Value = total;
        Log.Info(this.ToString() + " and total is " + total);
        Thread.Sleep(1);
    }
    public override string ToString()
    {
        return "This is " + metername;
    }
}
