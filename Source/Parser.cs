using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using PcapDotNet.Core;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using woanware;
using System.Linq;
using System.Text.RegularExpressions;

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
        public bool IsRunning { get; private set; }
        private Dictionary<Connection, PacketReconstructor> _dictionary;
        private string _outputPath = string.Empty;
        private DateTime _timestamp = DateTime.MinValue;
        /// <summary>
        /// Time in the PCAP to wait before seeing what sessions can be written to the db
        /// </summary>
        private int _bufferInterval = 10;
        private int _sessionInterval = 5;
        private long _packetCount;
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pcapPath"></param>
        /// <param name="outputPath"></param>
        public void Parse(string pcapPath, string outputPath)
        {
            Task task = Task.Factory.StartNew(() =>
            {
                try
                {
                    _outputPath = outputPath;

                    // Check for previous DB
                    if (File.Exists(System.IO.Path.Combine(_outputPath, Global.DB_FILE)) == true)
                    {
                        string retDel = IO.DeleteFiles(_outputPath);
                        if (retDel.Length > 0)
                        {
                            OnError("An error occurred whilst deleting the existing files: " + retDel);
                            return;
                        }
                    }

                    string ret = Db.CreateDatabase(_outputPath);
                    if (ret.Length > 0)
                    {
                        OnError("Unable to create database: " + ret);
                        return;
                    }

                    _packetCount = 0;
                    _dictionary = new Dictionary<Connection, PacketReconstructor>();

                    OfflinePacketDevice selectedDevice = new OfflinePacketDevice(pcapPath);
                    using (PacketCommunicator packetCommunicator = selectedDevice.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 1000))                                 
                    {
                        packetCommunicator.ReceivePackets(0, DispatcherHandler);
                    }

                    WriteOldSessions(null);

                    _dictionary.Clear();
                    _dictionary = null;

                    OnComplete();
                }
                catch (Exception ex)
                {
                    OnMessage("An error occurred whilst parsing: " + ex.Message);
                }
                finally
                {
                    IsRunning = false;
                }
            });
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
                    return;
                }

                Connection connection = new Connection(packet);
                
                if (_dictionary.ContainsKey(connection) == false)
                {
                    PacketReconstructor packetReconstructor = new PacketReconstructor(_outputPath);
                    _dictionary.Add(connection, packetReconstructor);
                }

                _dictionary[connection].ReassemblePacket(packet);

                if (_timestamp == DateTime.MinValue)
                {
                    _timestamp = packet.Timestamp;
                }

                // Only write the data after the user defined period
                if (packet.Timestamp > _timestamp.AddMinutes(_bufferInterval))
                {
                    _timestamp = packet.Timestamp;
                    WriteOldSessions(packet);
                }
            }
            catch (Exception ex)
            {
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
                Regex regex = new Regex(@"^Host:\s+(.*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

                OnMessage("Writing session data to database..." + _packetCount + " packets");
                using (DbConnection dbConnection = Db.GetOpenConnection(_outputPath))
                using (var db = new NPoco.Database(dbConnection, NPoco.DatabaseType.SQLCe))
                {
                    NPoco.Transaction transaction = db.GetTransaction();
                    var keys = _dictionary.Keys.ToList();
                    foreach (var connection in keys)
                    {
                        var temp = _dictionary[connection];

                        if (packet != null)
                        {
                            if (temp.TimestampLastPacket == null)
                            {
                                if (temp.DataSize == 0)
                                {
                                    _dictionary[connection].Dispose();
                                    _dictionary.Remove(connection);
                                }
                                continue;
                            }

                            // Lets ignore sessions that are still within the threshold
                            if (packet.Timestamp < temp.TimestampLastPacket.Value.AddMinutes(_sessionInterval))
                            {
                                continue;
                            }
                        }

                        Session session = new Session(temp.Guid);
                        session.DataSize = temp.DataSize;
                        if (session.DataSize == 0)
                        {
                            _dictionary[connection].Dispose();
                            _dictionary.Remove(connection);
                            continue;
                        }

                        session.SourceIp = connection.SourceIpNumeric;
                        session.SourcePort = connection.SourcePort;
                        session.DestinationIp = connection.DestinationIpNumeric;
                        session.DestinationPort = connection.DestinationPort;
                        session.TimestampFirstPacket = _dictionary[connection].TimestampFirstPacket;
                        session.TimestampLastPacket = _dictionary[connection].TimestampLastPacket;

                        _dictionary[connection].Dispose();
                        _dictionary.Remove(connection);

                        string host = File.ReadAllText(System.IO.Path.Combine(_outputPath, session.Guid + ".txt"));
                        Match match = regex.Match(host);
                        if (match.Success == true)
                        {
                            session.HttpHost = match.Groups[1].Value.Trim();
                        }

                        db.Insert("Sessions", "Id", session);
                       
                    }

                    transaction.Complete();
                }
            }
            catch (Exception ex)
            {
                OnMessage("An error occurred whilst writing the old sessions to the database: " + ex.Message);                
            }
            finally
            {
                OnMessage(string.Empty);
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
