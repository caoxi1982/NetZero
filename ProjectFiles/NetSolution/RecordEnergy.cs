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
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Numerics;
using FTOptix.Modbus;
using FTOptix.CommunicationDriver;
#endregion

public class RecordEnergy : BaseNetLogic
{ 
    static private string[] shiftcolumns = { "Group", "MeterType", "Year", "Month", "Day", "Week", "MeterName", "Shift", "Consume", "ProductVolume", "RateToCarbon", "RateToCost" };
    static private string[] multiRatecolumns = { "Group", "MeterType", "Year", "Month", "Day", "Week", "MeterName", "RateDuration", "Consume", "ProductVolume", "RateToCarbon", "RateToCost" };

    private Store myDbStore;
    private IUAVariable time;
    public override void Start()
    {
        myDbStore = Project.Current.Get<Store>("DataStores/ODBCPowerDB");
        time = Project.Current.GetVariable("CommonTypes/ClockLogic/Time");
    }

    public override void Stop()
    {
        myDbStore = null;
    }
    /*
     * shiftOrrate means :
     * 1: Shift1
     * 2: Shift2
     * 3: Shift3
     * 10:  0:00-8:00 Valley
     * 20:  11:00-14:00 Normal
     * 21   15:00-18:00 Normal 
     * 22   22:00-24:00 Normal
     * 30:  8:00-10:00 High
     * 31   18:00-22:00 High
     * 40:  10:00-11:00 Peak
     * 41:  14:00-15:00 Peak
     * test data:
     * var shiftvalues = new object[1, 12];
        shiftvalues[0, 0] = "Takeup";
        shiftvalues[0, 1] = "Power";
        shiftvalues[0, 2] = 2023;
        shiftvalues[0, 3] = 4;
        shiftvalues[0, 4] = 27;
        shiftvalues[0, 5] = 17;
        shiftvalues[0, 6] = "LineTotal";
        shiftvalues[0, 7] = "2";
        shiftvalues[0, 8] = 33.33;
        shiftvalues[0, 9] = 44.44;
        shiftvalues[0, 10] = 0.549;
        shiftvalues[0, 11] = 0.75;
     */
    [ExportMethod]
    public void CreateRecord(String shiftOrrate)
    {
        // Prepare SQL Query
        var _now = (DateTime)time.Value;
        var zhCN = new System.Globalization.CultureInfo("zh-CN");
        var chinaCalendar = zhCN.DateTimeFormat.Calendar;       
     
        var shiftvalues = new object[1, 12];
        shiftvalues[0, 0] = Owner.GetVariable("Group");
        shiftvalues[0, 1] = Owner.GetVariable("MeterType");
        shiftvalues[0, 2] = _now.Year;
        shiftvalues[0, 3] = _now.Month;
        shiftvalues[0, 4] = _now.Day;
        shiftvalues[0, 5] = chinaCalendar.GetWeekOfYear(_now, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
        shiftvalues[0, 6] = Owner.BrowseName;
        shiftvalues[0, 7] = shiftOrrate;
        shiftvalues[0, 8] = Owner.GetVariable("Energy");
        shiftvalues[0, 9] = Owner.GetVariable("ProductOutputTotal");
        shiftvalues[0, 10] = Owner.GetVariable("RateToCarbon");
        shiftvalues[0, 11] = Owner.GetVariable("RateToCost");
        // Execute insert
        try
        {
            if (int.Parse(shiftOrrate) < 6)
            {
                //myDbStore.Tables[6].Insert(shiftcolumns, shiftvalues);
                myDbStore.Insert("recordshiftenergy", shiftcolumns, shiftvalues);
                return;
            }
            else
            {
                myDbStore.Tables[7].Insert(multiRatecolumns, shiftvalues);
                return;
            }
        }
        catch (Exception ex)
        {
            Log.Error("Create Record Error", ex.Message);
            return;
        }

    }
    [ExportMethod]
    public void UpdateShiftRecord(String shiftOrrate)
    {
        (UInt64, double, double) pervious = SelectID(shiftOrrate);
        if (pervious.Item1 == 0)
        {
            Log.Error("Update", "No pervious record");
            return;
        }
        else
        {
            double energy = (double)Owner.GetVariable("Energy").Value - pervious.Item2;
            double volume = (double)Owner.GetVariable("ProductOutputTotal").Value - pervious.Item3;
            if (energy < 0 || volume < 0)
                throw new Exception("Something wrong ,the data is always > 0");
            else
            {
                string updateshiftrecord = $"UPDATE recordmultirateenergy SET Consume = {energy},ProductVolume = {volume} WHERE ID = {pervious.Item1}";
                string updateraterecord = $"UPDATE recordmultirateenergy SET Consume = {energy},ProductVolume = {volume} WHERE ID = {pervious.Item1}";

                // Execute insert
                try
                {
                    Object[,] ResultSet;
                    String[] Header;
                    if (int.Parse(shiftOrrate) < 6)
                    {
                        myDbStore.Query(updateshiftrecord, out Header, out ResultSet);
                        return;
                    }
                    else
                    {
                        myDbStore.Query(updateraterecord, out Header, out ResultSet);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Update Record Error", ex.Message);
                    return;
                }
            }
        }
    }
    // output parameter is Tuple (ID,Consume, ProductVolume)
    private (UInt64,double,double) SelectID(String shiftOrrate)
    {
        var group = Owner.GetVariable("Group").Value;
        var metertype = Owner.GetVariable("MeterType").Value;
        var metername = Owner.BrowseName;
        string shiftquery = $"SELECT ID,Consume,ProductVolume FROM recordshiftenergy where Group={group} and MeterType ={metertype} " +
            $"and MeterName={metername} and Shift={shiftOrrate} order by ID desc limit 1";
        string ratequery = $"SELECT ID,Consume,ProductVolume FROM recordmultirateenergy where Group={group} and MeterType ={metertype} " +
            $"and MeterName={metername} and RateDuration={shiftOrrate} order by ID desc limit 1";

        Object[,] ResultSet;
        String[] Header;
        try
        {
            if (int.Parse(shiftOrrate) < 6)
                myDbStore.Query(shiftquery, out Header, out ResultSet);
            else
                myDbStore.Query(ratequery, out Header, out ResultSet);
            if (ResultSet.Length == 1)
            {
                Log.Info($"Current ID:{ResultSet[0, 0]}");
                return ((UInt64)ResultSet[0, 0], (double)ResultSet[0, 1], (double)ResultSet[0, 2]);
            }
            return (0,-1,-1);
        }
        catch (Exception ex)
        {
            Log.Error("Select Shift ID Error", ex.Message);
            return (0,-1,-1);
        }
    }
    // Delete is not used,But can be used in the future to keep only one yeas data
    private void Delete(int value)
    {
        Object[,] ResultSet;
        String[] Header;
        myDbStore.Query("DELETE FROM Demo WHERE Value<=65535 ORDER BY Timestamp DESC LIMIT 1", out Header, out ResultSet);
        Log.Info("Delete", "Deleted last record");
    }
}
