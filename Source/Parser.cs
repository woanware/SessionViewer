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
using Ionic.Zlib;
using HtmlAgilityPack;

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
        private LookupService _ls; 

        /// <summary>
        /// Time in the PCAP to wait before seeing what sessions can be written to the db
        /// </summary>
        public int BufferInterval { get; set; }
        public int SessionInterval { get; set; }
        public bool AutoGzip { get; set; }
        public bool AutoHttp { get; set; }
        public bool IgnoreLocal { get; set; }
        private long _packetCount;
        private long _maxSize;
        private Regex _regexHost;
        private Regex _regexMethod;
        private Regex _regexHttpResponse;
        private Regex _regexHttpRequest;
        private Regex _regexUnprintable;
        private Regex _regexHttpContentLength;
        private Regex _regexHttpContentEncoding;
        private Regex _regexChunkedEncoding;
        private Regex _regexLink;
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public Parser()
        {
            AutoGzip = false;
            _ls = new LookupService("GeoIP.dat", LookupService.GEOIP_MEMORY_CACHE);

            _regexHost = new Regex(@"^Host:\s*(.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
           // _regexMethod = new Regex(@"^()\s+.HTTP/1\.[0,1]", RegexOptions.Compiled);
            _regexHttpResponse = new Regex(@"^HTTP/1\.[0,1]\s+\d*\s\w*.*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _regexHttpRequest = new Regex(@"^(GET|HEAD|POST|DELETE|OPTIONS|PUT|TRACE|TRACK)\s+.*HTTP/1\.[01]", RegexOptions.Compiled);
            _regexUnprintable = new Regex(@"[^\u0000-\u007F]", RegexOptions.Compiled);
            _regexHttpContentLength = new Regex(@"^content-length:\s*(\d*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _regexHttpContentEncoding = new Regex(@"^content-encoding:\s*(\w*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _regexChunkedEncoding = new Regex(@"^transfer-encoding:\s*chunked", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _regexMethod = new Regex(@"^(GET|HEAD|POST|DELETE|OPTIONS|PUT|TRACE|TRACK|CONNECT)\s+?([http://|https://|/|\w|\d].*)HTTP/1\.[01]", RegexOptions.Compiled);
            _regexLink = new Regex(@"<a href=[""|'](.*)[""|']>.*?</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            _regexHost = new Regex(@"^Host:\s*(.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);
            //_regexMethod = new Regex(@"^(GET|HEAD|POST|DELETE|OPTIONS|PUT|TRACE|TRACK)\s+?([http://|/].*)HTTP/1\.[01]", RegexOptions.Compiled);
            _regexLink = new Regex(@"<a href=[""|'](.*)[""|']>.*?</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);
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
            (new Thread(() =>
            {
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
                    PacketReconstructor packetReconstructor = new PacketReconstructor(_outputPath, AutoHttp, AutoGzip, _maxSize);
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
                    NPoco.Transaction transaction = db.GetTransaction();
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

                        try
                        {
                            Country country = _ls.getCountry(connection.SourceIp);
                            if (country != null)
                            {
                                session.SourceCountry = country.getCode();
                            }
                        }
                        catch (Exception ex) { }
                        
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
                        catch (Exception) {}

                        session.TimestampFirstPacket = _dictionary[connection].TimestampFirstPacket;
                        session.TimestampLastPacket = _dictionary[connection].TimestampLastPacket;
                        session.IsGzipped = _dictionary[connection].IsGzipped;
                        session.IsChunked = _dictionary[connection].IsChunked;
                        //session.HttpHost = _dictionary[connection].HttpHost;
                        //session.HttpMethods = _dictionary[connection].HttpMethods;

                        bool ishttp = _dictionary[connection].IsHttp;
                        //string httpHost = _dictionary[connection].HttpHost;
                        _dictionary[connection].Dispose();
                        _dictionary.Remove(connection);

                        if (ishttp == true)
                        {
                            if (AutoGzip == true)
                            {
                                if (session.IsGzipped == true & session.IsChunked == false)
                                {
                                    PerformGzipDecode(session.Guid);
                                }
                            }

                            session = ParseHttpData(session);
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
        //
        
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.Error("Unable to parse URL: " + link.Attributes[attribute].Value + " (" + ex.Message + ")");
        //        }
        //    }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="guid"></param>
        private Session ParseHttpData(Session session)
        {
            string fileName = string.Empty;
            if (File.Exists(System.IO.Path.Combine(_outputPath, session.Guid + ".txt")) == true)
            {
                fileName = System.IO.Path.Combine(_outputPath, session.Guid + ".txt");
            }
            else
            {
                fileName = System.IO.Path.Combine(_outputPath, session.Guid + ".bin");
            }

            string line = string.Empty;
            bool haveHttpHost = false;
            List<string> httpMethods = new List<string>();
            List<string> urls = new List<string>();
            List<string> links = new List<string>();

            if (session.DataSize > 102400) // 100 MB
            {
                using (FileStream storageInfo = new System.IO.FileStream(System.IO.Path.Combine(_outputPath, session.Guid + ".info"), 
                                                                         System.IO.FileMode.Create))
                using (System.IO.StreamReader file = new System.IO.StreamReader(fileName))
                {
                    while ((line = file.ReadLine()) != null)
                    {
                        ParseHttpUrlsAndMethods(session, 
                                                haveHttpHost, 
                                                line, 
                                                httpMethods, 
                                                urls, 
                                                storageInfo);

                        MatchCollection matchesLink = _regexLink.Matches(line);
                        foreach (Match match in matchesLink)
                        {
                            string md5 = Text.ConvertByteArrayToHexString(Security.GenerateMd5Hash(match.Groups[1].Value.Trim()));
                            if (links.Contains(md5) == false)
                            {
                                links.Add(md5);
                                Functions.WriteToFileStream(storageInfo, "LINK: " + match.Groups[1].Value + Environment.NewLine);
                            }
                        }
                    }
                }
            }
            else
            {
                using (FileStream storageInfo = new FileStream(System.IO.Path.Combine(_outputPath, session.Guid + ".info"), 
                                                               System.IO.FileMode.Create))
                using (System.IO.StreamReader file = new System.IO.StreamReader(fileName))
                {
                    while ((line = file.ReadLine()) != null)
                    {
                        ParseHttpUrlsAndMethods(session, 
                                                haveHttpHost, 
                                                line, 
                                                httpMethods, 
                                                urls, 
                                                storageInfo);
                    }
                }

                ParseLinks(fileName, session.Guid);
            }

            httpMethods.Sort();
            session.HttpMethods = string.Join(",", httpMethods);

            return session;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="haveHttpHost"></param>
        /// <param name="line"></param>
        /// <param name="httpMethods"></param>
        /// <param name="urls"></param>
        /// <param name="storageInfo"></param>
        private void ParseHttpUrlsAndMethods(Session session, 
                                             bool haveHttpHost, 
                                             string line, 
                                             List<string> httpMethods, 
                                             List<string> urls, 
                                             FileStream storageInfo)
        {
            if (haveHttpHost == false)
            {
                Match match = _regexHost.Match(line);
                if (match.Success == true)
                {
                    haveHttpHost = true;
                    session.HttpHost = match.Groups[1].Value.Trim();
                }
            }

            Match matchMethod = _regexMethod.Match(line);
            if (matchMethod.Success == true)
            {
                if (httpMethods.Contains(matchMethod.Groups[1].Value) == false)
                {
                    httpMethods.Add(matchMethod.Groups[1].Value);
                }

                if (matchMethod.Groups[2].Value.Trim().Length > 0)
                {
                    string md5 = Text.ConvertByteArrayToHexString(Security.GenerateMd5Hash(matchMethod.Groups[2].Value.Trim()));
                    if (urls.Contains(md5) == false)
                    {
                        urls.Add(md5);
                        Functions.WriteToFileStream(storageInfo, "URL: " + matchMethod.Groups[2].Value + Environment.NewLine);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="guid"></param>
        private void ParseLinks(string file, string guid)
        {
            List<string> links = new List<string>();

            string data = File.ReadAllText(file);
            HtmlNode.ElementsFlags.Remove("form");

            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(data);

            if (htmlDocument.DocumentNode.SelectNodes(@"//a[@href]") == null)
            {
                return;
            }

            using (FileStream storageInfo = new System.IO.FileStream(System.IO.Path.Combine(_outputPath, guid + ".info"), System.IO.FileMode.Append))
            {
                foreach (HtmlNode link in htmlDocument.DocumentNode.SelectNodes(@"//a[@href]"))
                {
                    try
                    {
                        if (link.Attributes["href"] == null)
                        {
                            continue;
                        }

                        string md5 = Text.ConvertByteArrayToHexString(Security.GenerateMd5Hash(link.Attributes["href"].Value.Trim()));
                        if (md5.ToLower() == "6666cd76f96956469e7be39d750cc7d9")
                        {
                            // Ignore "/"
                            continue;
                        }

                        if (links.Contains(md5) == false)
                        {
                            links.Add(md5);
                            Functions.WriteToFileStream(storageInfo, "LINK: " + link.Attributes["href"].Value + Environment.NewLine);
                        }
                    }
                    catch (Exception) { }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="guid"></param>
        private void PerformGzipDecode(string guid)
        {
            try
            {
                StringBuilder gzip = new StringBuilder();
                StringBuilder ascii = new StringBuilder();
                byte[] temp = File.ReadAllBytes(System.IO.Path.Combine(_outputPath, guid + ".bin"));

                using (MemoryStream memoryStream = new MemoryStream(temp))
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

                IO.WriteTextToFile(gzip.ToString(), System.IO.Path.Combine(_outputPath, guid + ".html"), false);
                IO.WriteTextToFile(ascii.ToString(), System.IO.Path.Combine(_outputPath, guid + ".txt"), false);
            }
            catch (Exception ex) 
            {
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
            int contentLength = -1;
            string contentEncoding = string.Empty;
            bool chunked = false;
            bool noContentLength = false;
            long bodyStartPosition = 0;
            long bodyEndPosition = streamReader.BaseStream.Length;
            while ((line = ReadLine(streamReader)) != null)
            {
                // If we have no "Content-Length" then we just need to cycle 
                // through to the end, so that we have the end position
                if (noContentLength == true)
                {
                    if (line != string.Empty)
                    {
                        continue;
                    }
                }

                if (line == string.Empty)
                {
                    if (contentLength == -1)
                    {
                        if (noContentLength == true)
                        {
                            return PerformDecompression(streamReader, 
                                                        bodyStartPosition, 
                                                        streamReader.BaseStream.Position - bodyStartPosition, 
                                                        contentEncoding, 
                                                        chunked, 
                                                        response);
                        }
                        else
                        {
                            noContentLength = true;
                            bodyStartPosition = streamReader.BaseStream.Position;
                            continue;
                        }
                    }
                    else
                    {
                        return PerformDecompression(streamReader,
                                                    bodyStartPosition,
                                                    contentLength,
                                                    contentEncoding,
                                                    chunked,
                                                    response);
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

                        response.AppendLine(line);
                        continue;
                    }
                }

                if (contentEncoding.Length == 0)
                {
                    Match match = _regexHttpContentEncoding.Match(line);
                    if (match.Success == true)
                    {
                        contentEncoding = match.Groups[1].Value;
                        response.AppendLine(line);
                        continue;
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

            if (contentLength == -1)
            {
                if (noContentLength == true)
                {
                    return PerformDecompression(streamReader,
                                                bodyStartPosition,
                                                streamReader.BaseStream.Position - bodyStartPosition,
                                                contentEncoding,
                                                chunked,
                                                response);
                }
            }

            return response.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamReader"></param>
        /// <param name="bodyStartPosition"></param>
        /// <param name="length"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="chunked"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        private string PerformDecompression(BinaryReader streamReader, 
                                            long bodyStartPosition, 
                                            long length,
                                            string contentEncoding, 
                                            bool chunked, 
                                            StringBuilder response)
        {
            byte[] body = new byte[length];
            streamReader.BaseStream.Seek(bodyStartPosition, SeekOrigin.Begin);
            int ret = streamReader.Read(body, 0, (int)length);

            if (contentEncoding.ToLower() == "gzip" & chunked == false)
            {
                byte[] decompressedTemp = Decompress(body);
                string decompressed = Encoding.ASCII.GetString(decompressedTemp);
                return response.Append(decompressed).ToString();
            }
            else if (contentEncoding.ToLower() == "gzip" & chunked == true)
            {
                //string line = string.Empty;
                //while ((line = ReadLine(streamReader)) != null)
                //{
                //    string length = "0x" + line;
                //}
                return response.ToString();
            }
            else
            {
                string decompressed = Encoding.ASCII.GetString(body);
                return response.Append(decompressed).ToString();
            }
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
            using (Ionic.Zlib.GZipStream stream = new Ionic.Zlib.GZipStream(new MemoryStream(gzip), Ionic.Zlib.CompressionMode.Decompress))
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
