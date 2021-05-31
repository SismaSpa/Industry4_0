using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Connecting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using Sisma.Industry40Test.Models;
using Sisma.Industry40Test.Utils;

namespace Sisma.Industry40Test
{
    class Program
    {
        
        #region properties

        private static IMqttClient _client;
        private static IMqttClientOptions _clientOptions;

        private const string MACHINE_STATUS_TOPIC_SUFFIX = "/MachineStatus";
        private const string INFO_MESSAGE_TOPIC_SUFFIX = "/Info_Message";

        enum MachineType
        {
            LMD, SWX 
        }

        #endregion


        #region main

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("MQTT client example");

                string command, ip, port, serial, machineType;
                do
                {
                    Console.WriteLine();
                    Console.WriteLine(" > Press 'C' to connect, 'D' to disconnect");
                    Console.WriteLine(" > Press 'S' to read machine status, 'R' to read all machine messages, 'I' to send info message");
                    Console.WriteLine(" > Press 'Q' to close program");
                    Console.WriteLine();
                    command = Console.ReadLine();

                    if (!string.IsNullOrWhiteSpace(command) && !command.Equals("Q", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (command.Equals("C", StringComparison.CurrentCultureIgnoreCase))
                        {
                            // disconnect if already connected
                            DisconnectClient();

                            // broker ip 
                            Console.WriteLine("Insert broker IP (ex: '192.168.1.100' or 'localhost' for local broker connection):");
                            ip = Console.ReadLine();
                            IPAddress ipVal = IPAddress.Loopback;
                            if (!ip.Equals("localhost", StringComparison.CurrentCultureIgnoreCase))
                            {
                                bool validIp = IPAddress.TryParse(ip, out ipVal);
                                if (!validIp)
                                {
                                    Console.WriteLine($"Invalid ip value '{ip}'! Default will be used (localhost)");
                                    ipVal = IPAddress.Loopback;
                                }
                            }

                            // broker port
                            Console.WriteLine("Insert broker port (default is 1883):");
                            port = Console.ReadLine();
                            int portVal;
                            bool validPort = int.TryParse(port, out portVal);
                            if (!validPort)
                            {
                                Console.WriteLine("Invalid port value! Default will be used (1883)");
                                portVal = 1883;
                            }

                            // initialize MQTT client, connect to broker IP and port
                            Init(ipVal == IPAddress.Loopback ? "localhost" : ipVal.ToString(), portVal);
                            Connect();
                        }
                        else if (command.Equals("D", StringComparison.CurrentCultureIgnoreCase))
                        {
                            DisconnectClient();
                        }
                        else if (command.Equals("S", StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (_client == null || !_client.IsConnected)
                                Console.WriteLine("Connect client before read machine status!");
                            else
                            {
                                // subscribe to a topic
                                Console.WriteLine("Insert machine serial:");
                                serial = Console.ReadLine();

                                string topic = "Sisma/" + serial + "/+" + MACHINE_STATUS_TOPIC_SUFFIX;
                                Subscribe(topic);

                                // unsubscribe from topic and go back to menu
                                string unsub;
                                do
                                {
                                    Console.WriteLine("Press 'Q' to unsubscribe to machine status topic and go back to menu:");
                                    unsub = Console.ReadLine();

                                } while (!unsub.Equals("Q", StringComparison.CurrentCultureIgnoreCase));

                                Unsubscribe(topic);
                            }
                        }
                        else if (command.Equals("I", StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (_client == null || !_client.IsConnected)
                                Console.WriteLine("Connect client before send info message!");
                            else
                            {
                                // subscribe to a topic
                                Console.WriteLine("Insert machine serial:");
                                serial = Console.ReadLine();

                                Console.WriteLine("Insert machine type number");
                                // TODO: add other machine types
                                Console.WriteLine("0 = LMD, 1 = SWA/SWT");
                                machineType = Console.ReadLine();

                                bool validType = int.TryParse(machineType, out int mType);
                                if (validType && mType >= 0 && mType <= 1)
                                {
                                    string topic = "Sisma/" + serial + "/" + ((MachineType)mType).ToString() + INFO_MESSAGE_TOPIC_SUFFIX;
                                    string send, msg1, msg2;
                                    do
                                    {
                                        Console.WriteLine("Insert first message:");
                                        msg1 = Console.ReadLine();
                                        Console.WriteLine("Insert second message:");
                                        msg2 = Console.ReadLine();
                                        SendInfoMessage(topic, msg1, msg2);

                                        Console.WriteLine("Press 'Q' to go back to menu");
                                        Console.WriteLine("Press any key to send another message:");
                                        send = Console.ReadLine();

                                    } while (!send.Equals("Q", StringComparison.CurrentCultureIgnoreCase));

                                }
                                else
                                    Console.WriteLine("Invalid machine type!");
                            }
                        }
                        else if (command.Equals("R", StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (_client == null || !_client.IsConnected)
                                Console.WriteLine("Connect client before send info message!");
                            else
                            {
                                // subscribe to a topic
                                Console.WriteLine("Insert machine serial:");
                                serial = Console.ReadLine();

                                string topic = "Sisma/" + serial + "/#" ;
                                Subscribe(topic);

                                // unsubscribe from topic and go back to menu
                                string unsub;
                                do
                                {
                                    Console.WriteLine("Press 'Q' to unsubscribe to generic machine topic and go back to menu:");
                                    unsub = Console.ReadLine();

                                } while (!unsub.Equals("Q", StringComparison.CurrentCultureIgnoreCase));

                                Unsubscribe(topic);
                            }
                        }
                        else
                            Console.WriteLine("Insert a valid command!");
                    }

                } while (!command.Equals("Q", StringComparison.CurrentCultureIgnoreCase));                                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during client initialization/connection!");
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine(ex.InnerException.Message);
            }
            finally
            {
                DisconnectClient();
            }
        }

        #endregion


        #region methods

        /// <summary>
        /// Initialize MQTT client 
        /// </summary>
        /// <param name="serverUrl">broker URL</param>
        /// <param name="serverPort">broker port</param>
        private static void Init(string serverUrl = "localhost", int serverPort = 1883)
        {
            try
            {
                if (_client != null && _client.IsConnected)
                {
                    Console.WriteLine("Client already connected. It will be disconnected before reintializing!");
                    _client.DisconnectAsync();
                }

                MqttClientOptionsBuilder builder = new MqttClientOptionsBuilder()
                    .WithClientId("SismaMQTTClient")
                    .WithTcpServer(serverUrl, serverPort);
                Console.WriteLine($"Configuring client: server URL {serverUrl} - port [{serverPort}] ...");

                _clientOptions = builder.Build();
                Console.WriteLine("MQTT client initialized!");
            }
            catch (Exception ex)
            {
                throw new Exception("Error initializing MQTT client!", ex);
            }
        }

        /// <summary>
        /// Connect client using options 'IMqttClientOptions' built after calling 'Init()' method 
        /// </summary>        
        private static void Connect()
        {
            try
            {
                _client = new MqttFactory().CreateMqttClient();
                
                // connection event
                _client.UseConnectedHandler(e => Console.WriteLine("Client connected to broker"));
                // disconnection event
                _client.UseDisconnectedHandler(e => Console.WriteLine("Client disconnected from broker!"));
                // message event
                _client.UseApplicationMessageReceivedHandler(e =>
                {
                    if (e.ApplicationMessage.Topic.EndsWith(MACHINE_STATUS_TOPIC_SUFFIX))
                        ReadMachineStatusMessage(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
                    else
                        Console.WriteLine("Generic message received: '" + Encoding.UTF8.GetString(e.ApplicationMessage.Payload) + "' - topic '" + e.ApplicationMessage.Topic + "'");
                });

                _client.ConnectAsync(_clientOptions, CancellationToken.None);
            }
            catch (Exception ex)
            {
                throw new Exception("Error connecting MQTT client!", ex);
            }
        }        

        /// <summary>
        /// Subscribe to given topics
        /// </summary>
        /// <param name="topics">Optional topics to subscribe</param>
        private static void Subscribe(params string[] topics)
        {
            if (topics == null || topics.Length == 0)
            {
                Console.WriteLine("Give at least one topic as parameter to subscribe!");
                return;
            }

            try
            {
                // Subscribe to topic
                MqttTopicFilter[] topicsFilters = new MqttTopicFilter[topics.Length];
                for (int i = 0; i < topics.Length; i++)
                    topicsFilters[i] = new MqttTopicFilter() { Topic = topics[i] };

                _client.SubscribeAsync(topicsFilters);
                Console.WriteLine("Subscribed to topics: " + string.Join(" ", topics));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error subscribing to topic: " + string.Join(" ", topics) + ex);
            }
        }

        /// <summary>
        /// Unsubscribe from given topic
        /// </summary>
        /// <param name="topic">topic to unsubscribe from</param>
        private static void Unsubscribe(string topic)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(topic))
                {
                    Console.WriteLine("Give a valid topic as parameter to unsubscribe!");
                    return;
                }

                if (_client != null && _client.IsConnected)
                {
                    _client.UnsubscribeAsync(topic);
                    Console.WriteLine($"Unsubscribed from '{topic}' topic!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error unsubscribing from topic '{topic}' " + ex);
            }
        }

        /// <summary>
        /// Disconnect MQTT client and release all resources
        /// </summary>
        private static void DisconnectClient()
        {
            if (_client != null)
            {
                if (_client.IsConnected)
                    _client.DisconnectAsync();

                _client.Dispose();
                _client = null;
            }
        }

        /// <summary>
        /// Read machine status message received from 'MachineStatus' topic
        /// </summary>
        /// <param name="msg">message received</param>
        private static void ReadMachineStatusMessage(string msg)
        {
            try
            {
                // deserialize machine status message
                MachineStatus ms = (MachineStatus)JSONUtils.DeserializeJSON(typeof(MachineStatus), msg, out EventArgs deserErrorArgs);
                // check malformed JSON message
                if (deserErrorArgs != EventArgs.Empty)
                    Console.WriteLine("Wrong message received: " + ((Newtonsoft.Json.Serialization.ErrorEventArgs)deserErrorArgs).ErrorContext.Error.Message);
                else
                    Console.WriteLine($"Received status '{ms.Status}' at '{ms.TimeStamp}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading machine status message! " + ex.Message);
            }
        }

        /// <summary>
        /// Send 'Info_Message' message to broker
        /// </summary>
        /// <param name="topic">topic to send message</param>
        /// <param name="msg1">first message</param>
        /// <param name="msg2">second message</param>
        private static void SendInfoMessage(string topic, string msg1, string msg2)
        {
            try
            {
                // build info message and serialize it before sending
                LmdSwxInfoMessage im = new LmdSwxInfoMessage()
                {
                    Message1 = msg1,
                    Message2 = msg2
                };
                string message = JSONUtils.SerializeJSON(im, out EventArgs serErrorArgs);

                // configure message options and send it
                var messageBuilder = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(message)
                    .WithExactlyOnceQoS()
                    .WithRetainFlag()
                    .Build();
                 _client.PublishAsync(messageBuilder);

                Console.WriteLine($"Sent message '{message}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending InfoMessage! " + ex);
            }
        }

        #endregion

    }
}
