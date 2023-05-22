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

public class RunPowerMeter : BaseNetLogic
{
    /* for the power meter, 1 minute is enough;
     * new PeriodicTask(GeneratorValue, 60000, LogicObject);
        * ex:powersetting is 6KW
        *    voltage setting is 230V
        *    current setting is 11.5A
        *    energy per minute is 6KW/60 minutes = 100WH
        *    100 is the interval for the timer1
    */
    private PeriodicTask timer1;
    private DateTime current_time;
    private string metername;
    private double radian;
    public override void Start()
    {
        timer1 = new PeriodicTask(GeneratorValue, 3000, LogicObject);
        timer1.Start();
        current_time = System.DateTime.Now;
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
        if (radian > 180 )
        {
            radian = 1;
        }
        radian += 0.5;
        double sinvalue =  Math.Sin((radian*2*Math.PI)/360);

        int UAPowersetting = Owner.GetVariable("PowerSetting").Value;
        int UAVoltagesetting = Owner.GetVariable("VoltageSetting").Value;

        var UAVoltage = Owner.GetVariable("Voltage");
        var UACurrent = Owner.GetVariable("Current");
        var UAPower = Owner.GetVariable("Power");
        var UAEnergy = Owner.GetVariable("Energy");

        var UAProductSpeedSetting = Owner.GetVariable("ProductOutPutRateSetting");
        var UAProductSpeed = Owner.GetVariable("ProductOutPutRate");
        var UAProductVolume = Owner.GetVariable("ProductOutPutTotal");

        // set voltage
        float[] v_value = UAVoltage.Value;
        for (int i = 1; i < 4; i++)
        {
            v_value[i] = UAVoltagesetting + (float)r.NextDouble() * 2;
        }
        // set power and current Current[0] =  0 , Power[0] = total
        float[] p_value = UAPower.Value;
        float[] i_value = UACurrent.Value;
        p_value[0] = (float)UAPowersetting * (float)sinvalue + (float)r.NextDouble();
        i_value[0] = 0;
        for (int i = 1; i < 4; i++)
        {
            p_value[i] = p_value[0] / 3 + (float)r.NextDouble();
            i_value[i] = (p_value[0] - (float)r.NextDouble()) * 2;
        }
        // set energy ,as Power unit is KW, Energy unit is KWH
        float[] e_value = UAEnergy.Value;
        for (int i = 0; i < 4; i++)
        {
            e_value[i] += (p_value[i] / 60);
        }
        //write back to Optix
        UAPower.Value = p_value;
        UAEnergy.Value = e_value;
        UACurrent.Value = i_value;
        UAVoltage.Value = v_value;

        float productSpeed = (float)(UAProductSpeedSetting.Value * (1 + 0.4 * r.NextDouble()));
        double productVolume = UAProductVolume.Value;
        productVolume += productSpeed;
        UAProductSpeed.Value = productSpeed;
        UAProductVolume.Value = productVolume;

        Log.Info(this.ToString() + $"Power is {p_value[0]},{p_value[1]},{p_value[2]},{p_value[3]}\n" +
                                    $"Energy is {e_value[0]},{e_value[1]},{e_value[2]},{e_value[3]}\n"    );
        Thread.Sleep(1);
    }
    public override string ToString()
    {
        return "This is " + metername;
    }
}
