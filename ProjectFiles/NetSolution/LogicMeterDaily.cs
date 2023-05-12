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
#endregion

public class LogicMeterDaily : BaseNetLogic
{
    public override void Start()
    {
        DrawCarbonByShiftChart();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
    [ExportMethod]
    public void DrawCarbonByShiftChart()
    {
        // Get the different pieces we need to build the graph
        IUANode myModelObject = Owner.Get("CarbonByShift");
        var meter = (String)Owner.GetVariable("SelectMeterName").Value;
        DateTime select_date = Owner.GetVariable("SelectDay").Value;
        var nodata = Owner.GetVariable("NoData1");
        int year = select_date.Year;
        int month = select_date.Month;
        int day = select_date.Day;
        Store myDbStore = InformationModel.Get<Store>(Owner.GetVariable("MyDatabase").Value);
        string sqlQuery = $"SELECT Shift,RateToCarbon AS Value FROM RecordShiftEnergy " +
            $"WHERE MeterName=\'{meter}\' AND Year={year} AND Month={month} AND Day={day}";
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
                String columnName = "Shift_" + Convert.ToString(ResultSet[i, 0]);
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
}
