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
using System.ComponentModel;
using FTOptix.AuditSigning;
using FTOptix.Recipe;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.OPCUAServer;
using FTOptix.WebUI;
using FTOptix.RAEtherNetIP;
#endregion

public class RecordEnergy : BaseNetLogic
{
    static private string[] shiftcolumns = { "State", "Group", "MeterType", "Year", "Month", "Day", "Week", "MeterName", "Shift", "Consume", "ProductVolume", "RateToCarbon", "RateToCost" };
    static private string[] multiRatecolumns = { "State", "Group", "MeterType", "Year", "Month", "Day", "Week", "MeterName", "RateDuration", "Consume", "ProductVolume", "RateToCarbon", "RateToCost" };
    static private string[] test = { "1", "2","3","10","20","21","22","30","31","40","41"};
    private Store myDbStore;
    private IUAVariable time;
    Dictionary<string, float> rateCost = new Dictionary<string, float>();

    public override void Start()
    {
        myDbStore = Project.Current.Get<Store>("DataStores/ODBCPowerDB");
        time = Project.Current.GetVariable("Model/CommonTypes/ClockLogic/Time");
        GetRate();
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
    // This Method can be add in Model/CommonTypes/TimeInterval Object Type for all meters
    public void CreateRecord(String shiftOrrate)
    {
        // Prepare SQL Query
        var _now = (DateTime)time.Value;
        var zhCN = new System.Globalization.CultureInfo("zh-CN");
        var chinaCalendar = zhCN.DateTimeFormat.Calendar;

        var shiftvalues = new object[1, 13];
        shiftvalues[0, 0] = 1;
        shiftvalues[0, 1] = (string)Owner.GetVariable("Group").Value;
        shiftvalues[0, 2] = (string)Owner.GetVariable("MeterType").Value;
        shiftvalues[0, 3] = _now.Year;
        shiftvalues[0, 4] = _now.Month;
        shiftvalues[0, 5] = _now.Day;
        shiftvalues[0, 6] = chinaCalendar.GetWeekOfYear(_now, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
        shiftvalues[0, 7] = Owner.BrowseName;
        shiftvalues[0, 8] = shiftOrrate;
        Single[] v = Owner.GetVariable("Energy").Value;
        shiftvalues[0, 9] = v[0];
        shiftvalues[0, 10] = (float)Owner.GetVariable("ProductOutputTotal").Value;
        shiftvalues[0, 11] = (float)Owner.GetVariable("RateToCarbon").Value;
        

        // Execute insert
        try
        {
            if (int.Parse(shiftOrrate) < 6)
            {
                shiftvalues[0, 12] = (float)Owner.GetVariable("RateToCost").Value;
                myDbStore.Insert("RecordShiftEnergy", shiftcolumns, shiftvalues);
                return;
            }
            else
            {
                shiftvalues[0, 12] = rateCost[shiftOrrate];
                myDbStore.Insert("RecordMultiRateEnergy", multiRatecolumns, shiftvalues);
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
       // Tuple(ID, Consume, ProductVolume, RateToCarbon, RateToCost)
        (long, double, double,double,double) pervious = SelectID(shiftOrrate);
        if (pervious.Item1 <= 0)
        {
            Log.Error("Update", "No pervious record");
            return;
        }
        else
        {
            Single[] a = Owner.GetVariable("Energy").Value;
            double energy = a[0] - pervious.Item2;
            double volume = (double)Owner.GetVariable("ProductOutputTotal").Value - pervious.Item3;
            double carbon = energy * pervious.Item4;
            double cost = energy * pervious.Item5;
            if (energy < 0 || volume < 0)
                throw new Exception("Something wrong ,the data is always > 0");
            else
            {
                string updateshiftrecord = $"UPDATE RecordShiftEnergy SET State = 2,Consume = {energy},ProductVolume = {volume},RateToCarbon={carbon},RateToCost={cost} WHERE ID = {pervious.Item1}";
                string updateraterecord = $"UPDATE recordmultirateenergy SET State = 2,Consume = {energy},ProductVolume = {volume},RateToCarbon={carbon},RateToCost={cost} WHERE ID = {pervious.Item1}";

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
    // output parameter is Tuple (ID,Consume, ProductVolume,RateToCarbon,RateToCost)
    private (long, double, double, double, double) SelectID(String shiftOrrate)
    {
        var group = (string)Owner.GetVariable("Group").Value;
        var metertype = (string)Owner.GetVariable("MeterType").Value;
        var metername = Owner.BrowseName;
        string shiftquery = $"SELECT ID,Consume,ProductVolume,RateToCarbon,RateToCost FROM recordshiftenergy where State=1 and Group=\'{group}\' and MeterType =\'{metertype}\' " +
            $"and MeterName=\'{metername}\' and Shift=\'{shiftOrrate}\' order by ID desc limit 1";
        string ratequery = $"SELECT ID,Consume,ProductVolume,RateToCarbon,RateToCostFROM recordmultirateenergy where State=1 and Group=\'{group}\' and MeterType =\'{metertype}\' " +
            $"and MeterName=\'{metername}\' and RateDuration=\'{shiftOrrate}\' order by ID desc limit 1";

        Object[,] ResultSet;
        String[] Header;
        try
        {
            if (int.Parse(shiftOrrate) < 10)
                myDbStore.Query(shiftquery, out Header, out ResultSet);
            else
                myDbStore.Query(ratequery, out Header, out ResultSet);
            if (ResultSet.GetLength(0) == 1)
            {
                Log.Info($"Current ID:{ResultSet[0, 0]}");
                return ((long)ResultSet[0, 0], (double)ResultSet[0, 1], (double)ResultSet[0, 2], (double)ResultSet[0, 3], (double)ResultSet[0, 4]);
            }
            return (0, -1, -1,-1,-1);
        }
        catch (Exception ex)
        {
            Log.Error("Select Shift ID Error", ex.Message);
            return (0, -1, -1,-1,-1);
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
    [ExportMethod]
    public void addTestData(int month,int week, int day)
    {
        Random r = new Random();
        // Prepare SQL Query
        foreach (string i in test)
        {
            var shiftvalues = new object[1, 13];
            shiftvalues[0, 0] = 2;
            shiftvalues[0, 1] = (string)Owner.GetVariable("Group").Value;
            shiftvalues[0, 2] = (string)Owner.GetVariable("MeterType").Value;
            shiftvalues[0, 3] = 2023;
            shiftvalues[0, 4] = month;
            shiftvalues[0, 5] = day;
            shiftvalues[0, 6] = week;
            shiftvalues[0, 7] = Owner.BrowseName;
            shiftvalues[0, 8] = Convert.ToString(i);
            shiftvalues[0, 9] = r.NextDouble() * 20;
            shiftvalues[0, 10] = r.NextDouble() * 10;
            shiftvalues[0, 11] = (float)Owner.GetVariable("RateToCarbon").Value;
            shiftvalues[0, 12] = (float)Owner.GetVariable("RateToCost").Value;
            // Execute insert
            try
            {
                switch (i)
                {
                    case "1":
                    case "2":
                    case "3":
                        myDbStore.Insert("RecordShiftEnergy", shiftcolumns, shiftvalues);
                        break;
                    case "10":
                    case "20":
                    case "21":
                    case "22":
                    case "30":
                    case "31":
                    case "40":
                    case "41":
                        shiftvalues[0, 12] = rateCost[i];
                        myDbStore.Insert("RecordMultiRateEnergy", multiRatecolumns, shiftvalues);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Test Mode Add Record Error", ex.Message);
                return;
            }
        }

    }
    [ExportMethod]
    public void GetRate()
    {
        var multiRate = Project.Current.GetObject("Model/Configures/MultiRate").Children;
        rateCost.Clear();
        foreach (var r in multiRate)
        {
            if (r is TimeInterval)
            {
                rateCost.Add(r.GetVariable("ID").Value, r.GetVariable("RateToCost").Value);
            }
        }
    }
}
