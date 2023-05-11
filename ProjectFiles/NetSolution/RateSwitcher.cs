#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.DataLogger;
using FTOptix.NativeUI;
using FTOptix.System;
using FTOptix.UI;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using FTOptix.Core;
#endregion
using NetZero;
using FTOptix.Modbus;
using FTOptix.CommunicationDriver;
using FTOptix.AuditSigning;
using FTOptix.Recipe;
using FTOptix.EventLogger;
public class RateSwitcher : BaseNetLogic
{
    private PeriodicTask myPeriodicTask;
    private string peak1id, peak2id, high1id, high2id, normal1id, normal2id, normal3id, valley1id;
    public override void Start()
    {
        peak1id = (string)Owner.GetVariable("Peak_1/ID").Value;
        peak2id = (string)Owner.GetVariable("Peak_2/ID").Value;
        high1id = (string)Owner.GetVariable("High_1/ID").Value;
        high2id = (string)Owner.GetVariable("High_2/ID").Value;
        normal1id = (string)Owner.GetVariable("Normal_1/ID").Value;
        normal2id = (string)Owner.GetVariable("Normal_2/ID").Value;
        normal3id = (string)Owner.GetVariable("Normal_3/ID").Value;
        valley1id = (string)Owner.GetVariable("Valley_1/ID").Value;
        myPeriodicTask = new PeriodicTask(SwitchRateDuration, 3000, LogicObject);
        myPeriodicTask.Start();
    }

    public override void Stop()
    {
        myPeriodicTask.Dispose();
    }

    private void SwitchRateDuration()
    {
        var peak1 = new TimeDuration(TimeOnly.FromDateTime(Owner.GetVariable("Peak_1/StartTime").Value),
                                        TimeOnly.FromDateTime(Owner.GetVariable("Peak_1/EndTime").Value));
        var peak2 = new TimeDuration(TimeOnly.FromDateTime(Owner.GetVariable("Peak_2/StartTime").Value),
                                        TimeOnly.FromDateTime(Owner.GetVariable("Peak_2/EndTime").Value));
        var high1 = new TimeDuration(TimeOnly.FromDateTime(Owner.GetVariable("High_1/StartTime").Value),
                                        TimeOnly.FromDateTime(Owner.GetVariable("High_1/EndTime").Value));
        var high2 = new TimeDuration(TimeOnly.FromDateTime(Owner.GetVariable("High_2/StartTime").Value),
                                            TimeOnly.FromDateTime(Owner.GetVariable("High_2/EndTime").Value));
        var normal1 = new TimeDuration(TimeOnly.FromDateTime(Owner.GetVariable("Normal_1/StartTime").Value),
                                            TimeOnly.FromDateTime(Owner.GetVariable("Normal_1/EndTime").Value));
        var normal2 = new TimeDuration(TimeOnly.FromDateTime(Owner.GetVariable("Normal_2/StartTime").Value),
                                            TimeOnly.FromDateTime(Owner.GetVariable("Normal_2/EndTime").Value));
        var normal3 = new TimeDuration(TimeOnly.FromDateTime(Owner.GetVariable("Normal_3/StartTime").Value),
                                            TimeOnly.FromDateTime(Owner.GetVariable("Normal_3/EndTime").Value));
        var valley1 = new TimeDuration(TimeOnly.FromDateTime(Owner.GetVariable("Valley_1/StartTime").Value),
                                            TimeOnly.FromDateTime(Owner.GetVariable("Valley_1/EndTime").Value));
        // get current time
        var _now = (DateTime)Project.Current.GetVariable("Model/CommonTypes/ClockLogic/Time").Value;
        var CurrentTime = TimeOnly.FromDateTime(_now);
        if (peak1.IsInTimeRange(CurrentTime))
        {
            Log.Info("Rate Duration Peak 1");
            Owner.GetVariable("CurrentShift").Value = peak1id;
            setid(peak1id);
            return;
        }
        if (peak2.IsInTimeRange(CurrentTime))
        {
            Log.Info("Rate Duration Peak 2");
            Owner.GetVariable("CurrentShift").Value = peak2id;
            setid(peak2id);
            return;
        }
        if (high1.IsInTimeRange(CurrentTime))
        {
            Log.Info("Rate Duration High 1");
            Owner.GetVariable("CurrentShift").Value = high1id;
            setid(high1id);
            return;
        }
        if (high2.IsInTimeRange(CurrentTime))
        {
            Log.Info("Rate Duration High 2");
            Owner.GetVariable("CurrentShift").Value = high2id;
            setid(high2id);
            return;
        }
        if (normal1.IsInTimeRange (CurrentTime))
        {
            Log.Info("Rate Duration Normal 1");
            Owner.GetVariable("CurrentShift").Value = normal1id;
            setid(normal1id);
            return;
        }
        if (normal2.IsInTimeRange(CurrentTime))
        {
            Log.Info("Rate Duration Normal 2");
            Owner.GetVariable("CurrentShift").Value = normal2id;
            setid(normal2id);
            return;
        }
        if (normal3.IsInTimeRange(CurrentTime))
        {
            Log.Info("Rate Duration Normal 3");
            Owner.GetVariable("CurrentShift").Value = normal3id;
            setid(normal3id);
            return;
        }
        if (valley1.IsInTimeRange(CurrentTime))
        {
            Log.Info("Rate Duration Valley 1");
            Owner.GetVariable("CurrentShift").Value = valley1id;
            setid(valley1id);
            return;
        }

        void setid(string s)
        {
            Owner.GetVariable("Peak_1/Working").Value = 0;
            Owner.GetVariable("Peak_2/Working").Value = 0;
            Owner.GetVariable("High_1/Working").Value = 0;
            Owner.GetVariable("High_2/Working").Value = 0;
            Owner.GetVariable("Normal_1/Working").Value = 0;
            Owner.GetVariable("Normal_2/Working").Value = 0;
            Owner.GetVariable("Normal_3/Working").Value = 0;
            Owner.GetVariable("Valley_1/Working").Value = 0;
            switch (s){
                case "40":
                    Owner.GetVariable("Peak_1/Working").Value = 1;
                    break;
                case "41":
                    Owner.GetVariable("Peak_2/Working").Value = 1;
                    break;
                case "30":
                    Owner.GetVariable("High_1/Working").Value = 1;
                    break;
                case "31":
                    Owner.GetVariable("High_2/Working").Value = 1;
                    break;
                case "20":
                    Owner.GetVariable("Normal_1/Working").Value = 1;
                    break;
                case "21":
                    Owner.GetVariable("Normal_2/Working").Value = 1;                        
                    break;
                case "22":
                    Owner.GetVariable("Normal_3/Working").Value = 1;
                    break;
                case "10":
                    Owner.GetVariable("Valley_1/Working").Value = 1;
                    break;
                }
        };
    }
}
