#region Using directives
using System;
using UAManagedCore;
using FTOptix.HMIProject;
using FTOptix.Core;
using FTOptix.NetLogic;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Server;
using System.Security.Authentication;
using System.Net;
using MQTTnet;
using System.Collections.Generic;
using MQTTnet.Protocol;
using System.Linq;
using System.Net.Security;
using FTOptix.Alarm;
using System.Reflection;
using FTOptix.SerialPort;
using FTOptix.EthernetIP;
using FTOptix.S7TiaProfinet;
using FTOptix.Modbus;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.DataLogger;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using FTOptix.Recipe;
using FTOptix.OPCUAServer;
using FTOptix.CommunicationDriver;
using FTOptix.WebUI;
using Encoding = System.Text.Encoding;
using OpcUa = UAManagedCore.OpcUa;

#endregion


// About
// MQTTnet is a high performance .NET library for MQTT based communication.
// It provides a MQTT client and a MQTT server (broker).
// The implementation is based on the documentation from http://mqtt.org/.

// Nuget https://www.nuget.org/packages/MQTTnet/
// Official page https://github.com/chkr1011/MQTTnet

// License
// MIT License

// MQTTnet Copyright (c) 2016-2021 Christian Kratky

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:

// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
// THE USE OR OTHER DEALINGS IN THE SOFTWARE.

public class MQTTLogic : BaseNetLogic
{
    public override void Start()
    {
        //Initialize Dictionary for QoS
        InitQoSDictionary();

        //Retrieve the value of parameters associated with this runtime script
        ReadMQTTBrokerConfigurationVariables();

        sentPackages.Value = 0;
        receivedPackages.Value = 0;

        //Logic for keeping the broker server instance task active
        mqttServerAutoResetEvent = new AutoResetEvent(false);

        //Logic for keeping the client broker instance task active
        mqttClientAutoResetEvent = new AutoResetEvent(false);

        //Logic to bind the script to the current FactoryTalk Optix app session
        defaultNamespaceIndex = LogicObject.NodeId.NamespaceIndex;
        sessionHandler = LogicObject.Context.Sessions.CurrentSessionHandler;

        //Starts the broker server instance, if MqttAsServer and Autostart parameter have value true
        if (mqttAsServer.Value && mqttAsServerAutoStartVariable.Value)
            StartMQTTServer();

        //Starts the client broker instance, if the parameter MqttAsClient
        if (mqttAsClient.Value)
            StartMQTTClient();

        //Start publishing Live Tags
        if (mqttAsClient.Value && publisher.Value && publisherLiveTags.Value)
            StartListeningToVariables();

        //Avvio pubblicazione Custom Payload
        if (mqttAsClient.Value && publisher.Value && publisherCustomPayload.Value)
            StartCustomPayload();

        //Start publishing DB tables
        if (mqttAsClient.Value && publisher.Value && publisherStoreTables.Value)
            StartPublisherTables();

        //StartStop Server, Client, Publisher and Subscriber Management
        mqttAsServer.VariableChange += MqttAsServer_VariableChange;
        mqttAsClient.VariableChange += MqttAsClient_VariableChange;
        publisher.VariableChange += Publisher_VariableChange;
        publisherCustomPayload.VariableChange += PublisherCustomPayload_VariableChange;
        publisherLiveTags.VariableChange += PublisherLiveTags_VariableChange;
        subscriber.VariableChange += Subscriber_VariableChange;
        subscriberLiveTags.VariableChange += SubscriberLiveTags_VariableChange;
        subscriberCustomPayload.VariableChange += SubscriberCustomPayload_VariableChange;
    }

    public override void Stop()
    {
        closing = true;

        StopMQTTBroker();
        StopMQTTClient();

        if (variableSynchronizer != null)
            variableSynchronizer.Dispose();

        if (publisherLiveTagsFolder != null)
        {
            var node = InformationModel.Get(publisherLiveTagsFolder.Value);
            if (node != null)
            {
                liveVariables = GetNodesIntoFolder<UAVariable>(node);

                if ((double)publisherLiveTagsPeriod.Value == 0)
                    foreach (UAVariable v in liveVariables)
                    {
                        v.VariableChange -= OnVariableChange;
                    }
            }

        }

        if (liveDataTask != null)
            liveDataTask.Dispose();

        if (customPayloadTask != null)
            customPayloadTask.Dispose();

        if (myLRT != null)
            myLRT.Dispose();

        if (delayedTask != null)
            delayedTask.Dispose();

        if (mqttClientInstantiation != null)
            mqttClientInstantiation.Dispose();
    }

    private void SubscriberCustomPayload_VariableChange(object sender, VariableChangeEventArgs e)
    {
        if (mqttClient != null)
        {
            if (subscriber.Value)
            {
                if (subscriberCustomPayload.Value)
                    mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                        .WithTopic((string)subscriberCustomPayloadTopic.Value)
                        .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)1)
                        .Build());
                else
                {
                    string[] topics = new string[1];
                    topics[0] = "";

                    if (subscriberCustomPayloadTopic.Value != null)
                        topics[0] = (string)subscriberCustomPayloadTopic.Value;

                    mqttClient.UnsubscribeAsync(topics);
                }

            }
        }
    }

    private void PublisherCustomPayload_VariableChange(object sender, VariableChangeEventArgs e)
    {
        if (publisher.Value && publisherCustomPayload.Value)
            StartCustomPayload();

        if (!publisherCustomPayload.Value)
        {
            publisherCustomPayloadMessage.VariableChange -= OnVariableChange;

            if (customPayloadTask != null)
                customPayloadTask.Dispose();
        }
    }

    private void SubscriberLiveTags_VariableChange(object sender, VariableChangeEventArgs e)
    {
        if (mqttClient != null)
        {
            if (subscriber.Value)
            {
                if (subscriberLiveTags.Value)
                    mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                        .WithTopic((string)subscriberLiveTagsTopic.Value)
                        .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)1)
                        .Build());
                else
                {
                    string[] topics = new string[1];
                    topics[0] = "";

                    if (subscriberLiveTagsTopic.Value != null)
                        topics[0] = (string)subscriberLiveTagsTopic.Value;

                    mqttClient.UnsubscribeAsync(topics);
                }

            }
        }
    }

    private void Subscriber_VariableChange(object sender, VariableChangeEventArgs e)
    {
        if (mqttClient != null)
        {
            if (!subscriber.Value)
            {
                string[] topics = new string[2];
                topics[0] = "";
                topics[1] = "";

                if (subscriberLiveTagsTopic.Value != null)
                    topics[0] = (string)subscriberLiveTagsTopic.Value;
                if (subscriberStoreTablesStoreTablesTopic.Value != null)
                    topics[1] = (string)subscriberStoreTablesStoreTablesTopic.Value;

                mqttClient.UnsubscribeAsync(topics);
            }
            else
            {
                if (subscriber.Value && subscriberLiveTags.Value)
                    mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                        .WithTopic((string)subscriberLiveTagsTopic.Value)
                        .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)1)
                        .Build());

                if (subscriber.Value && subscriberStoreTables.Value)
                    mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                        .WithTopic((string)subscriberStoreTablesStoreTablesTopic.Value)
                        .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)1)
                        .Build());

                if (subscriber.Value && subscriberCustomPayload.Value)
                    mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                        .WithTopic((string)subscriberCustomPayloadTopic.Value)
                        .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)1)
                        .Build());
            }
        }
    }

    private void PublisherLiveTags_VariableChange(object sender, VariableChangeEventArgs e)
    {
        if (publisher.Value && publisherLiveTags.Value)
            StartListeningToVariables();
        if (!publisherLiveTags.Value && variableSynchronizer != null)
        {
            if (variableSynchronizer != null)
                variableSynchronizer.Dispose();

            liveVariables = GetNodesIntoFolder<UAVariable>(InformationModel.Get(publisherLiveTagsFolder.Value));
            if ((double)publisherLiveTagsPeriod.Value == 0)
                foreach (UAVariable v in liveVariables)
                {
                    v.VariableChange -= OnVariableChange;
                }
            if (liveDataTask != null)
                liveDataTask.Dispose();
        }

    }

    private void Publisher_VariableChange(object sender, VariableChangeEventArgs e)
    {
        if (mqttClient != null)
        {
            if (publisher.Value && publisherLiveTags.Value)
                StartListeningToVariables();
            if (publisher.Value && publisherCustomPayload.Value)
                StartCustomPayload();

            if (!publisher.Value && variableSynchronizer != null)
            {
                if (variableSynchronizer != null)
                    variableSynchronizer.Dispose();

                liveVariables = GetNodesIntoFolder<UAVariable>(InformationModel.Get(publisherLiveTagsFolder.Value));
                if ((double)publisherLiveTagsPeriod.Value == 0)
                    foreach (UAVariable v in liveVariables)
                    {
                        v.VariableChange -= OnVariableChange;
                    }
                if (liveDataTask != null)
                    liveDataTask.Dispose();
                if (customPayloadTask != null)
                    customPayloadTask.Dispose();
            }
        }
    }

    private void MqttAsClient_VariableChange(object sender, VariableChangeEventArgs e)
    {
        if ((mqttAsClientConnected == null || mqttAsClient == null) && (mqttAsClient.Value))
        {
            ReadMQTTBrokerConfigurationVariables();
            StartMQTTClient();

            //Start publishing Live Tags
            if (mqttAsClient.Value && publisher.Value && publisherLiveTags.Value)
                StartListeningToVariables();

            //Start publishing DB tables
            if (mqttAsClient.Value && publisher.Value && publisherStoreTables.Value)
                StartPublisherTables();
        }
        else
        {
            if (mqttAsClient.Value && !mqttAsClientConnected.Value)
            {
                ReadMQTTBrokerConfigurationVariables();
                StartMQTTClient();

                //Start publishing Live Tags
                if (mqttAsClient.Value && publisher.Value && publisherLiveTags.Value)
                    StartListeningToVariables();

                //Start publishing DB tables
                if (mqttAsClient.Value && publisher.Value && publisherStoreTables.Value)
                    StartPublisherTables();
            }
            if (!mqttAsClient.Value)
                StopMQTTClient();
        }
    }

    private void MqttAsServer_VariableChange(object sender, VariableChangeEventArgs e)
    {
        if ((mqttAsServerIsRunningVariable == null || mqttAsServer == null) && (mqttAsServer.Value))
        {
            ReadMQTTBrokerConfigurationVariables();
            StartMQTTServer();
        }
        else
        {
            if (mqttAsServer.Value && !mqttAsServerIsRunningVariable.Value)
            {
                ReadMQTTBrokerConfigurationVariables();
                StartMQTTServer();
            }
            if (!mqttAsServer.Value)
                StopMQTTBroker();
        }
    }

    private void InitQoSDictionary()
    {
        mqttQoS = new Dictionary<int, MqttQualityOfServiceLevel>();
        mqttQoS.Add(0, MqttQualityOfServiceLevel.AtMostOnce);
        mqttQoS.Add(1, MqttQualityOfServiceLevel.AtLeastOnce);
        mqttQoS.Add(2, MqttQualityOfServiceLevel.ExactlyOnce);
    }

    /// <summary>
    /// Reads the parameters (variables) associated with the script in the development environment
    /// </summary>
    private void ReadMQTTBrokerConfigurationVariables()
    {
        mqttAsServer = LogicObject.GetVariable("MqttServer");
        if (mqttAsServer.Value)
        {
            mqttAsServerIpAddressVariable = LogicObject.GetVariable("MqttServer/IPAddress");
            if (mqttAsServerIpAddressVariable == null)
                throw new CoreConfigurationException("Server IPAddress variable not found");

            mqttAsServerPortVariable = LogicObject.GetVariable("MqttServer/Port");
            if (mqttAsServerPortVariable == null)
                throw new CoreConfigurationException("Server Port variable not found");

            mqttAsServerUseSSLVariable = LogicObject.GetVariable("MqttServer/UseSSL");
            if (mqttAsServerUseSSLVariable == null)
                throw new CoreConfigurationException("Server UseSSL variable not found");

            if (mqttAsServerUseSSLVariable.Value)
            {
                mqttAsServerCertificateVariable = LogicObject.GetVariable("MqttServer/UseSSL/Certificate");
                if (mqttAsServerCertificateVariable == null)
                    throw new CoreConfigurationException("Server Certificate variable not found");

                mqttAsServerCertificatePasswordVariable = LogicObject.GetVariable("MqttServer/UseSSL/CertificatePassword");
                if (mqttAsServerCertificatePasswordVariable == null)
                    throw new CoreConfigurationException("Server CertificatePassword variable not found");
            }

            mqttAsServerUserAuthenticationVariable = LogicObject.GetVariable("MqttServer/UserAuthentication");
            if (mqttAsServerUserAuthenticationVariable == null)
                throw new CoreConfigurationException("Server UserAuthentication variable not found");

            if (mqttAsServerUserAuthenticationVariable.Value)
            {
                mqttAsServerAuthorizedUsersVariable = LogicObject.GetVariable("MqttServer/UserAuthentication/AuthorizedUsers");
                if (mqttAsServerAuthorizedUsersVariable == null)
                    throw new CoreConfigurationException("Server AuthorizedUsers variable not found");
            }

            mqttAsServerAutoStartVariable = LogicObject.GetVariable("MqttServer/AutoStart");
            if (mqttAsServerAutoStartVariable == null)
                throw new CoreConfigurationException("Server AutoStart variable not found");

            mqttAsServerMaxNumberOfConnectionsVariable = LogicObject.GetVariable("MqttServer/MaxNumberOfConnections");
            if (mqttAsServerMaxNumberOfConnectionsVariable == null)
                throw new CoreConfigurationException("Server MaxNumberOfConnections variable not found");

            mqttAsServerNumberOfConnections = LogicObject.GetVariable("MqttServer/NumberOfConnections");

            mqttAsServerIsRunningVariable = LogicObject.GetVariable("MqttServer/IsRunning");
            if (mqttAsServerIsRunningVariable == null)
                throw new CoreConfigurationException("Server IsRunning variable not found");

            mqttAsServerIsDebuggingModeVariable = LogicObject.GetVariable("MqttServer/IsDebuggingMode");
            if (mqttAsServerIsDebuggingModeVariable == null)
                throw new CoreConfigurationException("Server IsDebuggingMode variable not found");
        }

        mqttAsClient = LogicObject.GetVariable("MqttClient");

        sentPackages = LogicObject.GetVariable("MqttClient/SentPackages");
        receivedPackages = LogicObject.GetVariable("MqttClient/ReceivedPackages");

        if (mqttAsClient.Value)
        {
            mqttAsClientIpAddressVariable = LogicObject.GetVariable("MqttClient/IPAddress");
            if (mqttAsClientIpAddressVariable == null)
                throw new CoreConfigurationException("Client IPAddress variable not found");

            mqttAsClientPortVariable = LogicObject.GetVariable("MqttClient/Port");
            if (mqttAsClientPortVariable == null)
                throw new CoreConfigurationException("Client Port variable not found");

            mqttAsClientClientIdVariable = LogicObject.GetVariable("MqttClient/ClientId");
            if (mqttAsClientPortVariable == null)
                throw new CoreConfigurationException("Client ClientId variable not found");

            mqttAsClientUserAuthenticationVariable = LogicObject.GetVariable("MqttClient/UserAuthentication");
            if (mqttAsClientUserAuthenticationVariable == null)
                throw new CoreConfigurationException("Client UserAuthentication variable not found");

            if (mqttAsClientUserAuthenticationVariable.Value)
            {
                mqttAsClientAuthorizedUsersVariable = LogicObject.GetVariable("MqttClient/UserAuthentication/AuthorizedUsers");
                if (mqttAsClientAuthorizedUsersVariable == null)
                    throw new CoreConfigurationException("Client AuthorizedUsers variable not found");
            }


            mqttAsClientUseSSLVariable = LogicObject.GetVariable("MqttClient/UseSSL");
            if (mqttAsClientUseSSLVariable == null)
                throw new CoreConfigurationException("Client UseSSL variable not found");

            if (mqttAsClientUseSSLVariable.Value)
            {
                mqttAsClientCaCertificateVariable = LogicObject.GetVariable("MqttClient/UseSSL/CaCertificate");
                if (mqttAsClientCaCertificateVariable == null)
                    throw new CoreConfigurationException("Client CA Certificate variable not found");

                mqttAsClientClientCertificateVariable = LogicObject.GetVariable("MqttClient/UseSSL/ClientCertificate");
                if (mqttAsClientClientCertificateVariable == null)
                    throw new CoreConfigurationException("Client Certificate variable not found");

                mqttAsClientCertificatePasswordVariable = LogicObject.GetVariable("MqttClient/UseSSL/ClientCertificatePassword");
                if (mqttAsClientCertificatePasswordVariable == null)
                    throw new CoreConfigurationException("Client CertificatePassword variable not found");

                mqttAsClientAllowUntrustedCertificates = LogicObject.GetVariable("MqttClient/UseSSL/AllowUntrustedCertificates");
            }


            mqttAsClientConnected = LogicObject.GetVariable("MqttClient/Connected");

        }
        //publisher parameters
        publisher = LogicObject.GetVariable("Publisher");

        //publisher live data parameters
        publisherLiveTags = LogicObject.GetVariable("Publisher/LiveTags");
        publisherLiveTagsFolder = LogicObject.GetVariable("Publisher/LiveTags/LiveTagsFolder");
        publisherLiveTagsPeriod = LogicObject.GetVariable("Publisher/LiveTags/LiveTagsPeriod");
        publisherLiveTagsTopic = LogicObject.GetVariable("Publisher/LiveTags/LiveTagsTopic");
        publisherLiveTagsQoS = LogicObject.GetVariable("Publisher/LiveTags/QoS");
        publisherLiveTagsRetain = LogicObject.GetVariable("Publisher/LiveTags/Retain");

        //publisher custom payload parameters
        publisherCustomPayload = LogicObject.GetVariable("Publisher/CustomPayload");
        publisherCustomPayloadPeriod = LogicObject.GetVariable("Publisher/CustomPayload/CustomPayloadPeriod");
        publisherCustomPayloadTopic = LogicObject.GetVariable("Publisher/CustomPayload/CustomPayloadTopic");
        publisherCustomPayloadMessage = LogicObject.GetVariable("Publisher/CustomPayload/CustomPayloadMessage");
        publisherCustomPayloadQoS = LogicObject.GetVariable("Publisher/CustomPayload/QoS");
        publisherCustomPayloadRetain = LogicObject.GetVariable("Publisher/CustomPayload/Retain");


        //publisher tables parameters
        publisherStoreTables = LogicObject.GetVariable("Publisher/StoreTables");
        publisherStoreTablesStore = LogicObject.GetVariable("Publisher/StoreTables/Store");
        publisherStoreTablesTableNames = LogicObject.GetVariable("Publisher/StoreTables/TableNames");
        publisherStoreTablesPreserveData = LogicObject.GetVariable("Publisher/StoreTables/PreserveData");
        publisherStoreTablesMaximumItemsPerPacket = LogicObject.GetVariable("Publisher/StoreTables/MaximumItemsPerPacket");
        publisherStoreTablesMaximumPublishTime = LogicObject.GetVariable("Publisher/StoreTables/MaximumPublishTime");
        publisherStoreTablesMinimumPublishTime = LogicObject.GetVariable("Publisher/StoreTables/MinimumPublishTime");
        publisherStoreTablesStoreTablesTopic = LogicObject.GetVariable("Publisher/StoreTables/StoreTablesTopic");
        publisherStoreTablesQoS = LogicObject.GetVariable("Publisher/StoreTables/QoS");
        publisherStoreTablesRetain = LogicObject.GetVariable("Publisher/StoreTables/Retain");
        publisherStoreTablesAllRows = LogicObject.GetVariable("Publisher/StoreTables/AllRows");
        publisherStoreTablesTablesPrefix = LogicObject.GetVariable("Publisher/StoreTables/TablesPrefix");

        //subscriber parameters
        subscriber = LogicObject.GetVariable("Subscriber");

        //subscriber live data parameters
        subscriberLiveTags = LogicObject.GetVariable("Subscriber/LiveTags");
        subscriberLiveTagsFolder = LogicObject.GetVariable("Subscriber/LiveTags/LiveTagsFolder");
        subscriberLiveTagsTopic = LogicObject.GetVariable("Subscriber/LiveTags/LiveTagsTopic");
        subscriberLiveTagsLastPackageTimestamp = LogicObject.GetVariable("Subscriber/LiveTags/LastPackageTimestamp");

        //subscriber custom payload parameters
        subscriberCustomPayload = LogicObject.GetVariable("Subscriber/CustomPayload");
        subscriberCustomPayloadMessage = LogicObject.GetVariable("Subscriber/CustomPayload/CustomPayloadMessage");
        subscriberCustomPayloadTopic = LogicObject.GetVariable("Subscriber/CustomPayload/CustomPayloadTopic");

        //subscriber tables parameters
        subscriberStoreTables = LogicObject.GetVariable("Subscriber/StoreTables");
        subscriberStoreTablesStore = LogicObject.GetVariable("Subscriber/StoreTables/Store");
        subscriberStoreTablesStoreTablesTopic = LogicObject.GetVariable("Subscriber/StoreTables/StoreTablesTopic");


    }


    #region MQTTClient
    private void StartMQTTClient()
    {
        if (mqttClient != null && mqttClient.IsStarted)
        {
            Log.Error(MethodBase.GetCurrentMethod().Name, "Mqtt Client it is already running");
            return;
        }

        mqttClientInstantiation = new LongRunningTask(MQTTClientInstantiation, LogicObject);
        mqttClientInstantiation.Start();
    }

    private void StopMQTTClient()
    {
        if (mqttClient != null)
        {
            StopAndResetMQTTClient();
            ResetMQTTClientLongRunningTask();
        }
    }

    private void StopAndResetMQTTClient()
    {
        try
        {
            mqttClient.StopAsync().Wait();
            Log.Info("MQTTBrokerLogic", "MQTT client stopped");
        }
        catch (Exception ex)
        {
            Log.Error(MethodBase.GetCurrentMethod().Name, $"An exception occurred while stopping the MQTT Client: {ex.Message}");
        }
        finally
        {
            //Close the long running task, which remained "hanging" with "WaitOne()"
            mqttClientAutoResetEvent.Set();

            mqttClient?.Dispose();
            mqttClient = null;

            mqttAsClientConnected.Value = false;
        }
    }

    private void ResetMQTTClientLongRunningTask()
    {
        mqttClientInstantiation?.Dispose();
        mqttClientInstantiation = null;
    }

    /// <summary>
    /// Connection of the Client to the Broker
    /// </summary>
    private void MQTTClientInstantiation()
    {
        try
        {
            // Creates a new client
            MqttClientOptionsBuilder builder = new MqttClientOptionsBuilder()
                                                    .WithClientId((string)mqttAsClientClientIdVariable.Value)
                                                    .WithTcpServer((string)mqttAsClientIpAddressVariable.Value, (int)mqttAsClientPortVariable.Value);

            if (mqttAsClientUserAuthenticationVariable.Value)
            {
                Log.Info("User Security Password Changed!");
                //string[] userArray = mqttAsClientAuthorizedUsersVariable.Value;
                //User user = Project.Current.Find<User>(userArray[0]);
                //builder.WithCredentials(user.BrowseName, user.Password);
            }

            if (mqttAsClientUseSSLVariable.Value)
            {
                string pathCACert = ResourceUriValueToAbsoluteFilePath((string)mqttAsClientCaCertificateVariable.Value);
                string pathClientCert = ResourceUriValueToAbsoluteFilePath((string)mqttAsClientClientCertificateVariable.Value);

                var caCert = X509Certificate.CreateFromCertFile(@pathCACert);
                var clientCert = new X509Certificate2(@pathClientCert, (string)mqttAsClientCertificatePasswordVariable.Value);
                builder.WithTls(new MqttClientOptionsBuilderTlsParameters()
                {
                    UseTls = true,
                    SslProtocol = System.Security.Authentication.SslProtocols.Tls12,
                    AllowUntrustedCertificates = (bool)mqttAsClientAllowUntrustedCertificates.Value,
                    Certificates = new List<X509Certificate>()
                    {
                        clientCert, caCert
                    }
                }).Build();
            }

            // Create client options objects
            ManagedMqttClientOptions options = new ManagedMqttClientOptionsBuilder()
                                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(60))
                                    .WithClientOptions(builder.Build())
                                    .Build();

            // Creates the client object
            mqttClient = new MqttFactory().CreateManagedMqttClient();

            // Subscribes to topics
            if (subscriber.Value && subscriberLiveTags.Value)
                mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic((string)subscriberLiveTagsTopic.Value)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)1)
                    .Build());

            if (subscriber.Value && subscriberStoreTables.Value)
            {
                mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic((string)subscriberStoreTablesStoreTablesTopic.Value)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)1)
                    .Build());
            }

            if (subscriber.Value && subscriberCustomPayload.Value)
            {
                mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic((string)subscriberCustomPayloadTopic.Value)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)1)
                    .Build());
            }

            mqttClient.UseApplicationMessageReceivedHandler(e => ClientReceivedMessage(e));

            // Set up handlers
            mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnConnected);
            mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnDisconnected);
            mqttClient.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate(OnConnectingFailed);
            mqttClient.ApplicationMessageProcessedHandler = new ApplicationMessageProcessedHandlerDelegate(OnMessageProcessed);

            // Starts a connection with the Broker
            mqttClient.StartAsync(options).GetAwaiter().GetResult();

        }
        catch (Exception ex)
        {
            Log.Error(MethodBase.GetCurrentMethod().Name, $"An exception occurred while starting the MQTT broker client: {ex.Message}");

            mqttServer?.Dispose();
            mqttServer = null;
        }
    }

    public void OnConnected(MqttClientConnectedEventArgs obj)
    {
        mqttAsClientConnected.Value = true;
        Log.Info("Successfully connected.");
    }

    public void OnConnectingFailed(ManagedProcessFailedEventArgs obj)
    {
        mqttAsClientConnected.Value = false;
        Log.Info("Couldn't connect to broker.");
        Log.Info("Next retry in 1 minute...");
    }

    public void OnDisconnected(MqttClientDisconnectedEventArgs obj)
    {
        mqttAsClientConnected.Value = false;
        Log.Info("Broker Disconnection");
    }

    public void OnMessageProcessed(ApplicationMessageProcessedEventArgs obj)
    {
        if (obj.ApplicationMessage.ApplicationMessage.Topic == (string)publisherStoreTablesStoreTablesTopic.Value)
        {
            pendingPackage--;
            if (pendingPackage < 0)
                pendingPackage = 0;
            Log.Info("Pending Packages: " + pendingPackage.ToString());
        }

    }

    private void ClientReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        receivedPackages.Value++;

        JObject jsonObject = JObject.Parse(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));

        if (e.ApplicationMessage.Topic == (string)subscriberLiveTagsTopic.Value)
        {
            LiveTagsDataHandler(jsonObject);
        }
        else if (e.ApplicationMessage.Topic == (string)subscriberStoreTablesStoreTablesTopic.Value)
        {
            StoreData(jsonObject);
        }
        else if (e.ApplicationMessage.Topic == (string)subscriberCustomPayloadTopic.Value)
        {
            CustomPayload(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
        }
    }

    private void StoreData(JObject o)
    {
        try
        {
            string tableName = (string)o.Property("TableName");
            string[] columns = null;
            object[,] values = null;

            var rows = o.SelectToken("$.Rows");
            int rowsCounter = 0;
            foreach (var item in rows)
            {
                rowsCounter++;
            }
            var rowElements = rows.First.SelectToken("$.Variables");
            int rowElementsCounter = 0;
            foreach (var element in rowElements)
            {
                rowElementsCounter++;
            }
            values = new object[rowsCounter, rowElementsCounter];
            int rowCounterTemp = 0;
            foreach (var item in rows)
            {
                var row = (JObject)item;
                var variables = row.SelectToken("$.Variables");
                columns = new string[rowElementsCounter];
                rowElementsCounter = 0;
                foreach (var itemRow in variables)
                {
                    var variable = (JObject)itemRow;
                    var variableName = (string)variable.Property("VariableName");
                    var variableValue = variable.Property("Value");
                    var variableType = (string)variable.Property("VariableType");
                    columns[rowElementsCounter] = variableName;

                    switch (variableType)
                    {
                        case "Int16":
                            values[rowCounterTemp, rowElementsCounter] = (Int16)variableValue;
                            break;
                        case "Int32":
                            values[rowCounterTemp, rowElementsCounter] = (Int32)variableValue;
                            break;
                        case "Int64":
                            values[rowCounterTemp, rowElementsCounter] = (Int64)variableValue;
                            break;
                        case "UInt16":
                            values[rowCounterTemp, rowElementsCounter] = (UInt16)variableValue;
                            break;
                        case "UInt32":
                            values[rowCounterTemp, rowElementsCounter] = (UInt32)variableValue;
                            break;
                        case "UInt64":
                            values[rowCounterTemp, rowElementsCounter] = (UInt64)variableValue;
                            break;
                        case "Boolean":
                            values[rowCounterTemp, rowElementsCounter] = (Boolean)variableValue;
                            break;
                        case "String":
                            values[rowCounterTemp, rowElementsCounter] = (string)variableValue;
                            break;
                        case "UtcTime":
                        case "DateTime":
                            values[rowCounterTemp, rowElementsCounter] = (DateTime)variableValue;
                            break;
                        default:
                            break;
                    }
                    rowElementsCounter++;
                }
                rowCounterTemp++;
            }
            //var values = new object[,] { { timestamp, localTimestamp, id, modbusTag1, modbusTag2, modbusTag3 } };

            Store processDataDb = InformationModel.Get<Store>(subscriberStoreTablesStore.Value);
            var tblProcessData = processDataDb.Tables.Get<Table>(tableName);
            tblProcessData.Insert(columns, values);
        }
        catch (Exception e)
        {

            Log.Info(e.ToString());
        }
    }

    private void LiveTagsDataHandler(JObject o)
    {
        try
        {
            if (o.Property("Timestamp") != null)
                subscriberLiveTagsLastPackageTimestamp.Value = (DateTime)o.Property("Timestamp");

            var records = o.SelectToken("$.Records");
            foreach (var item in records)
            {
                var record = (JObject)item;
                string tagName = (string)record.Property("TagName");
                var tagValue = record.Property("Value");
                switch (InformationModel.Get((InformationModel.Get(subscriberLiveTagsFolder.Value)).GetVariable(tagName).DataType).BrowseName)
                {
                    case "String":
                        (InformationModel.Get(subscriberLiveTagsFolder.Value)).GetVariable(tagName).Value = (string)tagValue;
                        break;
                    case "Boolean":
                        (InformationModel.Get(subscriberLiveTagsFolder.Value)).GetVariable(tagName).Value = (Boolean)tagValue;
                        break;
                    case "Int16":
                        (InformationModel.Get(subscriberLiveTagsFolder.Value)).GetVariable(tagName).Value = (Int16)tagValue;
                        break;
                    case "Int32":
                        (InformationModel.Get(subscriberLiveTagsFolder.Value)).GetVariable(tagName).Value = (Int32)tagValue;
                        break;
                    case "Float":
                        (InformationModel.Get(subscriberLiveTagsFolder.Value)).GetVariable(tagName).Value = (Double)tagValue;
                        break;
                    default:
                        break;
                }
            }
        }
        catch (Exception e)
        {

            Log.Info(e.ToString());
        }
    }

    private void CustomPayload(string message)
    {
        subscriberCustomPayloadMessage.Value = message;
    }

    #endregion

    #region MQTTServer

    /// <summary>
    /// Starts the task of instantiating and starting the broker unless the broker has already been started
    /// </summary>

    [ExportMethod]
    public void StartMQTTServer()
    {
        if (mqttServer != null && mqttServer.IsStarted)
        {
            Log.Error(MethodBase.GetCurrentMethod().Name, "Unable to start the MQTT broker, it is already running");
            return;
        }

        mqttServerInstantiation = new LongRunningTask(MQTTServerInstantiation, LogicObject);
        mqttServerInstantiation.Start();
    }

    /// <summary>
    /// Stops and resets the broker instance and then the task dedicated to keeping it active
    /// </summary>
    private void StopMQTTBroker()
    {
        if (mqttServer != null)
        {
            StopAndResetMQTTBrokerServer();
            ResetMQTTServerBrokerLongRunningTask();
        }
    }

    private void StopAndResetMQTTBrokerServer()
    {
        try
        {
            mqttServer.StopAsync().Wait();
            Log.Info("MQTTBrokerLogic", "MQTT server broker stopped");
        }
        catch (Exception ex)
        {
            Log.Error(MethodBase.GetCurrentMethod().Name, $"An exception occurred while stopping the MQTT server broker: {ex.Message}");
        }
        finally
        {
            //I close the long running task, which remained "hanging" with "WaitOne()"
            mqttServerAutoResetEvent.Set();

            mqttServer?.Dispose();
            mqttServer = null;

            mqttAsServerIsRunningVariable.Value = false;
            connectedClientIdList.Clear();
        }
    }

    private void ResetMQTTServerBrokerLongRunningTask()
    {
        mqttServerInstantiation?.Dispose();
        mqttServerInstantiation = null;
    }

    /// <summary>
    /// Gets the parameters with which to instantiate the broker, then instantiates it
    /// </summary>
    private void MQTTServerInstantiation()
    {
        try
        {
            var mqttServerParameters = ReadMQTTServerParameters();
            if (mqttServerParameters.IsEmpty())
            {
                Log.Error("Parameters setup error: please check script parameters");
                return;
            }

            MqttServerOptionsBuilder optionsBuilder = InitializeMQTTBrokerOptions(mqttServerParameters);

            mqttServer = new MqttFactory().CreateMqttServer();

            //Uncomment the next line to activate a message management logic received by the broker
            //mqttServer.UseApplicationMessageReceivedHandler(e => ProcessReceivedMessage(e));

            mqttServer.ClientConnectedHandler = new MqttServerClientConnectedHandlerDelegate(e => OnMQTTClientConnected(e));
            mqttServer.ClientDisconnectedHandler = new MqttServerClientDisconnectedHandlerDelegate(e => OnMQTTClientDisconnected(e));

            mqttServer.StartAsync(optionsBuilder.Build()).Wait();

            Log.Info("MQTTBrokerLogic", $"MQTT broker server started, endpoint {mqttServerParameters.ipAddress}:{mqttServerParameters.port}");
            mqttAsServerIsRunningVariable.Value = true;

            mqttServerAutoResetEvent.WaitOne();
        }
        catch (Exception ex)
        {
            Log.Error(MethodBase.GetCurrentMethod().Name, $"An exception occurred while starting the MQTT broker server: {ex.Message}");

            mqttServer?.Dispose();
            mqttServer = null;
        }
    }

    /// <summary>
    /// Processes a received message
    /// </summary>
    /// <param name="e"></param>
    private void ProcessReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        var clientId = e.ClientId;
        var topic = e.ApplicationMessage.Topic;
        var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

        if (mqttAsServerIsDebuggingModeVariable.Value)
            Log.Info($"### RECEIVED APPLICATION MESSAGE ### Client [{clientId}], Topic = {topic}, Payload = {payload}, QoS = {e.ApplicationMessage.QualityOfServiceLevel}, Retain = {e.ApplicationMessage.Retain}");
    }

    private void OnMQTTClientConnected(MqttServerClientConnectedEventArgs e)
    {
        mqttAsServerNumberOfConnections.Value = connectedClientIdList.Count;
    }

    /// <summary>
    /// Updates the list of clients connected to the broker, at the moment of the disconnection of one of them
    /// </summary>
    /// <param name="e"></param>
    private void OnMQTTClientDisconnected(MqttServerClientDisconnectedEventArgs e)
    {
        Log.Info("MQTTBrokerLogic", $"MQTT client '{e.ClientId}' has been disconnected");
        connectedClientIdList.Remove(e.ClientId);
        mqttAsServerNumberOfConnections.Value = connectedClientIdList.Count;
    }

    /// <summary>
    /// Reads broker configuration parameters and sets broker options.
    /// </summary>
    private MqttServerOptionsBuilder InitializeMQTTBrokerOptions(MQTTServerParameters mqttServerParameters)
    {
        var optionsBuilder = new MqttServerOptionsBuilder()
            .WithConnectionBacklog(connectionBacklog) // queued connections
            .WithConnectionValidator(context =>
            {
                if (ValidateMQTTClient(context))
                    connectedClientIdList.Add(context.ClientId);
            })
            .WithApplicationMessageInterceptor(context =>
            {

            })
            .WithSubscriptionInterceptor(context =>
            {
                Log.Info("MQTTBrokerLogic", $"MQTT client '{context.ClientId}' subscribed to the MQTT broker on the topic {context.TopicFilter.Topic}");
            });

        if (mqttServerParameters.HasSSL())
        {
            optionsBuilder.WithoutDefaultEndpoint() // This call disables the default unencrypted endpoint on port 1883
                .WithEncryptedEndpoint()
                .WithEncryptedEndpointBoundIPAddress(mqttServerParameters.ipAddress)
                .WithEncryptedEndpointPort(mqttServerParameters.port)
                .WithDefaultEndpointBoundIPV6Address(IPAddress.None)
                .WithEncryptionCertificate(mqttServerParameters.sslParameters.certificate.Export(X509ContentType.Pfx))
                .WithEncryptionSslProtocol(SslProtocols.Tls12)
                .WithClientCertificate(ValidateClientCertificate, true);
        }
        else
        {
            optionsBuilder.WithDefaultEndpointBoundIPAddress(mqttServerParameters.ipAddress)
                .WithDefaultEndpointPort(mqttServerParameters.port)
                .WithDefaultEndpointBoundIPV6Address(IPAddress.None);
        }

        return optionsBuilder;
    }

    private bool ValidateClientCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
    {
        return true;
    }

    /// <summary>
    /// Validates the client that is attempting to connect to the broker
    /// In particular: if the broker cannot accept more connected clients than the current number, it refuses the connection.
    /// </summary>
    private bool ValidateMQTTClient(MqttConnectionValidatorContext context)
    {
        try
        {
            Log.Info("MQTTBrokerLogic", $"MQTT client '{context.ClientId}' is trying to connect to the MQTT broker");
            if (BrokerIsFull())
            {
                context.ReasonCode = MqttConnectReasonCode.ServerUnavailable;
                Log.Info("MQTTBrokerLogic", $"MQTT client '{context.ClientId}' connection the MQTT broker failed.");
                return false;
            }

            string username = context.Username;
            string password = context.Password;

            if (!IsUserAuthenticationNeeded())
            {
                Log.Info("MQTTBrokerLogic", $"MQTT client '{context.ClientId}' successfully connected to the MQTT broker (anonymous login)");
                context.ReasonCode = MqttConnectReasonCode.Success;
                return true;
            }

            if (string.IsNullOrEmpty(username))
            {
                Log.Info("MQTTBrokerLogic", $"MQTT client '{context.ClientId}' connection the MQTT broker failed. A valid username must be specified");
                context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return false;
            }

            if (!IsUserAuthorized(username, password))
            {
                Log.Info("MQTTBrokerLogic", $"MQTT client '{context.ClientId}' connection the MQTT broker failed (user '{username}'). Wrong username or password");
                context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return false;
            }

            Log.Info("MQTTBrokerLogic", $"MQTT client '{context.ClientId}' successfully connected to the MQTT broker (user '{username}')");
            context.ReasonCode = MqttConnectReasonCode.Success;

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(MethodBase.GetCurrentMethod().Name, $"An exception occurred while stopping the MQTT broker: {ex.Message}");
            return false;
        }

    }

    /// <summary>
    /// Logic for evaluating whether or not a broker can accept new connections/clients
    /// </summary>
    /// <returns></returns>
    private bool BrokerIsFull()
    {
        using (var temporarySessionHandler = LogicObject.Context.Sessions.ImpersonateSessionTemporary(sessionHandler))
        {
            SetDefaultNamespaceIndexIfNeeded();

            int maxNumberOfConnectedClients = mqttAsServerMaxNumberOfConnectionsVariable.Value;
            if (connectedClientIdList.Count >= maxNumberOfConnectedClients)
            {
                var message = $"{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")} Too much clients connected. The maximum number of concurrent connected MQTT clients is set to {maxNumberOfConnectedClients}";
                Log.Info("MQTTBrokerLogic", message);
                SendMessageToTopic("Error_Topic", message);
                return true;
            }

            return false;
        }
    }
    private bool IsUserAuthenticationNeeded()
    {
        using (var temporarySessionHandler = LogicObject.Context.Sessions.ImpersonateSessionTemporary(sessionHandler))
        {
            SetDefaultNamespaceIndexIfNeeded();

            return mqttAsServerUserAuthenticationVariable.Value;
        }
    }
    private bool IsUserAuthorized(string usernameToValidate, string passwordToValidate)
    {
        Log.Info("User Security Password Changed!");
        return true;
        //using (var temporarySessionHandler = LogicObject.Context.Sessions.ImpersonateSessionTemporary(sessionHandler))
        //{
        //    SetDefaultNamespaceIndexIfNeeded();

        //    try
        //    {
        //        string[] authorizedUsers = mqttAsServerAuthorizedUsersVariable.Value;
        //        if (authorizedUsers == null)
        //            return false;

        //        if (!authorizedUsers.Contains(usernameToValidate))
        //            return false;

        //        var userToValidate = Project.Current.Find<User>(usernameToValidate);
        //        if (userToValidate == null)
        //        {
        //            Log.Error(MethodBase.GetCurrentMethod().Name, $"User '{usernameToValidate}' can not be found in the 'Users' folder");
        //            return false;
        //        }

        //        return userToValidate.Password == passwordToValidate;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(MethodBase.GetCurrentMethod().Name, $"An exception occurred while checking user '{usernameToValidate}' credentials: {ex.Message}");
        //        return false;
        //    }
        //}
    }

    /// <summary>
    /// Binds the runtime script to the current session of the running FactoryTalk Optix application
    /// </summary>
    private void SetDefaultNamespaceIndexIfNeeded()
    {
        if (LogicObject.Context.DefaultNamespaceIndex == NodeId.InvalidNamespaceIndex)
            LogicObject.Context.DefaultNamespaceIndex = defaultNamespaceIndex;
    }


    /// <summary>
    /// Configuration of the broker parameters: IP address, port, etc..
    /// </summary>
    private MQTTServerParameters ReadMQTTServerParameters()
    {
        try
        {
            var mqttServerParameters = new MQTTServerParameters();

            string ipAddressValue = mqttAsServerIpAddressVariable.Value;
            Log.Info("IP ADDRESS: " + ipAddressValue);
            if (!IPAddress.TryParse(ipAddressValue, out IPAddress ipAddress))
            {
                Log.Error(MethodBase.GetCurrentMethod().Name, $"IP address '{ipAddressValue}' is not in a legal form");
                return new MQTTServerParameters();
            }
            mqttServerParameters.ipAddress = ipAddress;

            Log.Info("IP ADDRESS: " + mqttServerParameters.ipAddress);

            mqttServerParameters.port = mqttAsServerPortVariable.Value;

            if (mqttAsServerUseSSLVariable.Value)
            {
                string pathCACert = ResourceUriValueToAbsoluteFilePath(mqttAsServerCertificateVariable.Value);
                mqttServerParameters.sslParameters.certificate = new X509Certificate2(pathCACert, mqttAsServerCertificatePasswordVariable.Value, X509KeyStorageFlags.Exportable);
                mqttServerParameters.sslParameters.certificatePassword = mqttAsServerPortVariable.Value;
            }

            return mqttServerParameters;
        }
        catch (Exception ex)
        {
            Log.Error(MethodBase.GetCurrentMethod().Name, $"An exception occurred while reading the MQTT broker configuration: {ex.Message}");
            return new MQTTServerParameters();
        }
    }

    #endregion

    #region LiveTags
    /// <summary>
    /// Gets all the variables present in the "Model" node and for each one it listens to the value changes
    /// Every change of value is notified to the broker on the topic dedicated to the variable under analysis
    /// </summary>
    [ExportMethod]
    public void StartListeningToVariables()
    {
        variableSynchronizer = new RemoteVariableSynchronizer();
        try
        {
            liveVariables = GetNodesIntoFolder<UAVariable>(InformationModel.Get(publisherLiveTagsFolder.Value));
            if ((double)publisherLiveTagsPeriod.Value == 0)
                foreach (UAVariable v in liveVariables)
                {
                    // listens for changes in the variable
                    // https://www.rockwellautomation.com/docs/en/factorytalk-optix/1-00/contents-ditamap/developing-solutions/developing-projects-with-csharp/csharp-apis-reference/keep-field-vars-synchronized.html
                    variableSynchronizer.Add(v);
                    // associates a method that is triggered when the value of the variable changes
                    // https://www.rockwellautomation.com/docs/en/factorytalk-optix/1-00/contents-ditamap/developing-solutions/developing-projects-with-csharp/methods-and-events-in-csharp/variables-and-objects-generic-events.html
                    v.VariableChange += OnVariableChange;
                }
            else
            {
                liveDataTask = new PeriodicTask(LiveDataPublisher, publisherLiveTagsPeriod.Value, LogicObject);
                liveDataTask.Start();
            }
        }
        catch (Exception e)
        {
            Log.Error($"{MethodBase.GetCurrentMethod().Name} Exception: {e.Message}");
        }
    }

    private void LiveDataPublisher()
    {
        try
        {
            string message = CreateLiveVariablePacketFormatJSON(liveVariables);
            sentPackages.Value++;
            mqttClient.PublishAsync((string)publisherLiveTagsTopic.Value, message, mqttQoS[(int)publisherLiveTagsQoS.Value], (bool)publisherLiveTagsRetain.Value);
        }
        catch (Exception e)
        {
            Log.Info("MqttTools-LiveTagsPublisher", e.Message);
        }

    }

    public string CreateLiveVariablePacketFormatJSON(ICollection<IUANode> variables)
    {
        var sb = new StringBuilder();
        var sw = new StringWriter(sb);
        using (var writer = new JsonTextWriter(sw))
        {
            writer.Formatting = Formatting.None;

            writer.WriteStartObject();

            writer.WritePropertyName("Timestamp");
            writer.WriteValue(DateTime.Now);

            writer.WritePropertyName("Records");
            writer.WriteStartArray();

            foreach (var item in variables)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("TagName");
                writer.WriteValue(Log.Node(item).Replace(Log.Node(InformationModel.Get(publisherLiveTagsFolder.Value)) + "/", ""));
                writer.WritePropertyName("Value");
                var value = ((IUAVariable)item).Value.Value;
                writer.WriteValue(value);
                writer.WriteEndObject();
            }

            writer.WriteEnd();
            writer.WriteEndObject();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Given a change of value of a variable, a message is sent to the topic dedicated to it
    /// </summary>
    private void OnVariableChange(object sender, VariableChangeEventArgs e)
    {
        // All Tags Sending Values
        LiveDataPublisher();
        // Only Changed Tag Sending Value
        //ICollection<IUANode> variables = new List<IUANode>();
        //variables.Add((IUANode)sender);
        //var message = CreateLiveVariablePacketFormatJSON(variables);
    }

    #endregion

    #region Custom Payload
    [ExportMethod]
    public void StartCustomPayload()
    {
        try
        {
            if ((double)publisherCustomPayloadPeriod.Value == 0)
                publisherCustomPayloadMessage.VariableChange += PublisherCustomPayloadMessage_VariableChange;
            else
            {
                customPayloadTask = new PeriodicTask(CustomPayloadPublisher, publisherCustomPayloadPeriod.Value, LogicObject);
                customPayloadTask.Start();
            }
        }
        catch (Exception e)
        {
            Log.Error($"{MethodBase.GetCurrentMethod().Name} Exception: {e.Message}");
        }
    }

    private void PublisherCustomPayloadMessage_VariableChange(object sender, VariableChangeEventArgs e)
    {
        CustomPayloadPublisher();
    }

    private void CustomPayloadPublisher()
    {
        try
        {
            string message = (string)publisherCustomPayloadMessage.Value;
            sentPackages.Value++;
            mqttClient.PublishAsync((string)publisherCustomPayloadTopic.Value, message, mqttQoS[(int)publisherCustomPayloadQoS.Value], (bool)publisherCustomPayloadRetain.Value);
        }
        catch (Exception e)
        {
            Log.Info("MqttTools-LiveTagsPublisher", e.Message);
        }
    }

    #endregion


    /// <summary>
    /// Given a path to a project folder,
    /// obtains one list of objects of type T contained in the same folder (and relative sottocartelle, recursively)
    /// </summary>
    private static ICollection<IUANode> GetNodesIntoFolder<T>(IUANode iuaNode)
    {
        var objectsInFolder = new List<IUANode>();
        foreach (var o in iuaNode.Children)
        {
            switch (o)
            {
                case T _:
                    objectsInFolder.Add(o);
                    break;
                case Folder _:
                case UAObject _:
                    objectsInFolder.AddRange(GetNodesIntoFolder<T>(o));
                    break;
                default:
                    break;
            }
        }
        return objectsInFolder;
    }

    /// <summary>
    /// Get the number of active alarms
    /// </summary>
    private void GetActiveAlarmsNumber()
    {
        try
        {
            IContext context = LogicObject.Context;
            var retainedAlarms = context.GetNode(FTOptix.Alarm.Objects.RetainedAlarms);
            var localizedAlarmsVariable = retainedAlarms.Children.Get<IUAVariable>("LocalizedAlarms");
            var localizedAlarmsNodeId = (NodeId)localizedAlarmsVariable.Value;
            IUANode localizedAlarmsContainer = null;
            if (localizedAlarmsNodeId != null && !localizedAlarmsNodeId.IsEmpty)
            {
                localizedAlarmsContainer = context.GetNode(localizedAlarmsNodeId);
                activeAlarmsCounter = localizedAlarmsContainer?.Children.Count ?? 0;
                return;
            }
            activeAlarmsCounter = 0;
        }
        catch (Exception e)
        {
            Log.Error($"{MethodBase.GetCurrentMethod().Name} Exception: {e.Message}");
        }
    }
    private async void SendMessageToTopic(string topic, string msg)
    {
        try
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(msg)
                .WithExactlyOnceQoS()
                .WithRetainFlag(true)
                .Build();
            await mqttServer.PublishAsync(message);
        }
        catch (Exception e)
        {
            Log.Error($"{MethodBase.GetCurrentMethod().Name} Exception: {e.Message}");
        }
    }

    /// <summary>
    /// If needed build the alarm message package
    /// </summary>
    private string ConstructAlarmMessage(IEventArguments eventArguments, IReadOnlyList<object> args, string alarmDescription)
    {
        var time = (DateTime)eventArguments.GetFieldValue(args, "Time");
        var localTime = eventArguments.GetFieldValue(args, "LocalTime");
        var conditionName = eventArguments.GetFieldValue(args, "ConditionName");
        var ackedState = eventArguments.GetFieldValue(args, "AckedState/Id");
        var confirmedState = eventArguments.GetFieldValue(args, "ConfirmedState/Id");
        var activeState = eventArguments.GetFieldValue(args, "ActiveState/Id");
        var enabledState = eventArguments.GetFieldValue(args, "EnabledState/Id");
        var sourceName = eventArguments.GetFieldValue(args, "SourceName");
        var severity = eventArguments.GetFieldValue(args, "Severity");
        var value = (string)InformationModel.GetVariable((NodeId)eventArguments.GetFieldValue(args, "InputNode")).Value;
        if (value == "True")
            value = "1";
        if (value == "False")
            value = "0";

        var sb = new StringBuilder();
        var sw = new StringWriter(sb);
        using (var writer = new JsonTextWriter(sw))
        {
            writer.Formatting = Formatting.None;

            writer.WriteStartObject();
            writer.WritePropertyName("ConditionName");
            writer.WriteValue(conditionName);
            writer.WritePropertyName("Time");
            writer.WriteValue(time);
            writer.WritePropertyName("ActiveState_Id");
            writer.WriteValue(activeState);
            writer.WritePropertyName("AckedState_Id");
            writer.WriteValue(ackedState);
            writer.WritePropertyName("ConfirmedState_Id");
            writer.WriteValue(confirmedState);
            writer.WritePropertyName("EnabledState_Id");
            writer.WriteValue(enabledState);
            writer.WritePropertyName("SourceName");
            writer.WriteValue(sourceName);
            writer.WritePropertyName("Severity");
            writer.WriteValue(severity);
            writer.WritePropertyName("LocalTime");
            writer.WriteValue(((Struct)localTime).Values[0]);
            writer.WritePropertyName("Value");
            writer.WriteValue(value);

            //foreach (var evt in currentEventList)
            //{
            var message = ((UAManagedCore.LocalizedText)eventArguments.GetFieldValue(args, "Message")).Text;
            writer.WritePropertyName("Message");
            writer.WriteValue(message);
            //}

            writer.WriteEnd();
        }

        return sb.ToString();
    }

    private string ResourceUriValueToAbsoluteFilePath(UAValue value)
    {
        try
        {
            var resourceUri = new ResourceUri(value);
            return resourceUri.Uri;
        }
        catch (Exception ex)
        {
            Log.Error(MethodBase.GetCurrentMethod().Name, $"Path is not in a legal form: {ex.Message}");
            return string.Empty;
        }
    }

    private struct SSLParameters
    {
        public X509Certificate2 certificate;
        public string certificatePassword;

        public bool IsEmpty()
        {
            return certificate == null;
        }
    }

    #region DB Tables Publisher

    private void StartPublisherTables()
    {
        ConfigureSupportStoreAndForwardDB();
        myLRT = new LongRunningTask(FetchData, LogicObject);
        myLRT.Start();
    }

    private void ConfigureSupportStoreAndForwardDB()
    {
        try
        {
            SQLiteStore sqlStore = InformationModel.MakeObject<SQLiteStore>(supportStoreAndForwardDBName);
            sqlStore.Filename = supportStoreAndForwardDBFilename;
            LogicObject.Add(sqlStore);

            object[,] resultSet;
            string[] header;

            ((Store)sqlStore).AddTable("Tables");
            ((Store)sqlStore).Tables.Get("Tables").AddColumn("TableName", UAManagedCore.OpcUa.DataTypes.String);
            ((Store)sqlStore).Tables.Get("Tables").AddColumn("Id", UAManagedCore.OpcUa.DataTypes.Int16);
            ((Store)sqlStore).Tables.Get("Tables").AddColumn("RowId", UAManagedCore.OpcUa.DataTypes.Int64);

            foreach (var item in publisherStoreTablesTableNames.Children)
            {
                string tableName = (string)((IUAVariable)item).Value;
                sqlStore.Query($"SELECT * FROM Tables WHERE TableName = '{tableName}'", out header, out resultSet);
                if (resultSet.Length == 0)
                {
                    var values = new object[1, 3];

                    values[0, 0] = tableName;
                    values[0, 1] = 1;
                    if (publisherStoreTablesAllRows.Value)
                        values[0, 2] = -1;
                    else
                        values[0, 2] = GetTableMaxRowId(tableName);

                    string[] columns = new string[] { "TableName", "Id", "RowId" };
                    ((Store)sqlStore).Tables.Get("Tables").Insert(columns, values);
                }
            }

        }
        catch (Exception)
        {

            throw;
        }


        //try
        //{
        //    SQLiteStore sqlStore = InformationModel.MakeObject<SQLiteStore>(supportStoreAndForwardDBName);
        //    sqlStore.Filename = supportStoreAndForwardDBFilename;
        //    LogicObject.Add(sqlStore);

        //    object[,] resultSet;
        //    string[] header;

        //    ((Store)sqlStore).AddTable(DlTableName);
        //    ((Store)sqlStore).Tables.Get(DlTableName).AddColumn("Id", UAManagedCore.OpcUa.DataTypes.Int16);
        //    ((Store)sqlStore).Tables.Get(DlTableName).AddColumn("RowId", UAManagedCore.OpcUa.DataTypes.Int64);

        //    sqlStore.Query($"SELECT * FROM {DlTableName}", out header, out resultSet);
        //    if (resultSet.Length == 0)
        //    {
        //        var values = new object[1, 2];

        //        values[0, 0] = 1;
        //        values[0, 1] = -1;

        //        string[] columns = new string[] { "Id", "RowId" };
        //        ((Store)sqlStore).Tables.Get(DlTableName).Insert(columns, values);
        //    }

        //    ((Store)sqlStore).AddTable(AlarmsTableName);
        //    ((Store)sqlStore).Tables.Get(AlarmsTableName).AddColumn("Id", UAManagedCore.OpcUa.DataTypes.Int16);
        //    ((Store)sqlStore).Tables.Get(AlarmsTableName).AddColumn("RowId", UAManagedCore.OpcUa.DataTypes.Int64);

        //    sqlStore.Query($"SELECT * FROM {AlarmsTableName}", out header, out resultSet);
        //    if (resultSet.Length == 0)
        //    {
        //        var values = new object[1, 2];

        //        values[0, 0] = 1;
        //        values[0, 1] = -1;

        //        string[] columns = new string[] { "Id", "RowId" };
        //        ((Store)sqlStore).Tables.Get(AlarmsTableName).Insert(columns, values);
        //    }
        //}
        //catch (Exception e)
        //{
        //    throw new Exception("Unable to create support store " + e.Message);
        //}
    }

    private void UpdateLastRowPushed(string table, Int64 rowId)
    {
        Store store = LogicObject.Get<Store>(supportStoreAndForwardDBName);

        try
        {
            string query = $"UPDATE Tables SET \"RowId\" = {rowId} WHERE \"Id\"= 1 AND TableName ='{table}'";
            store.Query(query, out _, out _);
        }
        catch (Exception e)
        {
            Log.Info("Error: " + e.Message);
        }
    }

    private void UpdateLastRowPushed(bool dl, Int64 rowId)
    {
        //DataLogger dataLogger = InformationModel.Get<DataLogger>(LogicObject.GetVariable("Publisher/DataLoggerData/DataLogger").Value);
        Store store = LogicObject.Get<Store>(supportStoreAndForwardDBName);
        string tableName = dl ? DlTableName : AlarmsTableName;

        try
        {
            string query = $"UPDATE \"{tableName}\" SET \"RowId\" = {rowId} WHERE \"Id\"= 1";
            store.Query(query, out _, out _);
        }
        catch (Exception e)
        {
            Log.Info("Error: " + e.Message);
        }
    }

    private Int64 GetTableMaxRowId(string table)
    {
        object[,] resultSet;
        Store store = InformationModel.Get<Store>(publisherStoreTablesStore.Value);
        try
        {
            string query = $"SELECT MAX(\"RowId\") FROM \"{table}\"";

            if (store.Status == StoreStatus.Online)
            {
                store.Query(query, out _, out resultSet);
                if (resultSet.Length > 0)
                    if (resultSet[0, 0] != null)
                        return Int64.Parse(resultSet[0, 0].ToString());

            }
            return -1;
        }
        catch (Exception e)
        {
            Log.Error("PushAgent", "Failed to query maxid from DataLogger temporary table: " + e.Message);
            throw;
        }
    }

    private Int64 QueryTableLastRowId(string table)
    {
        object[,] resultSet;
        Store store = LogicObject.Get<Store>(supportStoreAndForwardDBName);

        try
        {
            string query = $"SELECT RowId FROM Tables WHERE Id = 1 AND TableName = '{table}'";

            store.Query(query, out _, out resultSet);

            if (resultSet.Length > 0)
                if (resultSet[0, 0] != null)
                    return Int64.Parse(resultSet[0, 0].ToString());

            return -1;
        }
        catch (Exception e)
        {
            throw new Exception("Failed to query internal DataLoggerStatusStore: " + e.Message);
        }
    }


    //[ExportMethod]
    //public Int64 QueryLastRowId(bool dl)
    //{
    //    object[,] resultSet;
    //    Store store = LogicObject.Get<Store>(supportStoreAndForwardDBName);
    //    string tableName = dl? DlTableName : AlarmsTableName;

    //    try
    //    {
    //        string query = $"SELECT \"RowId\" FROM \"{tableName}\" WHERE Id = 1";

    //        store.Query(query, out _, out resultSet);

    //        if (resultSet.Length > 0)
    //            if (resultSet[0, 0] != null)
    //            {
    //                Log.Info("Last RowId: " + resultSet[0, 0].ToString() + " query: " + query);
    //                return Int64.Parse(resultSet[0, 0].ToString());
    //            }

    //        return -1;
    //    }
    //    catch (Exception e)
    //    {
    //        throw new Exception("Failed to query internal DataLoggerStatusStore: " + e.Message);
    //    }
    //}

    public class LoggerRecord
    {
        public LoggerRecord(List<FieldRecord> fields)
        {
            this.Fields = fields;
            //this.localtimestamp = localtimestamp;
            //this.id = id;
        }

        //public override bool Equals(object obj)
        //{
        //    DataLoggerRecord other = obj as DataLoggerRecord;

        //    if (other == null)
        //        return false;

        //    if (timestamp != other.timestamp)
        //        return false;

        //    if (variables.Count != other.variables.Count)
        //        return false;

        //    for (int i = 0; i < variables.Count; ++i)
        //    {
        //        if (!variables[i].Equals(other.variables[i]))
        //            return false;
        //    }

        //    return true;
        //}

        public readonly List<FieldRecord> Fields;
        //public readonly DateTime localtimestamp;
        //public readonly Int32 id;
    }

    public class FieldRecord
    {
        public FieldRecord(string name, string type, UAValue value)
        {
            this.name = name;
            this.value = value;
            this.type = type;
        }

        //public FieldRecord(DateTime? timestamp,
        //                      string variableId,
        //                      UAValue value,
        //                      string serializedValue,
        //                      int? variableOpCode) : base(timestamp)
        //{
        //    this.variableId = variableId;
        //    this.value = value;
        //    this.serializedValue = serializedValue;
        //    this.variableOpCode = variableOpCode;
        //}

        //public override bool Equals(object obj)
        //{
        //    var other = obj as FieldRecord;
        //    return timestamp == other.timestamp &&
        //           variableId == other.variableId &&
        //           value == other.value &&
        //           serializedValue == other.serializedValue &&
        //           variableOpCode == other.variableOpCode;
        //}

        public readonly string name;
        public readonly UAValue value;
        public readonly string type;
    }

    public static List<LoggerRecord> GetTableRecordsFromQueryResult(object[,] resultSet, string[] header, List<string> columnList, List<string> columnType)
    {
        var records = new List<LoggerRecord>();

        var rowCount = resultSet != null ? resultSet.GetLength(0) : 0;
        var columnCount = header != null ? header.Length : 0;
        for (int i = 0; i < rowCount; ++i)
        {
            var rowFields = new List<FieldRecord>();
            for (int j = 0; j < columnCount; ++j)
            {
                var fieldRecord = new FieldRecord(columnList[j], columnType[j], GetUAValue(resultSet[i, j], columnType[j]));//resultSet[i, j].ToString());
                rowFields.Add(fieldRecord);
            }
            var record = new LoggerRecord(rowFields);
            records.Add(record);
        }

        return records;
    }

    private static UAValue GetUAValue(object value, string type)
    {
        if (value == null)
            return null;
        try
        {
            switch (type)
            {
                case ("Boolean"):
                    return new UAValue(Int32.Parse(GetBoolean(value)));
                case ("Byte"):
                    return new UAValue(Byte.Parse(value.ToString()));
                case ("SByte"):
                    return new UAValue(SByte.Parse(value.ToString()));
                case ("Int16"):
                    return new UAValue(Int16.Parse(value.ToString()));
                case ("Int32"):
                    return new UAValue(Int32.Parse(value.ToString()));
                case ("Int64"):
                    return new UAValue(Int64.Parse(value.ToString()));
                case ("UInt16"):
                    return new UAValue(UInt16.Parse(value.ToString()));
                case ("UInt32"):
                    return new UAValue(UInt32.Parse(value.ToString()));
                case ("UInt64"):
                    return new UAValue(UInt64.Parse(value.ToString()));
                case ("Float"):
                    return new UAValue((float)((double)value));
                case ("Double"):
                    return new UAValue((double)value);
                case ("String"):
                    return new UAValue(value.ToString());
                case ("ByteString"):
                    return new UAValue((ByteString)value);
                case ("UtcTime"):
                case ("DateTime"):
                    return new UAValue(GetTimestamp(value));
                default:
                    break;
            }
            //NodeId valueType = variableToLog.ActualDataType;
            //if (valueType == OpcUa.DataTypes.Boolean)
            //    return new UAValue(Int32.Parse(GetBoolean(value)));
            //else if (valueType == OpcUa.DataTypes.Integer)
            //    return new UAValue(Int64.Parse(value.ToString()));
            //else if (valueType == OpcUa.DataTypes.UInteger)
            //    return new UAValue(UInt64.Parse(value.ToString()));
            //else if (valueType == OpcUa.DataTypes.Byte)
            //    return new UAValue(Byte.Parse(value.ToString()));
            //else if (valueType == OpcUa.DataTypes.SByte)
            //    return new UAValue(SByte.Parse(value.ToString()));
            //else if (valueType == OpcUa.DataTypes.Int16)
            //    return new UAValue(Int16.Parse(value.ToString()));
            //else if (valueType == OpcUa.DataTypes.UInt16)
            //    return new UAValue(UInt16.Parse(value.ToString()));
            //else if (valueType == OpcUa.DataTypes.Int32)
            //    return new UAValue(Int32.Parse(value.ToString()));
            //else if (valueType == OpcUa.DataTypes.UInt32)
            //    return new UAValue(UInt32.Parse(value.ToString()));
            //else if (valueType == OpcUa.DataTypes.Int64)
            //    return new UAValue(Int64.Parse(value.ToString()));
            //else if (valueType == OpcUa.DataTypes.UInt64)
            //    return new UAValue(UInt64.Parse(value.ToString()));
            //else if (valueType == OpcUa.DataTypes.Float)
            //    return new UAValue((float)((double)value));
            //else if (valueType == OpcUa.DataTypes.Double)
            //    return new UAValue((double)value);
            //else if (valueType == OpcUa.DataTypes.DateTime)
            //    return new UAValue(GetTimestamp(value));
            //else if (valueType == OpcUa.DataTypes.String)
            //    return new UAValue(value.ToString());
            //else if (valueType == OpcUa.DataTypes.ByteString)
            //    return new UAValue((ByteString)value);
            //else if (valueType == OpcUa.DataTypes.NodeId)
            //    return new UAValue((NodeId)value);
        }
        catch (Exception e)
        {
            Log.Warning("PushAgent", "Parse Exception: " + e.Message);
            throw;
        }

        return null;
    }

    private static string GetBoolean(object value)
    {
        var valueString = value.ToString();
        if (valueString == "0" || valueString == "1")
            return valueString;

        if (valueString.ToLower() == "false")
            return "0";
        else
            return "1";
    }

    private static DateTime GetTimestamp(object value)
    {
        if (Type.GetTypeCode(value.GetType()) == TypeCode.DateTime)
            return ((DateTime)value);
        else
            return DateTime.SpecifyKind(DateTime.Parse(value.ToString()), DateTimeKind.Utc);
    }

    private List<LoggerRecord> QueryOlderEntries(string table, int numberOfEntries, Int64 lastPulledRecordId)
    {
        Store store = InformationModel.Get<Store>(publisherStoreTablesStore.Value);

        List<LoggerRecord> records = null;
        object[,] resultSet;
        string[] header;

        Table loggerTable = store.Tables.Get<Table>(table);
        List<string> fieldsName = new List<string>();
        List<string> fieldsType = new List<string>();
        List<FieldRecord> fields = new List<FieldRecord>();
        string columns = "RowId";
        fieldsName.Add("RowId");
        fieldsType.Add("UInt64");
        foreach (var col in loggerTable.Columns)
        {
            if (columns != string.Empty)
                columns += ", ";
            columns += "\"" + col.BrowseName + "\"";
            fieldsName.Add(col.BrowseName);
            fieldsType.Add(InformationModel.Get(((IUAVariable)col).DataType).BrowseName);
        }
        //foreach (var variable in dataLogger.VariablesToLog)
        //{
        //    variablesToLogList.Add(variable);
        //}

        try
        {
            string query = $"SELECT {columns} " +
                           $"FROM \"{table}\" " +
                           $"WHERE RowId > {lastPulledRecordId} " +
                           $"ORDER BY \"RowId\" ASC " +
                           $"LIMIT {numberOfEntries}";
            store.Query(query, out header, out resultSet);
            records = GetTableRecordsFromQueryResult(resultSet, header, fieldsName, fieldsType);
        }
        catch (Exception e)
        {
            throw new Exception("Failed to query the internal PushAgent temporary table: " + e.Message);
        }

        return records;
    }


    //private List<LoggerRecord> QueryOlderEntries(bool dl, int numberOfEntries, Int64 lastPulledRecordId)
    //{
    //    IUANode logger = null;
    //    if (dl)
    //        logger = InformationModel.Get(publisherDataLoggerDataDataLogger.Value);
    //    else
    //        logger = InformationModel.Get(publisherAlarmsEventsHistoryAlarmsEventsLogger.Value);

    //    Store store = InformationModel.Get<Store>(logger.GetVariable("Store").Value);
    //    string tableName = "";
    //    if (logger.GetVariable("TableName") is null)
    //        tableName = logger.BrowseName;
    //    else
    //        tableName = (string)logger.GetVariable("TableName").Value;

    //    List<LoggerRecord> records = null;
    //    object[,] resultSet;
    //    string[] header;

    //    Table loggerTable = store.Tables.Get<Table>(tableName);
    //    string columns = "";
    //    List<string> fieldsName = new List<string>();
    //    List<FieldRecord> fields = new List<FieldRecord>();
    //    foreach (var col in loggerTable.Columns)
    //    {
    //        if (columns != string.Empty)
    //            columns += ", ";
    //        columns += "\"" + col.BrowseName + "\"";
    //        fieldsName.Add(col.BrowseName);
    //    }
    //    foreach (var variable in dataLogger.VariablesToLog)
    //    {
    //        variablesToLogList.Add(variable);
    //    }

    //    try
    //    {
    //        string query = $"SELECT {columns} " +
    //                       $"FROM \"{tableName}\" " +
    //                       $"WHERE RowId > {lastPulledRecordId} " +
    //                       $"ORDER BY \"RowId\" ASC " +
    //                       $"LIMIT {numberOfEntries}";
    //        store.Query(query, out header, out resultSet);
    //        records = GetDataLoggerRecordsFromQueryResult(resultSet, header, fieldsName);
    //    }
    //    catch (Exception e)
    //    {
    //        throw new Exception("Failed to query the internal PushAgent temporary table: " + e.Message);
    //    }

    //    return records;
    //}

    public class Packet
    {
        public Packet(DateTime timestamp, string clientId, string table)
        {
            this.timestamp = timestamp.ToUniversalTime();
            //this.timestamp = timestamp;
            //this.localtimestamp = localtimestamp;
            //this.id = id;
            this.clientId = clientId;
            this.tableName = table;
        }

        public readonly DateTime timestamp;
        //public readonly DateTime localtimestamp;
        //public readonly Int32 id;
        public readonly string clientId;
        public readonly string tableName;
    }

    public class LoggerRowPacket : Packet
    {
        public LoggerRowPacket(DateTime timestamp, string clientId, string tableName,
                                   List<LoggerRecord> records) : base(timestamp, clientId, tableName)
        {
            this.records = records;
        }

        public readonly List<LoggerRecord> records;
    }

    public class JSONBuilder
    {
        public JSONBuilder()
        {
            //this.insertOpCode = insertOpCode;
            //this.insertVariableTimestamp = insertVariableTimestamp;
        }

        public string CreateLoggerRowPacketFormatJSON(LoggerRowPacket packet)
        {
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            using (var writer = new JsonTextWriter(sw))
            {

                writer.Formatting = Formatting.None;

                writer.WriteStartObject();
                writer.WritePropertyName("Timestamp");
                writer.WriteValue(packet.timestamp);
                writer.WritePropertyName("ClientId");
                writer.WriteValue(packet.clientId);
                writer.WritePropertyName("TableName");
                writer.WriteValue((string)publisherStoreTablesTablesPrefix.Value + "_" + packet.tableName);
                writer.WritePropertyName("Rows");
                writer.WriteStartArray();
                foreach (var record in packet.records)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("Variables");
                    writer.WriteStartArray();
                    foreach (var item in record.Fields)
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("VariableName");
                        writer.WriteValue(item.name);
                        writer.WritePropertyName("VariableType");
                        writer.WriteValue(item.type);
                        writer.WritePropertyName("Value");
                        writer.WriteValue(item.value?.Value);
                        writer.WriteEndObject();
                    }
                    writer.WriteEnd();
                    writer.WriteEndObject();
                }
                writer.WriteEnd();
                writer.WriteEndObject();
            }
            return sb.ToString();
        }

        //public string CreateVariablePacketFormatJSON(VariablePacket packet)
        //{
        //    var sb = new StringBuilder();
        //    var sw = new StringWriter(sb);
        //    using (var writer = new JsonTextWriter(sw))
        //    {
        //        writer.Formatting = Formatting.None;

        //        writer.WriteStartObject();
        //        writer.WritePropertyName("Timestamp");
        //        writer.WriteValue(packet.timestamp);
        //        writer.WritePropertyName("ClientId");
        //        writer.WriteValue(packet.clientId);
        //        writer.WritePropertyName("Records");
        //        writer.WriteStartArray();
        //        foreach (var record in packet.records)
        //        {
        //            writer.WriteStartObject();

        //            writer.WritePropertyName("VariableName");
        //            writer.WriteValue(record.variableId);
        //            writer.WritePropertyName("SerializedValue");
        //            writer.WriteValue(record.serializedValue);
        //            writer.WritePropertyName("VariableTimestamp");
        //            writer.WriteValue(record.timestamp);

        //            if (insertOpCode)
        //            {
        //                writer.WritePropertyName("VariableOpCode");
        //                writer.WriteValue(record.variableOpCode);
        //            }

        //            writer.WriteEndObject();
        //        }
        //        writer.WriteEnd();
        //        writer.WriteEndObject();
        //    }

        //    return sb.ToString();
        //}

        //private readonly bool insertOpCode;
        //private readonly bool insertVariableTimestamp;
    }

    private string GenerateJSON(LoggerRowPacket packet)
    {
        return jsonCreator.CreateLoggerRowPacketFormatJSON(packet);
    }

    //private int nextRestartTimeout;
    //private DelayedTask restartDataFetchTask;
    //private DelayedTask dataFetchTask;
    //private void StartFetchTimer()
    //{
    //    Int64 lastPulledRecordId = QueryTableLastRowId((string)table.Value);
    //    Int64 maxRowId = GetTableMaxRowId((string)table.Value);
    //    try
    //    {
    //        // Set the correct timeout by checking number of records to be sent
    //        if (maxRowId - lastPulledRecordId >= (Int32)publisherStoreTablesMaximumItemsPerPacket.Value)
    //            nextRestartTimeout = (int)publisherStoreTablesMinimumPublishTime.Value;
    //        else
    //            nextRestartTimeout = (int)publisherStoreTablesMaximumPublishTime.Value;

    //        restartDataFetchTask = new DelayedTask(OnRestartDataFetchTimer, 0, LogicObject);
    //        restartDataFetchTask.Start();
    //        restartDataFetchTaskRunning = true;
    //    }
    //    catch (Exception e)
    //    {
    //        OnFetchError(e.Message);
    //    }
    //}

    //private void OnRestartDataFetchTimer()
    //{
    //    restartDataFetchTaskRunning = false;

    //    dataFetchTask = new DelayedTask(OnFetchRequired, nextRestartTimeout, LogicObject);
    //    dataFetchTask.Start();
    //    dataFetchTaskRunning = true;
    //}

    //private void OnFetchRequired()
    //{
    //    dataFetchTaskRunning = false;

    //    if (pushAgentStore.RecordsCount() > 0)
    //        FetchData();
    //    else
    //        StartFetchTimer();
    //}


    [ExportMethod]
    public void FetchData()
    {
        pendingPackage = 0;
        while (true)
        {
            if (closing)
                return;
            if (mqttAsClientConnected.Value && pendingPackage == 0)
            {
                jsonCreator = new JSONBuilder();
                bool minPublishTime = false;
                foreach (IUAVariable table in publisherStoreTablesTableNames.Children)
                {
                    Int64 lastPulledRecordId = QueryTableLastRowId((string)table.Value);
                    Int64 maxRowId = GetTableMaxRowId((string)table.Value);

                    if (maxRowId > lastPulledRecordId)
                        pendingPackage++;

                    if (maxRowId - lastPulledRecordId >= (int)publisherStoreTablesMaximumItemsPerPacket.Value)
                        minPublishTime = true;
                }
                if (pendingPackage > 0)
                {
                    int nextRestartTimeout = 0;

                    if (minPublishTime)
                        nextRestartTimeout = (int)publisherStoreTablesMinimumPublishTime.Value;
                    else
                        nextRestartTimeout = (int)publisherStoreTablesMaximumPublishTime.Value;

                    delayedTask = new DelayedTask(OnRestartDataFetchTimer, nextRestartTimeout, LogicObject);
                    Log.Info("Started Delayd Task...");
                    delayedTask.Start();
                }
            }
        }



        //Int64 lastPulledRecordId = QueryLastRowId(dl);
        //Int64 maxRowId = GetMaxRowId(dl);

        ////se ci sono record da inviare allora predispongo il pacchetto per l'invio
        //if (maxRowId > lastPulledRecordId)
        //{
        //    List<LoggerRecord> loggerRecords = null;
        //    try
        //    {
        //        loggerRecords = QueryOlderEntries(dl, (int)publisherDataLoggerMaximumItemsPerPacket.Value, lastPulledRecordId);
        //        if (loggerRecords.Count > 0)
        //        {
        //            Log.Info("Check 1");
        //            pendingSendPacket = new LoggerRowPacket(DateTime.Now,
        //                                                        (string)mqttAsClientClientIdVariable.Value,
        //                                                        loggerRecords);

        //            string json = GenerateJSON(pendingSendPacket);
        //            Log.Info(json);

        //            mqttClient.PublishAsync((string)publisherDataLoggerDataLoggerDataTopic.Value, json);

        //            //mqttClientConnector.Publish(json,
        //            //                            mqttToolDataLoggerParameters.brokerTopic,
        //            //                            false,
        //            //                            mqttToolDataLoggerParameters.mqttConfigurationParameters.qos);

        //            //var firstItem = records.Cast<DataLoggerRecord>().ToList().ElementAt(records.Count - 1);
        //            //Log.Info("Last Record: " + firstItem.id.ToString());
        //            //UpdateDataLoggerLastRowPushed((UInt32)firstItem.id);
        //            //pendingSendPacket = null;
        //        }
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }


        //}

        //

        //try
        //{
        //    records = QueryOlderEntries(GetMaximumRecordsPerPacket(), (UInt64)lastPulledRecordId).Cast<Record>().ToList();
        //}
        //catch (Exception e)
        //{
        //    OnFetchError(e.Message);
        //}


    }

    private void OnRestartDataFetchTimer()
    {
        Log.Info("Executing Delayed Task");
        foreach (IUAVariable table in publisherStoreTablesTableNames.Children)
        {
            Int64 lastPulledRecordId = QueryTableLastRowId((string)table.Value);
            Int64 maxRowId = GetTableMaxRowId((string)table.Value);

            //se ci sono record da inviare allora predispongo il pacchetto per l'invio
            if (maxRowId > lastPulledRecordId)
            {
                List<LoggerRecord> loggerRecords = null;
                try
                {
                    loggerRecords = QueryOlderEntries((string)table.Value, (int)publisherStoreTablesMaximumItemsPerPacket.Value, lastPulledRecordId);
                    if (loggerRecords.Count > 0 && mqttClient != null)
                    {
                        pendingSendPacket = new LoggerRowPacket(DateTime.Now,
                                                                    (string)mqttAsClientClientIdVariable.Value,
                                                                    (string)table.Value,
                                                                    loggerRecords);

                        string json = GenerateJSON(pendingSendPacket);
                        sentPackages.Value++;
                        mqttClient.PublishAsync((string)publisherStoreTablesStoreTablesTopic.Value, json, mqttQoS[(int)publisherStoreTablesQoS.Value], (bool)publisherLiveTagsRetain.Value);
                        var lastRecord = loggerRecords.Last();
                        Log.Info("Last Record: " + lastRecord.Fields[0].value.ToString());
                        UpdateLastRowPushed((string)table.Value, UInt32.Parse(lastRecord.Fields[0].value));
                        pendingSendPacket = null;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        Log.Info("Finished Delayed Task");
        delayedTask.Dispose();
    }

    //public void FetchData(bool dl)
    //{
    //    jsonCreator = new JSONBuilder();
    //    Int64 lastPulledRecordId = QueryLastRowId(dl);
    //    Int64 maxRowId = GetMaxRowId(dl);

    //    //se ci sono record da inviare allora predispongo il pacchetto per l'invio
    //    if (maxRowId > lastPulledRecordId)
    //    {
    //        List<LoggerRecord> loggerRecords = null;
    //        try
    //        {
    //            loggerRecords = QueryOlderEntries(dl, (int)publisherDataLoggerMaximumItemsPerPacket.Value, lastPulledRecordId);
    //            if (loggerRecords.Count > 0)
    //            {
    //                Log.Info("Check 1");
    //                pendingSendPacket = new LoggerRowPacket(DateTime.Now,
    //                                                            (string)mqttAsClientClientIdVariable.Value,
    //                                                            loggerRecords);

    //                string json = GenerateJSON(pendingSendPacket);
    //                Log.Info(json);

    //                mqttClient.PublishAsync((string)publisherDataLoggerDataLoggerDataTopic.Value, json);

    //                //mqttClientConnector.Publish(json,
    //                //                            mqttToolDataLoggerParameters.brokerTopic,
    //                //                            false,
    //                //                            mqttToolDataLoggerParameters.mqttConfigurationParameters.qos);

    //                //var firstItem = records.Cast<DataLoggerRecord>().ToList().ElementAt(records.Count - 1);
    //                //Log.Info("Last Record: " + firstItem.id.ToString());
    //                //UpdateDataLoggerLastRowPushed((UInt32)firstItem.id);
    //                //pendingSendPacket = null;
    //            }
    //        }
    //        catch (Exception)
    //        {

    //            throw;
    //        }


    //    }

    //    //

    //    //try
    //    //{
    //    //    records = QueryOlderEntries(GetMaximumRecordsPerPacket(), (UInt64)lastPulledRecordId).Cast<Record>().ToList();
    //    //}
    //    //catch (Exception e)
    //    //{
    //    //    OnFetchError(e.Message);
    //    //}


    //}

    #endregion

    const string DlTableName = "DlSupportTable";
    const string AlarmsTableName = "AlarmSupportTable";
    const string supportStoreAndForwardDBName = "SupportStoreAndForwardDB";
    const string supportStoreAndForwardDBFilename = "SupportStoreAndForwardDB";
    private struct MQTTServerParameters
    {
        public IPAddress ipAddress;
        public ushort port;
        public SSLParameters sslParameters;

        public bool IsEmpty()
        {
            return ipAddress == null;
        }

        public bool HasSSL()
        {
            return !sslParameters.IsEmpty();
        }
    }

    private struct MQTTClientParameters
    {
        public SSLParameters sslParameters;

        public bool HasSSL()
        {
            return !sslParameters.IsEmpty();
        }
    }

    private ISessionHandler sessionHandler;
    private int defaultNamespaceIndex;

    private IMqttServer mqttServer;
    private IManagedMqttClient mqttClient;

    private IUAVariable mqttAsServer;
    private IUAVariable mqttAsServerIpAddressVariable;
    private IUAVariable mqttAsServerPortVariable;
    private IUAVariable mqttAsServerCertificateVariable;
    private IUAVariable mqttAsServerUseSSLVariable;
    private IUAVariable mqttAsServerCertificatePasswordVariable;
    private IUAVariable mqttAsServerUserAuthenticationVariable;
    private IUAVariable mqttAsServerAuthorizedUsersVariable;
    private IUAVariable mqttAsServerAutoStartVariable;
    private IUAVariable mqttAsServerIsRunningVariable;
    private IUAVariable mqttAsServerIsDebuggingModeVariable;
    private IUAVariable mqttAsServerMaxNumberOfConnectionsVariable;
    private IUAVariable mqttAsServerNumberOfConnections;

    private IUAVariable mqttAsClient;
    private IUAVariable mqttAsClientIpAddressVariable;
    private IUAVariable mqttAsClientPortVariable;
    private IUAVariable mqttAsClientClientIdVariable;
    private IUAVariable mqttAsClientConnected;
    private IUAVariable mqttAsClientUserAuthenticationVariable;
    private IUAVariable mqttAsClientAuthorizedUsersVariable;
    private IUAVariable mqttAsClientClientCertificateVariable;
    private IUAVariable mqttAsClientCaCertificateVariable;
    private IUAVariable mqttAsClientUseSSLVariable;
    private IUAVariable mqttAsClientCertificatePasswordVariable;
    private IUAVariable mqttAsClientAllowUntrustedCertificates;

    private IUAVariable publisher;
    private IUAVariable publisherLiveTags;
    private IUAVariable publisherLiveTagsPeriod;
    private IUAVariable publisherLiveTagsFolder;
    private IUAVariable publisherLiveTagsTopic;
    private IUAVariable publisherLiveTagsQoS;
    private IUAVariable publisherLiveTagsRetain;

    private IUAVariable publisherCustomPayload;
    private IUAVariable publisherCustomPayloadPeriod;
    private IUAVariable publisherCustomPayloadMessage;
    private IUAVariable publisherCustomPayloadTopic;
    private IUAVariable publisherCustomPayloadQoS;
    private IUAVariable publisherCustomPayloadRetain;

    private IUAVariable publisherStoreTables;
    private IUAVariable publisherStoreTablesStore;
    private IUAVariable publisherStoreTablesTableNames;
    private IUAVariable publisherStoreTablesMaximumItemsPerPacket;
    private IUAVariable publisherStoreTablesMaximumPublishTime;
    private IUAVariable publisherStoreTablesMinimumPublishTime;
    private IUAVariable publisherStoreTablesStoreTablesTopic;
    private IUAVariable publisherStoreTablesPreserveData;
    private IUAVariable publisherStoreTablesQoS;
    private IUAVariable publisherStoreTablesRetain;
    private IUAVariable publisherStoreTablesAllRows;
    private static IUAVariable publisherStoreTablesTablesPrefix;

    private IDictionary<int, MqttQualityOfServiceLevel> mqttQoS;

    private int pendingPackage;

    private JSONBuilder jsonCreator;

    private IUAVariable subscriber;

    private IUAVariable subscriberLiveTags;
    private IUAVariable subscriberLiveTagsFolder;
    private IUAVariable subscriberLiveTagsTopic;
    private IUAVariable subscriberLiveTagsLastPackageTimestamp;

    private IUAVariable subscriberCustomPayload;
    private IUAVariable subscriberCustomPayloadMessage;
    private IUAVariable subscriberCustomPayloadTopic;

    private IUAVariable subscriberStoreTables;
    private IUAVariable subscriberStoreTablesStore;
    private IUAVariable subscriberStoreTablesStoreTablesTopic;

    private IUAVariable sentPackages;
    private IUAVariable receivedPackages;

    private LongRunningTask myLRT;
    private LoggerRowPacket pendingSendPacket;

    private ICollection<IUANode> liveVariables;
    private PeriodicTask liveDataTask;
    private PeriodicTask customPayloadTask;
    private DelayedTask delayedTask;
    private LongRunningTask mqttServerInstantiation;
    private AutoResetEvent mqttServerAutoResetEvent;

    private LongRunningTask mqttClientInstantiation;
    private AutoResetEvent mqttClientAutoResetEvent;

    private List<string> connectedClientIdList = new List<string>();
    private readonly int connectionBacklog = 1000;
    private int activeAlarmsCounter;

    private RemoteVariableSynchronizer variableSynchronizer;

    private bool closing = false;
}
