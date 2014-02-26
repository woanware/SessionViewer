using PcapDotNet.Core;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using woanware;
using woanware.Network;
using System.Threading.Tasks;
using System.Data;

namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    public class Parser
    {
        #region Events
        public event Global.DefaultEvent Complete;
        public event Global.MessageEvent Exclamation;
        public event Global.MessageEvent Message;
        public event Global.MessageEvent Error;
        #endregion

        #region Member Variables
        private CancellationTokenSource cancelSource = null;
        public bool IsRunning { get; private set; }
        public bool IsStopping { get; private set; }
        private Dictionary<Connection, PacketReconstructor> _dictionary;
        private string _outputPath = string.Empty;
        private DateTime _timestamp = DateTime.MinValue;
        private LookupService _ls; 

        /// <summary>
        /// Time in the PCAP to wait before seeing what sessions can be written to the db
        /// </summary>
        public int BufferInterval { get; set; }
        public int SessionInterval { get; set; }
        public int Threads { get; set; }
        public bool IgnoreLocal { get; set; }
        private long _packetCount;
        private long _maxSize;

        private List<InterfaceParser> _parsers;
        private SessionParser sessionParser;
        private AutoResetEvent done = null;
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public Parser(int threads)
        {
            this.Threads = threads;

            _ls = new LookupService("GeoIP.dat", LookupService.GEOIP_MEMORY_CACHE);

            sessionParser = new SessionParser(this.Threads);
            sessionParser.MessageEvent += SessionParser_MessageEvent;
            sessionParser.CompleteEvent += SessionParser_CompleteEvent;
        }

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pcapPath"></param>
        /// <param name="outputPath"></param>
        /// <param name="maxSize"></param>
        public void Parse(string pcapPath, 
                          string outputPath,
                          long maxSize)
        {
            this.cancelSource = new CancellationTokenSource();
            Task task = Task.Factory.StartNew(() =>
            {
                this.IsRunning = true;
                this.IsStopping = false;
                this.sessionParser.Start();

                try
                {
                    _outputPath = outputPath;
                    _maxSize = maxSize;

                    // Check for previous DB
                    if (File.Exists(System.IO.Path.Combine(_outputPath, Global.DB_FILE)) == true)
                    {
                        OnMessage("Deleting database...");

                        string retDel = IO.DeleteFiles(_outputPath);
                        if (retDel.Length > 0)
                        {
                            OnError("An error occurred whilst deleting the existing files: " + retDel);
                            return;
                        }
                    }

                    woanware.IO.WriteTextToFile("Start: " + DateTime.Now.ToString() + Environment.NewLine, System.IO.Path.Combine(_outputPath, "Log.txt"), true);

                    OnMessage("Creating database...");

                    string ret = Db.CreateDatabase(_outputPath);
                    if (ret.Length > 0)
                    {
                        OnError("Unable to create database: " + ret);
                        return;
                    }

                    OnMessage("Database created...");

                    _packetCount = 0;
                    _dictionary = new Dictionary<Connection, PacketReconstructor>();

                    OfflinePacketDevice selectedDevice = new OfflinePacketDevice(pcapPath);
                    using (PacketCommunicator packetCommunicator = selectedDevice.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 1000))
                    {
                        packetCommunicator.ReceivePackets(0, DispatcherHandler);
                    }

                    // Write any remaining sessions
                    WriteOldSessions(null);

                    _dictionary.Clear();
                    _dictionary = null;

                    sessionParser.SetProcessed();

                    OnMessage("All sessions added to queue, now waiting for session parsing to complete...");

                    this.done = new AutoResetEvent(false);
                    this.done.WaitOne();

                    woanware.IO.WriteTextToFile("End: " + DateTime.Now.ToString() + Environment.NewLine, System.IO.Path.Combine(_outputPath, "Log.txt"), true);
                    woanware.IO.WriteTextToFile("Packets: " + _packetCount + Environment.NewLine, System.IO.Path.Combine(_outputPath, "Log.txt"), true);
                    woanware.IO.WriteTextToFile("TCP Sessions: " + this.sessionParser.TotalSessions + Environment.NewLine, System.IO.Path.Combine(_outputPath, "Log.txt"), true);

                    OnComplete();
                }
                catch (Exception ex)
                {
                    OnError("An error occurred whilst parsing: " + ex.Message);
                }
                finally
                {
                    IsRunning = false;
                }
            }, cancelSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        public void ClearParsers()
        {
            _parsers = new List<InterfaceParser>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parser"></param>
        public void SetParser(InterfaceParser parser)
        {
            _parsers.Add(parser);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            this.IsStopping = true;
            cancelSource.Cancel();
            sessionParser.Stop();
        }
        #endregion

        #region Session Parser Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void SessionParser_MessageEvent(string message)
        {
            OnMessage(message);
        }

        /// <summary>
        /// 
        /// </summary>
        private void SessionParser_CompleteEvent()
        {
            if (done != null)
            {
                done.Set();
                done = null;
            }
        }
        #endregion

        #region Misc Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="packet"></param>
        private void DispatcherHandler(PcapDotNet.Packets.Packet packet)
        {
            try
            {
                _packetCount++;
                IpV4Datagram ip = packet.Ethernet.IpV4;
                TcpDatagram tcp = ip.Tcp;

                if (tcp == null)
                {
                    Console.WriteLine("No TCP: " + ip.Source.ToString() + "#" + ip.Destination.ToString());
                    return;
                }

                if (IgnoreLocal == true)
                {
                    if (Networking.IsOnIntranet(System.Net.IPAddress.Parse(ip.Source.ToString())) == true &
                        Networking.IsOnIntranet(System.Net.IPAddress.Parse(ip.Destination.ToString())) == true)
                    {
                        return;
                    }
                }
                
                Connection connection = new Connection(packet);
                
                if (_dictionary.ContainsKey(connection) == false)
                {
                    PacketReconstructor packetReconstructor = new PacketReconstructor(_outputPath, _maxSize);

                    var parsers = from p in _parsers where p.Type == ParserType.Packet select p;
                    packetReconstructor.SetPacketParsers(parsers.ToList());
                    _dictionary.Add(connection, packetReconstructor);
                }

                _dictionary[connection].ReassemblePacket(packet);

                if (_timestamp == DateTime.MinValue)
                {
                    _timestamp = packet.Timestamp;
                }

                // Only write the data after the user defined period
                if (packet.Timestamp > _timestamp.AddMinutes(BufferInterval))
                {
                    _timestamp = packet.Timestamp;
                    WriteOldSessions(packet);
                }

                if (_packetCount % 10000 == 0)
                {
                    OnMessage("Processed " + _packetCount + " packets...(" + _dictionary.Count + " sessions)");
                }
            }
            catch (Exception ex)
            {
                OnMessage("Error: " + ex.ToString());
                Console.WriteLine(ex);
            } 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="packet"></param>
        private void WriteOldSessions(PcapDotNet.Packets.Packet packet)
        {
            try
            {
                OnMessage("Writing session data to database..." + _packetCount + " packets");
                using (DbConnection dbConnection = Db.GetOpenConnection(_outputPath))
                using (var db = new NPoco.Database(dbConnection, NPoco.DatabaseType.SQLCe))
                {
                    var keys = _dictionary.Keys.ToList();
                    foreach (var connection in keys)
                    {
                        var temp = _dictionary[connection];

                        if (temp.TimestampLastPacket == null)
                        {
                            if (temp.DataSize == 0)
                            {
                                _dictionary[connection].Dispose();
                                _dictionary.Remove(connection);
                            }
                            continue;
                        }

                        if (temp.HasFin == false)
                        {
                            if (packet != null)
                            {
                                // Lets ignore sessions that are still within the threshold
                                if (packet.Timestamp < temp.TimestampLastPacket.Value.AddMinutes(SessionInterval))
                                {
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            if (packet != null)
                            {
                                // Only kill sessions that have had a FIN and at least a minute has past
                                if (packet.Timestamp < temp.TimestampLastPacket.Value.AddMinutes(1))
                                {
                                    continue;
                                }
                            }
                        }

                        Session session = CreateNewSession(temp.Guid,
                                                           temp.DataSize,
                                                           connection);

                        _dictionary[connection].Dispose();
                        if (_dictionary.Remove(connection) == false)
                        {
                            Console.WriteLine("Unable to remove connection object: " + connection.GetName());
                        }

                        if (temp.DataSize > 0)
                        {
                            int pk = Convert.ToInt32(db.Insert("Sessions", "Id", session));
                            PerformSessionProcessing(session, pk);
                        }
                    }

                    OnMessage("Commiting database transaction..." + _packetCount + " packets");
                }
            }
            catch (Exception ex)
            {
                this.Log().Error(ex.ToString());
            }
            finally
            {
                OnMessage(string.Empty);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="dataSize"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        private Session CreateNewSession(string guid, 
                                         long dataSize, 
                                         Connection connection)
        {
            Session session = new Session(guid);
            session.DataSize = dataSize;
            session.SourceIp = connection.SourceIpNumeric;
            session.SourcePort = connection.SourcePort;

            try
            {
                Country country = _ls.getCountry(connection.SourceIp);
                if (country != null)
                {
                    session.SourceCountry = country.getCode();
                }
            }
            catch (Exception) { }

            session.DestinationIp = connection.DestinationIpNumeric;
            session.DestinationPort = connection.DestinationPort;

            try
            {
                Country country = _ls.getCountry(connection.DestinationIp);
                if (country != null)
                {
                    session.DestinationCountry = country.getCode();
                }
            }
            catch (Exception) { }

            session.TimestampFirstPacket = _dictionary[connection].TimestampFirstPacket;
            session.TimestampLastPacket = _dictionary[connection].TimestampLastPacket;

            return session;
        }

        /// <summary>
        /// Run any applicable parsers
        /// </summary>
        /// <param name="session"></param>
        /// <param name="db"></param>
        /// <param name="pk"></param>
        private void PerformSessionProcessing(Session session, 
                                              int pk)
        {
            //OnMessage("Performing session parsing...");

            var parsers = from p in this._parsers where (p.Port == session.SourcePort | p.Port == session.DestinationPort) & p.Type == ParserType.Session select p;
            foreach (InterfaceParser parser in parsers)
            {
                if (parser.Enabled == false)
                {
                    continue;
                }

                SessionTask sessionTask = new SessionTask(parser.Name, session, _outputPath, pk);
                this.sessionParser.Add(sessionTask);
            }
        }
        #endregion

        #region Event Methods
        /// <summary>
        /// 
        /// </summary>
        private void OnComplete()
        {
            var handler = Complete;
            if (handler != null)
            {
                handler();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void OnMessage(string message)
        {
            var handler = Message;
            if (handler != null)
            {
                handler(message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void OnExclamation(string message)
        {
            var handler = Exclamation;
            if (handler != null)
            {
                handler(message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="error"></param>
        private void OnError(string error)
        {
            var handler = Error;
            if (handler != null)
            {
                handler(error);
            }
        }
        #endregion
    }
}
