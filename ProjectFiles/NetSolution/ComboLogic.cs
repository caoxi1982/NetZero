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
#endregion

public class ComboLogic : BaseNetLogic
{
    public override void Start()
    {
        GetData();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
    [ExportMethod]
    public void GetData()
    {
        string sqlQuery = Owner.GetVariable("MyQuery").Value;
        if (sqlQuery == null)
        {
            Log.Error("ComboBox", "Input query is null");
            return;
        }
        Store myDbStore = InformationModel.Get<Store>(Owner.GetVariable("MyDatabase").Value);
        IUANode myModelObject = Owner.Get("ComboObject");
        ComboBox myBox = (ComboBox)Owner;
        //string testquery = "SELECT \"GROUP\" FROM \"RecordShiftEnergy\" GROUP BY \"GROUP\"";
        // Prepare SQL Query
        Object[,] ResultSet;
        String[] Header;
        // Execute query and check result
        try
        {
            myDbStore.Query(sqlQuery, out Header, out ResultSet);
            if (ResultSet.GetLength(0) < 1)
            {
                Log.Error("ComboBox", "Input query returned less than one line");
                return;
            }
            // Delete all children from Object
            foreach (var children in myModelObject.Children)
            {
                children.Delete();
            }
            // For each column create an Object children
            for (int i = 0; i < ResultSet.GetLength(0); i++)
            {
                String columnName = Convert.ToString(ResultSet[i,0]);

                    var myObj = InformationModel.MakeVariable(columnName, OpcUa.DataTypes.String);
                    myObj.Value = columnName;
                    myModelObject.Add(myObj);
            }
            myBox.Refresh();
        }
        catch (Exception ex)
        {
            Log.Error("ComboBox", ex.Message);
            return;
        }
    }
}
