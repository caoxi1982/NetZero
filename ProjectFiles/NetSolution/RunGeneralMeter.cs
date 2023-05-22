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
using FTOptix.Modbus;
using FTOptix.CommunicationDriver;
using FTOptix.AuditSigning;
using FTOptix.Recipe;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.OPCUAServer;
using FTOptix.WebUI;
#endregion

public class RunGeneralMeter : BaseNetLogic
{
    private PeriodicTask timer1;
    private string metername;
    private double radian;
    public override void Start()
    {
        timer1 = new PeriodicTask(GeneratorValue, 3500, LogicObject);
        timer1.Start();
        radian = 1;
        metername = Owner.BrowseName;
    }

    public override void Stop()
    {
        timer1.Dispose();
    }
    private void GeneratorValue()
    {
        Random r = new Random();
        if (radian > 180)
        {
            radian = 1;
        }
        radian += 0.3;
        double sinvalue = Math.Sin((radian*2*Math.PI)/360);

        //Here is general MeterSpeed base on SpeedSetting and add some wave
        var UAspeed = Owner.GetVariable("SpeedSetting");
        var UAmeterspeed = Owner.GetVariable("MeterSpeed");
        var UAtotal = Owner.GetVariable("MeterTotal");
        var UAProductSpeedSetting = Owner.GetVariable("ProductOutPutRateSetting");
        var UAProductSpeed = Owner.GetVariable("ProductOutPutRate");
        var UAProductVolume = Owner.GetVariable("ProductOutPutTotal");
        string metername = Owner.BrowseName;

        float speed = (float)(UAspeed.Value * sinvalue + 2*r.NextDouble());// from 0.0 to 1.0
        float total = UAtotal.Value;
        total += speed;
        UAmeterspeed.Value = speed;
        UAtotal.Value = total;

        float productSpeed = (float)(UAProductSpeedSetting.Value * (1 + 0.2 * r.NextDouble()));
        double productVolume = UAProductVolume.Value;
        productVolume += productSpeed;
        UAProductSpeed.Value = productSpeed;
        UAProductVolume.Value = productVolume;

        Log.Info($"{metername} and total is {total} ,and product volume is {productVolume}");
        Thread.Sleep(1);
    }
    public override string ToString()
    {
        return "This is " + metername;
    }
}
