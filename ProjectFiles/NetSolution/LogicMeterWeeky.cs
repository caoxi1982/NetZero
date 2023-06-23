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
using FTOptix.Report;
using FTOptix.OPCUAServer;
using FTOptix.WebUI;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using FTOptix.SQLiteStore;
using FTOptix.OPCUAClient;
#endregion

public class LogicMeterWeeky : BaseNetLogic
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
        DrawCarbonByDayChart();
    }
    [ExportMethod]
    public void DrawCarbonByDayChart()
    {
        // Get the different pieces we need to build the graph
        IUANode myModelObject = Owner.Get("CarbonByDay");
        var meter = (String)Owner.GetVariable("SelectMeterName").Value;
        int week = Owner.GetVariable("SelectWeek").Value;
        int year = Owner.GetVariable("SelectYear").Value;
        var nodata = Owner.GetVariable("NoData1");
        Store myDbStore = InformationModel.Get<Store>(Owner.GetVariable("MyDatabase").Value);
        string sqlQuery = $"SELECT Day,SUM(RateToCarbon) AS Value FROM RecordShiftEnergy " +
            $"WHERE MeterName=\'{meter}\' AND Year={year} AND Week={week} GROUP BY Day";
        // Prepare SQL Query
        // Execute query and check result
        try
        {
            HistogramChart myChart = (HistogramChart)Owner.GetObject("CarbonByDayChart");
            Object[,] ResultSet;
            String[] Header;
            myDbStore.Query(sqlQuery, out Header, out ResultSet);
            if (ResultSet.GetLength(0) < 1)
            {
                nodata.Value = true;
                Log.Error("LogicMeterWeeky", "Line 57:Input query returned less than one line");
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
                String columnName = "Day_" + Convert.ToString(ResultSet[i, 0]);
                var myObj = InformationModel.MakeVariable(columnName, OpcUa.DataTypes.String);
                myObj.Value = Convert.ToDouble(ResultSet[i, 1]) * 1000;
                myModelObject.Add(myObj);

            }
            myChart.Refresh();
        }
        catch (Exception ex)
        {
            Log.Error("LogicMeterWeeky", ex.Message);
            return;
        }
    }
  
    [ExportMethod]
    public void DrawEnergyByRateChart()
    {
        // Get the different pieces we need to build the graph
        IUANode myModelObject = Owner.Get("EnergyByRate");
        var meter = (String)Owner.GetVariable("SelectMeterName").Value;
        int week = Owner.GetVariable("SelectWeek").Value;
        int year = Owner.GetVariable("SelectYear").Value;
        var nodata = Owner.GetVariable("NoData2");
        Store myDbStore = InformationModel.Get<Store>(Owner.GetVariable("MyDatabase").Value);
        string sqlQuery = $"SELECT RateDuration,SUM(RateToCarbon) AS Value FROM recordmultirateenergy " +
            $"WHERE MeterName=\'{meter}\' AND Year={year} AND Week={week} GROUP BY RateDuration ORDER BY RateDuration";

        // Prepare SQL Query
        // Execute query and check result
        try
        {
            PieChart myChart = (PieChart)Owner.GetObject("EnergyByRateChart");
            Object[,] ResultSet;
            String[] Header;
            myDbStore.Query(sqlQuery, out Header, out ResultSet);
            double valley = 0, normal = 0, high = 0, peak = 0;
            var myObj1 = InformationModel.MakeVariable("Valley", OpcUa.DataTypes.String);
            var myObj2 = InformationModel.MakeVariable("Normal", OpcUa.DataTypes.String);
            var myObj3 = InformationModel.MakeVariable("High", OpcUa.DataTypes.String);
            var myObj4 = InformationModel.MakeVariable("Peak", OpcUa.DataTypes.String);
            foreach (var children in myModelObject.Children)
            {
                children.Delete();
            }
            if (ResultSet.GetLength(0) < 1)
            {
                nodata.Value = true;  
                myModelObject.Add(myObj1); myModelObject.Add(myObj2); myModelObject.Add(myObj3); myModelObject.Add(myObj4);
                myChart.Refresh();
                Log.Info("LogicMeterWeeky", "Line 150:this meter may not support Multi-Rate");
                return;
            }
            nodata.Value = false;
            // Delete all children from Object
            // For each column create an Object children
            for (int i = 0; i < ResultSet.GetLength(0); i++)
            {

                switch (ResultSet[i, 0])
                {
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
            myObj1.Value = valley * 1000;
            myObj2.Value = normal * 1000;
            myObj3.Value = high * 1000;
            myObj4.Value = peak * 1000;
            myModelObject.Add(myObj1); myModelObject.Add(myObj2); myModelObject.Add(myObj3); myModelObject.Add(myObj4);
            myChart.Refresh();
        }
        catch (Exception ex)
        {
            Log.Error("LogicMeterWeeky", ex.Message);
            return;
        }
    }
}
