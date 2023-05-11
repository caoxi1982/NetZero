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
public class ShiftSwitcher : BaseNetLogic
{
    private string shift1id, shift2id, shift3id;
    public override void Start()
    {
        shift1id = (string)Owner.GetVariable("Shift_1/ID").Value;
        shift2id = (string)Owner.GetVariable("Shift_2/ID").Value;
        shift3id = (string)Owner.GetVariable("Shift_3/ID").Value;
        myPeriodicTask = new PeriodicTask(switchShifts, 5000, LogicObject);
        myPeriodicTask.Start();
    }

    public override void Stop()
    {
        myPeriodicTask.Dispose();
    }

    private void switchShifts()
    {
        var shift1 = new TimeDuration(TimeOnly.FromDateTime(Owner.GetVariable("Shift_1/StartTime").Value),
                                        TimeOnly.FromDateTime(Owner.GetVariable("Shift_1/EndTime").Value));
        var shift2 = new TimeDuration(TimeOnly.FromDateTime(Owner.GetVariable("Shift_2/StartTime").Value),
                                        TimeOnly.FromDateTime(Owner.GetVariable("Shift_2/EndTime").Value));
        var shift3 = new TimeDuration(TimeOnly.FromDateTime(Owner.GetVariable("Shift_3/StartTime").Value),
                                        TimeOnly.FromDateTime(Owner.GetVariable("Shift_3/EndTime").Value));
        // get current time
        var _now = (DateTime)Project.Current.GetVariable("Model/CommonTypes/ClockLogic/Time").Value;
        var CurrentTime = TimeOnly.FromDateTime(_now);
        if (shift1.IsInTimeRange(CurrentTime))
        {
            Log.Info("Shift 1");
            Owner.GetVariable("CurrentShift").Value = shift1id;
            setid(shift1id);
            return;
        }
        else if (shift2.IsInTimeRange(CurrentTime))
        {
            Log.Info("Shift 2");
            Owner.GetVariable("CurrentShift").Value = shift2id;
            setid(shift2id);
            return;
        }
        else if (shift3.IsInTimeRange(CurrentTime))
        {
            Log.Info("Shift 3");
            Owner.GetVariable("CurrentShift").Value = shift3id;
            setid(shift3id);
            return;
        }
        else
        {
            throw new Exception("This is should be impossible");
        }
         void setid(string s)
        {
            Owner.GetVariable("Shift_1/Working").Value = 0;
            Owner.GetVariable("Shift_2/Working").Value = 0;
            Owner.GetVariable("Shift_3/Working").Value = 0;
            switch (s)
            {
                case "1":
                    Owner.GetVariable("Shift_1/Working").Value = 1;
                    break;
                case "2":
                    Owner.GetVariable("Shift_2/Working").Value = 1;
                    break;
                case "3":
                    Owner.GetVariable("Shift_3/Working").Value = 1;
                    break;
            }
        }
    }

    private PeriodicTask myPeriodicTask;
}
