#region Using directives
using System;
using CoreBase = FTOptix.CoreBase;
using FTOptix.HMIProject;
using UAManagedCore;
using FTOptix.UI;
using FTOptix.NetLogic;
using FTOptix.Modbus;
using FTOptix.CommunicationDriver;
using FTOptix.AuditSigning;
using FTOptix.Recipe;
using FTOptix.EventLogger;
#endregion

public class ClockLogic : BaseNetLogic
{

	private bool testMode;
	public override void Start()
	{
		testMode = (bool)LogicObject.GetVariable("TestMode").Value;
		if (!testMode)
		{
			periodicTask = new PeriodicTask(UpdateTime, 1000, LogicObject);
			periodicTask.Start();
		}
		else
		{
			UpdateTime();
        }
	}

	public override void Stop()
	{
		if (!testMode)
		{
			periodicTask.Dispose();
		}
		periodicTask = null;
	}

	private void UpdateTime()
	{
        var zhCN = new System.Globalization.CultureInfo("zh-CN");
        var chinaCalendar = zhCN.DateTimeFormat.Calendar;
		var _now = DateTime.Now;
        LogicObject.GetVariable("Time").Value = _now;
		LogicObject.GetVariable("UTCTime").Value = DateTime.UtcNow;
        LogicObject.GetVariable("WeekOfYear").Value = 
			chinaCalendar.GetWeekOfYear(_now, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
        
    }

	private PeriodicTask periodicTask;
}
