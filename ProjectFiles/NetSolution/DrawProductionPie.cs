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
using System.Reactive;
using FTOptix.WebUI;
using FTOptix.S7TCP;
using FTOptix.DataLogger;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
#endregion

public class DrawProductionPie : BaseNetLogic
{
    public override void Start()
    {
        // Render graph
        UpdatePie();
    }

    public override void Stop()
    {
    }

    [ExportMethod]
    public void UpdatePie()
    {
        string dataValues = "";
        //QueryVariables - From Energy Settings
        string selectedNode = Project.Current.GetVariable("Model/EnergyVariables/selectedNode").Value;
        Int16 timeInterval = Project.Current.GetVariable("Model/EnergyVariables/timePeriod").Value;
        string range = "-" + timeInterval.ToString() + "h";
        string equip_Prod = "Batch";


        // Clean for float and double data types, might be useless in this case
        dataValues = dataValues.Replace(", ", "¡ì");
        dataValues = dataValues.Replace(",", ".");
        dataValues = dataValues.Replace("¡ì", ", ");

        // Get template name and create destination path
        string templatePath = new ResourceUri(Owner.GetVariable("templatePath").Value).Uri;
        var filePath = Owner.Get<WebBrowser>("WebBrowser").URL.Uri;

        // Read template page content
        string text = File.ReadAllText(templatePath);

        using var client = new InfluxDBClient(clientAddress, Token);

        var flux = $"from(bucket: \"{bucket}\")" +
          $" |> range(start: {range})" +
          "|> drop(columns: [\"_start\", \"_stop\",])" +
          $"|> filter(fn: (r) => r[\"_measurement\"] == \"{selectedNode}\")" +
          $"|> filter(fn: (r) => r[\"_field\"] == \"Energy\" or r[\"_field\"] == \"{equip_Prod}\")" +
          "|> pivot(rowKey:[\"_time\"], columnKey: [\"_field\"], valueColumn: \"_value\")" +
          $"|> group(columns: [\"host\", \"{equip_Prod}\"], mode: \"by\")" +
          "|> sum(column: \"Energy\")" +
          "|> group()" +
          "|> yield(name: \"Energy\")";

        var queryApi = client.GetQueryApiSync();

        //Query Data and store result in dataValues & dataNames String
        var tables = queryApi.QuerySync(flux, orgID);
        tables.ForEach(table =>
        {
            table.Records.ForEach(record =>
            {
             
                if (dataValues == "")
                {
                    dataValues = "{value: " + record.GetValueByKey("Energy") + ", name: \'" + record.GetValueByKey($"{equip_Prod}").ToString() + "\'}";
                }
                else
                {
                    dataValues += ", " + "{value: " + record.GetValueByKey("Energy") + ", name: \'" + record.GetValueByKey($"{equip_Prod}").ToString() + "\'}";
                }

            });
        });

        Log.Info("Drawing Production Pie Chart.");
        // Insert values
        text = text.Replace("$VALUES$", dataValues);
        // Write to file
        File.WriteAllText(filePath, text);
       
        // Refresh WebBrowser page
            Owner.Get<WebBrowser>("WebBrowser").Refresh();

       }

    private static string bucket = Project.Current.GetVariable("Model/EnergySettings/bucket").Value;
    private static string orgID = Project.Current.GetVariable("Model/EnergySettings/OrgID").Value;
    private static string Token = Project.Current.GetVariable("Model/EnergySettings/Token").Value;
    private static string clientAddress = Project.Current.GetVariable("Model/EnergySettings/InfluxAddress").Value;


}
