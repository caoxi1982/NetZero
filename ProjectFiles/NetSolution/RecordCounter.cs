#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.DataLogger;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.System;
using FTOptix.UI;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.Core;
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
using NodaTime;
#endregion

public class RecordCounter : BaseNetLogic
{
    static private string[] shiftcolumns = { "State", "Group", "MeterType", "Year", "Month", "Day", "Week", "MeterName", "Shift", "Consume", "ProductVolume", "RateToCarbon", "RateToCost" };
    static private string[] test = { "1", "2", "3" };
    private Store myDbStore;
    private IUAVariable time;

    public override void Start()
    {
        myDbStore = Project.Current.Get<Store>("DataStores/EmbeddedDatabase1");
        time = Project.Current.GetVariable("Model/CommonTypes/ClockLogic/Time");
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
        var shiftvalues = new object[1, 13];
        shiftvalues[0, 0] = 1;
        shiftvalues[0, 1] = (string)Owner.GetVariable("Group").Value;
        shiftvalues[0, 2] = (string)Owner.GetVariable("MeterType").Value;
        shiftvalues[0, 3] = _now.Year;
        shiftvalues[0, 4] = _now.Month;
        shiftvalues[0, 5] = _now.Day;
        shiftvalues[0, 6] = getWeekOfYear(_now);
        shiftvalues[0, 7] = Owner.BrowseName;
        shiftvalues[0, 8] = shiftOrrate;
        shiftvalues[0, 9] = (float)Owner.GetVariable("CounterTotal").Value;
        shiftvalues[0, 10] = (float)Owner.GetVariable("ProductOutputTotal").Value;
        shiftvalues[0, 11] = (float)Owner.GetVariable("RateToCarbon").Value;
        shiftvalues[0, 12] = (float)Owner.GetVariable("RateToCost").Value;
        // Execute insert
        try
        {
            if (int.Parse(shiftOrrate) < 6)
            {
                //myDbStore.Tables[6].Insert(shiftcolumns, shiftvalues);
                myDbStore.Insert("RecordShiftEnergy", shiftcolumns, shiftvalues);
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
        (long, double, double,double,double) pervious = SelectID(shiftOrrate);
        if (pervious.Item1 <= 0)
        {
            Log.Error("Update", "No pervious record");
            return;
        }
        else
        {
            double energy = (double)Owner.GetVariable("CounterTotal").Value - pervious.Item2;
            double volume = (double)Owner.GetVariable("ProductOutputTotal").Value - pervious.Item3;
            double carbon = energy * pervious.Item4;
            double cost = energy * pervious.Item5;
            if (energy < 0 || volume < 0)
                throw new Exception("Something wrong ,the data is always > 0");
            else
            {
                string updateshiftrecord = $"UPDATE RecordShiftEnergy SET State = 2,Consume = {energy},ProductVolume = {volume},RateToCarbon={carbon},RateToCost={cost} WHERE ID = {pervious.Item1}";

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
    private (long, double, double,double,double) SelectID(String shiftOrrate)
    {
        var group = (string)Owner.GetVariable("Group").Value;
        var metertype = (string)Owner.GetVariable("MeterType").Value;
        var metername = Owner.BrowseName;
        string shiftquery = $"SELECT ID,Consume,ProductVolume,RateToCarbon,RateToCost FROM recordshiftenergy where State=1 and Group=\'{group}\' and MeterType =\'{metertype}\' " +
            $"and MeterName=\'{metername}\' and Shift=\'{shiftOrrate}\' order by ID desc limit 1";
        Object[,] ResultSet;
        String[] Header;
        try
        {
            if (int.Parse(shiftOrrate) < 10)
            {
                myDbStore.Query(shiftquery, out Header, out ResultSet);
                if (ResultSet.GetLength(0) == 1)
                {
                    Log.Info($"Current ID:{ResultSet[0, 0]}");
                    return ((long)ResultSet[0, 0], (double)ResultSet[0, 1], (double)ResultSet[0, 2], (double)ResultSet[0, 3], (double)ResultSet[0, 4]);
                }
            }
            Log.Error("Select Shift ID Error", "Can not find the shift record");
            return (0, -1, -1,-1,-1);
        }
        catch (Exception ex)
        {
            Log.Error("Select Shift ID Error", ex.Message);
            return (0, -1, -1,-1,-1);
        }
    }
    private void Delete()
    {
        Object[,] ResultSet;
        String[] Header;
        string metername = Owner.BrowseName;
        myDbStore.Query($"DELETE FROM recordshiftenergy WHERE MeterName=\'{metername}\'", out Header, out ResultSet);
        Log.Info("Delete records recordshiftenergy table ", $"Meter:\'{metername}\' deleted");
    }
    [ExportMethod]
    public void addTestData(DateTime? date)
    {
        Random r = new Random();
        var _date = date??(DateTime)time.Value;
        // Prepare SQL Query
        foreach (string i in test)
        {
            var shiftvalues = new object[1, 13];
            shiftvalues[0, 0] = 2;
            shiftvalues[0, 1] = (string)Owner.GetVariable("Group").Value;
            shiftvalues[0, 2] = (string)Owner.GetVariable("MeterType").Value;
            shiftvalues[0, 3] = _date.Year;
            shiftvalues[0, 4] = _date.Month;
            shiftvalues[0, 5] = _date.Day;
            shiftvalues[0, 6] = getWeekOfYear(_date);
            shiftvalues[0, 7] = Owner.BrowseName;
            shiftvalues[0, 8] = i;
            shiftvalues[0, 9] = r.NextDouble() * 20;
            shiftvalues[0, 10] = r.NextDouble() * 10;
            shiftvalues[0, 11] = (double)Owner.GetVariable("RateToCarbon").Value * (double)shiftvalues[0, 9];
            shiftvalues[0, 12] = (double)Owner.GetVariable("RateToCost").Value * (double)shiftvalues[0, 9];
            // Execute insert
            try
            {
                myDbStore.Insert("RecordShiftEnergy", shiftcolumns, shiftvalues);
            }
            catch (Exception ex)
            {
                Log.Error("Test Mode Add Record Error", ex.Message);
                return;
            }
        }

    }
    [ExportMethod]
    public void startAPP()
    {
        this.Delete();
        var _today = (DateTime)time.Value;
        for (int i = 1; i < 15; i++)
        {
            addTestData(_today.AddDays(-i));
        }
    }
    private int getWeekOfYear(DateTime? date)
    {
        var _now = date??(DateTime)time.Value;
        var zhCN = new System.Globalization.CultureInfo("zh-CN");
        var chinaCalendar = zhCN.DateTimeFormat.Calendar;
        return chinaCalendar.GetWeekOfYear(_now, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
    }
}
