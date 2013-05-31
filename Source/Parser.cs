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
using System.Threading;
using System.Text;
using System.IO.Compression;
using System.Web;
using System.Diagnostics;
using ProtoBuf;

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
        public int BufferInterval { get; set; }
        public int SessionInterval { get; set; }
        public bool AutoGzip { get; set; }
        private long _packetCount;
        private Regex _regexHost;
        private Regex _regexMethod;
        private Regex _regexHttpResponse;
        private Regex _regexHttpRequest;
        private Regex _regexUnprintable;
        private Regex _regexHttpContentLength;
        private Regex _regexHttpContentEncoding;
        private Regex _regexChunkedEncoding;
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public Parser()
        {
            _regexHost = new Regex(@"^Host:\s+(.*)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
            _regexMethod = new Regex(@"^()\s+.HTTP/1\.[0,1]", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
            _regexHttpResponse = new Regex(@"^HTTP/1\.[0,1]\s+\d*\s\w*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            //_regexHttpRequest = new Regex(@"^.*\s.*HTTP/1.[0,1]", RegexOptions.Compiled);
            _regexHttpRequest = new Regex(@"^(GET|HEAD|POST|DELETE|OPTIONS|PUT|TRACE|TRACK)\s+.*HTTP/1\.[01]", RegexOptions.Compiled);
            _regexUnprintable = new Regex(@"[^\u0000-\u007F]", RegexOptions.Compiled);
            _regexHttpContentLength = new Regex(@"^content-length:\s*(\d*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _regexHttpContentEncoding = new Regex(@"^content-encoding:\s*(\w*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _regexChunkedEncoding = new Regex(@"^transfer-encoding:\s*chunked", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pcapPath"></param>
        /// <param name="outputPath"></param>
        public void Parse(string pcapPath, string outputPath)
        {
            (new Thread(() =>
            {
                try
                {
                    //Stopwatch sw = new Stopwatch();
                    //sw.Start();
                    _outputPath = outputPath;

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

                    WriteOldSessions(null);

                    _dictionary.Clear();
                    _dictionary = null;

                    //sw.Stop();
                    //UserInterface.DisplayMessageBox(sw.Elapsed.ToString(), System.Windows.Forms.MessageBoxIcon.Information);

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
            })).Start();
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
                if (packet.Timestamp > _timestamp.AddMinutes(BufferInterval))
                {
                    _timestamp = packet.Timestamp;
                    WriteOldSessions(packet);
                }

                if (_packetCount % 1000 == 0)
                {
                    OnMessage("Processed " + _packetCount + " packets...");
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
                            if (packet.Timestamp < temp.TimestampLastPacket.Value.AddMinutes(SessionInterval))
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
                        session.IsGzipped = _dictionary[connection].IsGzipped;
                        session.IsChunked = _dictionary[connection].IsChunked;
                        session.HttpHost = _dictionary[connection].HttpHost;
                        session.HttpMethods = _dictionary[connection].HttpMethods;

                        bool ishttp = _dictionary[connection].IsHttp;
                        string httpHost = _dictionary[connection].HttpHost;
                        _dictionary[connection].Dispose();
                        _dictionary.Remove(connection);

                        if (ishttp == true)
                       {
                        //    string text = File.ReadAllText(System.IO.Path.Combine(_outputPath, session.Guid + ".txt"));
                        //    //Match match = _regexHost.Match(text);
                        //    //if (match.Success == true)
                        //    //{
                        //    session.HttpHost = httpHost;
                        //    //}

                        //    //MatchCollection matches = _regexMethod.Matches(text);
                        //    //if (matches.Count > 0)
                        //    //{
                        //    //    if (matches.Count == 1)
                        //    //    {
                        //    //        session.HttpMethods = matches[0].Value.Trim();
                        //    //    }
                        //    //    else
                        //    //    {
                        //    //        var methods = matches.Cast<Match>().Select(mm => mm.Value).Skip(1).Distinct();
                        //    //        session.HttpMethods = string.Join(",", methods.ToList());
                        //    //        if (session.HttpMethods.Length > 100)
                        //    //        {
                        //    //            session.HttpMethods = session.HttpMethods.Substring(0, 100);
                        //    //        }

                        //    //        //session.HttpMethods = "Mixed";
                        //    //    }
                        //    }

                            if (AutoGzip == true)
                            {
                                if (session.IsGzipped == true & session.IsChunked == false)
                                {
                                    PerformGzipDecode(session.Guid);
                                }
                            }
                        }

                        db.Insert("Sessions", "Id", session);
                    }

                    OnMessage("Commiting database transaction..." + _packetCount + " packets");
                    transaction.Complete();
                }
            }
            catch (Exception ex)
            {
                OnError("An error occurred whilst writing the old sessions to the database: " + ex.Message);                
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
        private void PerformGzipDecode(string guid)
        {
            Storage storage = null;
            try
            {
                StringBuilder gzip = new StringBuilder();
                StringBuilder ascii = new StringBuilder();
                using (var file = File.OpenRead(System.IO.Path.Combine(_outputPath, guid + ".bin")))
                {
                    storage = Serializer.Deserialize<Storage>(file);

                    using (MemoryStream memoryStream = new MemoryStream(storage.Hex.ToArray()))
                    using (BinaryReader streamReader = new BinaryReader(memoryStream, Encoding.ASCII))
                    {
                        gzip.Append(Global.HTML_HEADER);

                        string line = string.Empty;
                        while ((line = ReadLine(streamReader)) != null)
                        {
                            if (_regexHttpRequest.Match(line).Success == true)
                            {
                                string request = ParseRequest(streamReader, line);
                                string sanitised = _regexUnprintable.Replace(woanware.Text.ReplaceNulls(request), ".");

                                ascii.Append(sanitised);
                                gzip.Append(Functions.GenerateHtml(sanitised, true));
                                continue;
                            }

                            if (_regexHttpResponse.Match(line).Success == true)
                            {
                                string response = ParseResponse(streamReader, line);
                                if (response == string.Empty)
                                {
                                    continue;
                                }

                                string sanitised = _regexUnprintable.Replace(woanware.Text.ReplaceNulls(response), ".");
                                gzip.Append(Functions.GenerateHtml(sanitised, false));

                                //ascii.Append(sanitised);

                                if (sanitised.EndsWith("\r\n\r\n") == false)
                                {
                                    sanitised += Environment.NewLine + Environment.NewLine;
                                }

                                ascii.Append(sanitised);
                                
                            }
                        }

                        gzip.Append(Global.HTML_FOOTER);
                    }
                }

                storage.Gzip = gzip.ToString();
                storage.Ascii = ascii.ToString();
                using (var file = File.Create(System.IO.Path.Combine(_outputPath, guid + ".bin")))
                {
                    Serializer.Serialize(file, storage);
                }
            }
            catch (Exception)
            {
                if (storage == null)
                {
                    return;
                }

                // Clear out the gzip content so that the default Html property will be displayed
                storage.Gzip = string.Empty;
                using (var file = File.Create(System.IO.Path.Combine(_outputPath, guid + ".bin")))
                {
                    Serializer.Serialize(file, storage);
                }
            }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamReader"></param>
        /// <param name="previousLine"></param>
        private string ParseRequest(BinaryReader streamReader, string previousLine)
        {
            StringBuilder request = new StringBuilder();
            request.AppendLine(previousLine);

            long previousPosition = 0;
            string line = string.Empty;
            while ((line = ReadLine(streamReader)) != null)
            {
                if (_regexHttpResponse.Match(line).Success == true)
                {
                    streamReader.BaseStream.Seek(previousPosition, SeekOrigin.Begin);
                    return request.ToString();
                }

                request.AppendLine(line);
                previousPosition = streamReader.BaseStream.Position;
            }

            return request.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamReader"></param>
        /// <param name="previousLine"></param>
        /// <returns></returns>
        private string ParseResponse(BinaryReader streamReader,
                                     string previousLine)
        {
            StringBuilder response = new StringBuilder();
            response.AppendLine(previousLine);

            string line = string.Empty;
            int contentLength = 0;
            string contentType = string.Empty;
            bool chunked = false;
            while ((line = ReadLine(streamReader)) != null)
            {
                if (line == string.Empty)
                {
                    //response.Append(Environment.NewLine);

                    byte[] body = new byte[contentLength];
                    int ret = streamReader.Read(body, 0, contentLength);

                    if (contentType.ToLower() == "gzip" & chunked == false)
                    {
                        byte[] decompressedTemp = Decompress(body);
                        string decompressed = Encoding.ASCII.GetString(decompressedTemp);
                        return response.Append(decompressed).ToString();
                    }
                    else if (contentType.ToLower() == "gzip" & chunked == true)
                    {
                        while ((line = ReadLine(streamReader)) != null)
                        {
                            string length = "0x" + line;
                        }
                    }
                    else
                    {
                        string decompressed = Encoding.ASCII.GetString(body);
                        return response.Append(decompressed).ToString();
                    }
                }

                if (contentLength == 0)
                {
                    Match match = _regexHttpContentLength.Match(line);
                    if (match.Success == true)
                    {
                        if (int.TryParse(match.Groups[1].Value, out contentLength) == false)
                        {
                            return string.Empty;
                        }
                    }
                }

                if (contentType.Length == 0)
                {
                    Match match = _regexHttpContentEncoding.Match(line);
                    if (match.Success == true)
                    {
                        contentType = match.Groups[1].Value;
                    }
                }

                if (chunked == false)
                {
                    Match match = _regexChunkedEncoding.Match(line);
                    if (match.Success == true)
                    {
                        chunked = true;
                    }
                }

                response.AppendLine(line);
            }

            return response.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        private string ReadLine(BinaryReader streamReader)
        {
            StringBuilder builder = new StringBuilder();
            while (true)
            {
                int num = streamReader.Read();
                switch (num)
                {
                    case -1:
                        if (builder.Length > 0)
                        {
                            return builder.ToString();
                        }

                        return null;

                    case 13:
                    case 10:
                        if (num == 13)
                        {
                            num = streamReader.Read();
                            if (num == 10)
                            {
                                return builder.ToString();
                            }

                            builder.Append((char)num);
                        }
                        break;
                }

                builder.Append((char)num);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gzip"></param>
        /// <returns></returns>
        private byte[] Decompress(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip),
                                                      CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

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
