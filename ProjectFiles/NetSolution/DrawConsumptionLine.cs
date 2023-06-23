#region Using directives
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using FTOptix.UI;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UAManagedCore;
using FTOptix.ODBCStore;
using InfluxDB.Client;
using Newtonsoft.Json.Linq;
using System.Data.Common;
using InfluxDB.Client.Configurations;
using FTOptix.WebUI;
using FTOptix.S7TCP;
using FTOptix.DataLogger;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
#endregion

public class DrawConsumptionLine : BaseNetLogic {
    public override void Start() {
        // Render graph
        UpdateSmooth();
    }

    public override void Stop() 
    {
    }

    [ExportMethod]
    public void UpdateSmooth() {
        //Chart Values
        string dataNames = "";
        string dataValues = "";
        string demandUnit = "";
        string consumptionUnit = "";
        //QueryVariables - From Energy Settings

        string selectedNode = Project.Current.GetVariable("Model/EnergyVariables/selectedNode").Value;
        Int16 timeInterval = Project.Current.GetVariable("Model/EnergyVariables/timePeriod").Value;
        string range = "-" + timeInterval.ToString() + "h";
        string aggregatedWindow = "15m";

        if (selectedNode.Contains("Water")){
            demandUnit = "m\xB3" + "/h";
            consumptionUnit = "m\xB3";
        }
        else if (selectedNode.Contains("Air")){
            demandUnit = "m\xB3" + "/h";
            consumptionUnit = "m\xB3";
        }
        else if (selectedNode.Contains("Electric")){
            demandUnit = "kW";
            consumptionUnit = "kWh";
        }
        else if (selectedNode.Contains("Steam")){
            demandUnit = "t/h";
            consumptionUnit = "t";
        }

        // Clean for float and double data types, might be useless in this case
        dataValues = dataValues.Replace(", ", "§");
        dataValues = dataValues.Replace(",", ".");
        dataValues = dataValues.Replace("§", ", ");

        // Get template name and create destination path
        string templatePath = new ResourceUri(Owner.GetVariable("templatePath").Value).Uri;
        var filePath = Owner.Get<WebBrowser>("WebBrowser").URL.Uri;

        // Read template page content
        string text = File.ReadAllText(templatePath);

        using var client = new InfluxDBClient(clientAddress, Token);
        var flux = $"from(bucket: \"{bucket}\") " +
        $" |> range(start: {range})" +
        $" |> filter(fn: (r) => r[\"_measurement\"] == \"{selectedNode}\") " +
        " |> filter(fn: (r) => r[\"_field\"] == \"Energy\") " +
        $" |> aggregateWindow(every: {aggregatedWindow}, fn: sum, createEmpty: false) " + 
        " |> yield(name: \"sum\")";

        var queryApi = client.GetQueryApiSync();

        //Query Data and store result in dataValues & dataNames String
        var tables = queryApi.QuerySync(flux, orgID);
        tables.ForEach(table =>
        {
            table.Records.ForEach(record =>
            {
                string timestamp = record.GetTime().ToString();

                if (dataNames == "")
                {
                    dataNames = "\'" + timestamp.Substring(timestamp.IndexOf(":") - 2, 5) + "\'";
                }
                else
                {
                    dataNames += ", \'" + timestamp.Substring(timestamp.IndexOf(":") - 2, 5)+ "\'";
                }
                if (dataValues == "")
                {
                    dataValues = record.GetValueByKey("_value").ToString();
                }
                else
                {
                    dataValues += ", " + record.GetValueByKey("_value").ToString();
                }

            });
        });

        Log.Info("Drawing Consumption Line.");

        // Insert values
        text = text.Replace("$DATAS$", dataNames);
        text = text.Replace("$VALUES$", dataValues);
        text = text.Replace("$xxxx$", selectedNode);

        // Write to file
        File.WriteAllText(filePath, text);
        // Refresh WebBrowser page
        Owner.Get<WebBrowser>("WebBrowser").Refresh();
       

        //Calculate Total Consumption & Emissions
        var flux2 = $"from(bucket: \"{bucket}\")" +
         $" |> range(start: {range})" +
         "|> drop(columns: [\"_start\", \"_stop\",])" +
         $"|> filter(fn: (r) => r[\"_measurement\"] == \"{selectedNode}\")" +
         "|> pivot(rowKey:[\"_time\"], columnKey: [\"_field\"], valueColumn: \"_value\")" +
         "|> sum(column: \"Energy\")" +
         "|> group()" +
         "|> yield(name: \"Energy\")";

        var tables2 = queryApi.QuerySync(flux2, orgID);
        tables2.ForEach(table =>
        {
            table.Records.ForEach(record =>
            {
                double totalConsumption = (double)record.GetValueByKey("Energy");
                Project.Current.GetVariable("Model/EnergyVariables/totalConsumptionVar").Value = Convert.ToInt16(totalConsumption).ToString() + consumptionUnit;

                if (selectedNode.Contains("Electric"))
                {
                    decimal emissions = Convert.ToDecimal(totalConsumption * 0.000433);
                    Project.Current.Get<Label>("UI/Screens/EnergyApplication/ConsumptionTemplate/Rectangle1/VerticalLayout1/Rectangle2/HorizontalLayout1/Label3").Text = "Total equivalent Emissions:";
                    Project.Current.GetVariable("Model/EnergyVariables/totalEmissionsVar").Value = emissions.ToString("F") + " tCO2";
                }
                else
                {
                    Project.Current.Get<Label>("UI/Screens/EnergyApplication/ConsumptionTemplate/Rectangle1/VerticalLayout1/Rectangle2/HorizontalLayout1/Label3").Text = "";
                    Project.Current.GetVariable("Model/EnergyVariables/totalEmissionsVar").Value = "";
                }
            });
            
        });
    }

    private static string bucket = Project.Current.GetVariable("Model/EnergySettings/bucket").Value;
    private static string orgID = Project.Current.GetVariable("Model/EnergySettings/OrgID").Value;
    private static string Token = Project.Current.GetVariable("Model/EnergySettings/Token").Value;
    private static string clientAddress = Project.Current.GetVariable("Model/EnergySettings/InfluxAddress").Value;

}
