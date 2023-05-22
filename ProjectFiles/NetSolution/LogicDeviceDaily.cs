#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.DataLogger;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.Core;
using FTOptix.AuditSigning;
using FTOptix.Recipe;
using FTOptix.EventLogger;
using System.Xml.Linq;
using FTOptix.Report;
using FTOptix.OPCUAServer;
using FTOptix.WebUI;
#endregion

public class LogicDeviceDaily : BaseNetLogic
{
    public override void Start()
    {
        RefreshAll();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
    [ExportMethod]
    public void RefreshAll()
    {
        DrawEnergyByRateChart();
        DrawCarbonByShiftChart();
        DrawCarbonByMeterChart();
    }
    [ExportMethod]
    public void DrawCarbonByShiftChart()
    {
        // Get the different pieces we need to build the graph
        IUANode myModelObject = Owner.Get("CarbonByShift");
        var group = (String)Owner.GetVariable("SelectDeviceName").Value;
        var nodata = Owner.GetVariable("NoData1");
        DateTime select_date = Owner.GetVariable("SelectDay").Value;
        int year = select_date.Year;
        int month = select_date.Month;
        int day = select_date.Day;
        Store myDbStore = InformationModel.Get<Store>(Owner.GetVariable("MyDatabase").Value);
        string sqlQuery = $"SELECT Shift,SUM(RateToCarbon) AS Value FROM RecordShiftEnergy " +
            $"WHERE Group=\'{group}\' AND Year={year} AND Month={month} AND Day={day} GROUP BY Shift";
        // Prepare SQL Query
        // Execute query and check result
        try
        {
            PieChart myChart = (PieChart)Owner.GetObject("CarbonByShiftChart");
            Object[,] ResultSet;
            String[] Header;
            myDbStore.Query(sqlQuery, out Header, out ResultSet);
            if (ResultSet.GetLength(0) < 1)
            {
                nodata.Value = true;
                Log.Error("LogicDeviceDaily", "Line 59:Input query returned less than one line");
                return;
            }
            nodata.Value = false;
            // Delete all children from Object
            foreach (var children in myModelObject.Children)
            {
                children.Delete();
            }
            // For each column create an Object children
            for (int i = 0; i < ResultSet.GetLength(0); i++)
            {
                String columnName = "Shift_"+ Convert.ToString(ResultSet[i,0]);
                    var myObj = InformationModel.MakeVariable(columnName, OpcUa.DataTypes.String);
                    myObj.Value = Convert.ToDouble(ResultSet[i, 1]);
                    myModelObject.Add(myObj);

            }
            myChart.Refresh();
        }
        catch (Exception ex)
        {
            Log.Error("LogicDeviceDaily", ex.Message);
            return;
        }
    }
    [ExportMethod]
    public void DrawCarbonByMeterChart()
    {
        // Get the different pieces we need to build the graph
        IUANode myModelObject = Owner.Get("CarbonByMeter");
        var group = (String)Owner.GetVariable("SelectDeviceName").Value;
        var nodata = Owner.GetVariable("NoData2");
        DateTime select_date = Owner.GetVariable("SelectDay").Value;
        int year = select_date.Year;
        int month = select_date.Month;
        int day = select_date.Day;
        Store myDbStore = InformationModel.Get<Store>(Owner.GetVariable("MyDatabase").Value);
        string sqlQuery = $"SELECT MeterName,SUM(RateToCarbon) AS Value FROM RecordShiftEnergy " +
            $"WHERE Group=\'{group}\' AND Year={year} AND Month={month} AND Day={day} GROUP BY MeterName";
        // Prepare SQL Query
        // Execute query and check result
        try
        {
            HistogramChart myChart = (HistogramChart)Owner.GetObject("CarbonByMeterChart");
            Object[,] ResultSet;
            String[] Header;
            myDbStore.Query(sqlQuery, out Header, out ResultSet);
            if (ResultSet.GetLength(0) < 1)
            {
                nodata.Value = true;
                Log.Error("LogicDeviceDaily", "Line 107:Input query returned less than one line");
                return;
            }
            nodata.Value = false;
            // Delete all children from Object
            foreach (var children in myModelObject.Children)
            {
                children.Delete();
            }
            // For each column create an Object children
            for (int i = 0; i < ResultSet.GetLength(0); i++)
            {
                String columnName = Convert.ToString(ResultSet[i, 0]);
                var myObj = InformationModel.MakeVariable(columnName, OpcUa.DataTypes.String);
                myObj.Value = Convert.ToDouble(ResultSet[i, 1]);
                myModelObject.Add(myObj);

            }
            myChart.Refresh();
        }
        catch (Exception ex)
        {
            Log.Error("LogicDeviceDaily", ex.Message);
            return;
        }
    }
    [ExportMethod]
    public void DrawEnergyByRateChart()
    {
        // Get the different pieces we need to build the graph
        IUANode myModelObject = Owner.Get("EnergyByRate");
        var group = (String)Owner.GetVariable("SelectDeviceName").Value;
        var nodata = Owner.GetVariable("NoData3");
        DateTime select_date = Owner.GetVariable("SelectDay").Value;
        int year = select_date.Year;
        int month = select_date.Month;
        int day = select_date.Day;
        Store myDbStore = InformationModel.Get<Store>(Owner.GetVariable("MyDatabase").Value);
        string sqlQuery = $"SELECT RateDuration,SUM(RateToCarbon) AS Value FROM recordmultirateenergy " +
            $"WHERE Group=\'{group}\' AND Year={year} AND Month={month} AND Day={day} GROUP BY RateDuration ORDER BY RateDuration";

        // Prepare SQL Query
        // Execute query and check result
        try
        {
            PieChart myChart = (PieChart)Owner.GetObject("EnergyByRateChart");
            Object[,] ResultSet;
            String[] Header;
            myDbStore.Query(sqlQuery, out Header, out ResultSet);
            if (ResultSet.GetLength(0) < 1)
            {
                nodata.Value = true;
                Log.Error("LogicDeviceDaily", "Line 156:Input query returned less than one line");
                return;
            }
            nodata.Value = false;
            // Delete all children from Object
            foreach (var children in myModelObject.Children)
            {
                children.Delete();
            }
            // For each column create an Object children
            double valley = 0, normal = 0, high = 0, peak = 0;
            var myObj1 = InformationModel.MakeVariable("Valley", OpcUa.DataTypes.String);
            var myObj2 = InformationModel.MakeVariable("Normal", OpcUa.DataTypes.String);
            var myObj3 = InformationModel.MakeVariable("High", OpcUa.DataTypes.String);
            var myObj4 = InformationModel.MakeVariable("Peak", OpcUa.DataTypes.String);
            for (int i = 0; i < ResultSet.GetLength(0); i++)
            {
                
                switch (ResultSet[i, 0]){
                    case "10":
                        valley += Convert.ToDouble(ResultSet[i, 1]);
                        break;
                    case "20":
                    case "21":
                    case "22":
                        normal += Convert.ToDouble(ResultSet[i, 1]);
                        break;
                    case "30":
                    case "31":
                        high += Convert.ToDouble(ResultSet[i, 1]);
                        break;
                    case "40":
                    case "41":
                        peak += Convert.ToDouble(ResultSet[i, 1]);
                        break;
                }
            }
            myObj1.Value = valley;
            myObj2.Value = normal;
            myObj3.Value = high;
            myObj4.Value = peak;
            myModelObject.Add(myObj1); myModelObject.Add(myObj2); myModelObject.Add(myObj3); myModelObject.Add(myObj4);
            myChart.Refresh();
        }
        catch (Exception ex)
        {
            Log.Error("LogicDeviceDaily", ex.Message);
            return;
        }
    }
}
