using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using PcapDotNet.Packets.Http;

// Translated from the file follow.c from WireShark source code
// the code can be found at: http://www.wireshark.org/download.html

namespace SessionViewer
{
    /* */

    /// <summary>
    /// A class that represent a node in a linked list that holds partial TCP session fragments 
    /// </summary>
    internal class tcp_frag
    {
        public ulong seq = 0;
        public ulong len = 0;
        public ulong data_len = 0;
        public byte[] data = null;
        public tcp_frag next = null;
    };

    /// <summary>
    /// Attempt to try and reconstruct the data portion of a TCP
    /// session. We will try and handle duplicates, TCP fragments, 
    /// and out of order packets in a smart way.
    /// </summary>
    public class PacketReconstructor : IDisposable
    {
        #region Events
        //public event Global.MessageEvent Exclamation;
        //public event Global.MessageEvent Error;
        #endregion

        #region Member Variables
        public string Guid { get; private set; }
        private tcp_frag[] _frags = new tcp_frag[2];  // holds two linked list of the session data, one for each direction    
        private ulong[] _seq = new ulong[2]; // holds the last sequence number for each direction
        private long[] _srcAddr = new long[2];
        private uint[] _srcPort = new uint[2];
        private uint[] _tcpPort = new uint[2];
        public DateTime? TimestampFirstPacket { get; set; }
        public DateTime? TimestampLastPacket { get; set; }
        //private FileStream _dataFileHex = null;
        //private FileStream _dataFileHtml = null;
        //private FileStream _dataFileText = null;
        private bool _lastPacketOutbound = true;
        public long DataSize { get; private set; }
        private Regex _regexGzip;
        private Regex _regexChunked;
        //private Regex _regexHttp;
        private Regex _regexHost;
        private Regex _regexMethod;
        private Regex _regexLink;
       //private int _packetCount;
        public bool IsGzipped { get; private set; }
        public bool HasFin { get; private set; }
        public bool IsChunked { get; private set; }
        public bool IsHttp { get; private set; }
        private bool _haveHttpHost;
        public string HttpHost { get; private set; }
        private List<string> _httpMethods;
        //private List<string> _httpUrls;
        private bool _autoHttp;
        private bool _autoGzip;
        private long _maxSize;
        private FileStream _storageHex = null;
        private FileStream _storageHtml = null;
        private FileStream _storageInfo = null;
        //private Storage _storage;
        private string _outputPath;
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="autoHttp"></param>
        /// <param name="autoGzip"></param>
        /// <param name="maxSize"></param>
        public PacketReconstructor(string outputPath, 
                                   bool autoHttp,
                                   bool autoGzip,
                                   long maxSize)
        {
            _outputPath = outputPath;
            _autoHttp = autoHttp;
            _autoGzip = autoGzip;
            _maxSize = maxSize;

            this.Guid = System.Guid.NewGuid().ToString();
            ResetTcpReassembly();

            HttpHost = string.Empty;
            _regexGzip = new Regex(@"^content-encoding:\s*gzip", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
            _regexChunked = new Regex(@"^transfer-encoding:\s*chunked", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
            _regexHost = new Regex(@"^Host:\s*(.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);
            _regexMethod = new Regex(@"^(GET|HEAD|POST|DELETE|OPTIONS|PUT|TRACE|TRACK)\s+?([http://|/].*)HTTP/1\.[01]", RegexOptions.Compiled);
            _regexLink = new Regex(@"<a href=[""|'](.*)[""|']>.*?</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            _httpMethods = new List<string>();

            _storageHex = new System.IO.FileStream(Path.Combine(_outputPath, Guid + ".bin"), System.IO.FileMode.Create);
            _storageInfo= new System.IO.FileStream(Path.Combine(_outputPath, Guid + ".urls"), System.IO.FileMode.Create);
            _storageHtml = new System.IO.FileStream(Path.Combine(_outputPath, Guid + ".html"), System.IO.FileMode.Create);
            Functions.WriteToFileStream(_storageHtml, Global.HTML_HEADER);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            try
            {
                Functions.WriteToFileStream(_storageHtml, Global.HTML_FOOTER);
                _storageHex.Dispose();
                _storageInfo.Dispose();
                _storageHtml.Dispose();
            }
            catch (Exception) {}
        }

        /// <summary>
        /// The main function of the class receives a tcp packet and reconstructs the stream
        /// </summary>
        /// <param name="packet"></param>
        public void ReassemblePacket(PcapDotNet.Packets.Packet packet)
        {
            try
            {
                if (_maxSize > 0)
                {
                    if (DataSize > _maxSize)
                    {
                        return;
                    }
                }

                IpV4Datagram ip = packet.Ethernet.IpV4;
                TcpDatagram tcp = ip.Tcp;

                if (tcp.IsFin == true)
                {
                    HasFin = true;
                }

                if (tcp.HeaderLength > tcp.Length)
                {
                    return;
                }

                // If the payload length is zero bail out
                ulong length = (ulong)(tcp.Length - tcp.HeaderLength);
                if (length == 0)
                {
                    return;
                }

                if (tcp.Http.IsRequest == true)
                {
                    IsHttp = true;
                }
                else if (tcp.Http.IsResponse == true)
                {
                    IsHttp = true;
                }

                if (TimestampFirstPacket == null)
                {
                    TimestampFirstPacket = packet.Timestamp;
                }

                TimestampLastPacket = packet.Timestamp;
                
                ReassembleTcp((ulong)tcp.SequenceNumber, 
                               length,
                               tcp.Payload.ToMemoryStream().ToArray(), 
                               (ulong)tcp.Payload.Length, 
                               tcp.IsSynchronize,
                               ip.Source.ToValue(), 
                               ip.Destination.ToValue(),
                               (uint)tcp.SourcePort, 
                               (uint)tcp.DestinationPort, 
                               packet.Timestamp);
            }
            catch (Exception ex )
            {
                //OnError(ex.Message);
            }
        }

        /// <summary>
        /// Writes the payload data to the database
        /// </summary>
        /// <param name="net_src"></param>
        /// <param name="net_dst"></param>
        /// <param name="srcport"></param>
        /// <param name="dstport"></param>
        /// <param name="index"></param>
        /// <param name="data"></param>
        /// <param name="timestamp"></param>
        private void SavePacketData(long net_src, 
                                    long net_dst, 
                                    uint srcport, 
                                    uint dstport, 
                                    int index, 
                                    byte[] data, 
                                    DateTime timestamp)
        {
            try
            {
                //byte[] temp = woanware.Text.ReplaceNulls(data);

                // Ignore empty packets
                if (data.Length == 0)
                {
                    return;
                }

                if (data.Length == 1)
                {
                    if (data[0] == 0)
                    {
                        return;
                    }
                }

                bool isOutBound = false;
                if (index == 0)
                {
                    isOutBound = true;
                }

                DataSize += data.Length;

                Functions.WriteToFileStream(_storageHex, data);

                if (_lastPacketOutbound != isOutBound)
                {
                    //Functions.WriteToFileStream(_storageHex, "\r\n");
                }

                // Remove unprintable characters
                Regex rgx = new Regex(@"[^\u0000-\u007F]");
                string presanitised = woanware.Text.ByteArrayToString((byte[])data, woanware.Text.EncodingType.Ascii);
                string sanitised = rgx.Replace(woanware.Text.ReplaceNulls(presanitised), ".");

                if (IsHttp == true)
                {
                    if (IsGzipped == false)
                    {
                        Match match = _regexGzip.Match(sanitised);
                        IsGzipped = match.Success;
                    }

                    if (IsChunked == false)
                    {
                        Match match = _regexChunked.Match(sanitised);
                        IsChunked = match.Success;
                    }

                    if (_autoHttp == true)
                    {
                        if (_haveHttpHost == false)
                        {
                            Match match = _regexHost.Match(sanitised);
                            if (match.Success == true)
                            {
                                _haveHttpHost = true;
                                HttpHost = match.Groups[1].Value.Trim();
                                if (HttpHost.Contains("woan"))
                                {

                                }
                            }
                        }

                        Match matchMethod = _regexMethod.Match(sanitised);
                        if (matchMethod.Success == true)
                        {
                            if (_httpMethods.Contains(matchMethod.Groups[1].Value) == false)
                            {
                                _httpMethods.Add(matchMethod.Groups[1].Value);
                            }

                            if (matchMethod.Groups[2].Value.Trim().Length > 0)
                            {
                                Functions.WriteToFileStream(_storageInfo, "URL: " + matchMethod.Groups[2].Value + Environment.NewLine);
                            }
                        }

                        MatchCollection matchesLink = _regexLink.Matches(sanitised);
                        if (matchesLink.Count > 0)
                        {
                            foreach (Match match in matchesLink)
                            {
                                Functions.WriteToFileStream(_storageInfo, "LINK: " + match.Groups[1].Value + Environment.NewLine);

                            }
                        }
                    }
                }

                // HTML
                StringBuilder html = new StringBuilder();
                if (isOutBound == true)
                {
                    html.Append("<font color=\"#006600\" size=\"2\">");
                }
                else
                {
                    html.Append("<font color=\"#FF0000\" size=\"2\">");
                }

                if (_lastPacketOutbound != isOutBound)
                {
                    //if (sanitised.EndsWith("\r\n\r\n") == true)
                    //{
                    //    sanitised = sanitised.Substring(0, sanitised.Length - 4);
                    //}

                    sanitised = sanitised.Trim();

                    Functions.WriteToFileStream(_storageHtml, @"<br>");
                    _lastPacketOutbound = isOutBound;
                }

                string tempHtml = HttpUtility.HtmlEncode(sanitised);
                if (tempHtml.EndsWith("\r\n\r\n") == true)
                {
                    tempHtml = tempHtml.Substring(0, tempHtml.Length - 4);
                }

                tempHtml = tempHtml.Replace("\r\n", "<br>");
                html.Append(tempHtml);
                html.Append(@"</font>");
                Functions.WriteToFileStream(_storageHtml, html.ToString());
            }
            catch (Exception ex) 
            {
               // OnError(ex.Message);
            }
        }

        /// <summary>
        /// Reconstructs the tcp session
        /// </summary>
        /// <param name="sequence">Sequence number of the tcp packet</param>
        /// <param name="length">The size of the original packet data</param>
        /// <param name="data">The captured data</param>
        /// <param name="data_length">The length of the captured data</param>
        /// <param name="synflag"></param>
        /// <param name="net_src">The source ip address</param>
        /// <param name="net_dst">The destination ip address</param>
        /// <param name="srcport">The source port</param>
        /// <param name="dstport">The destination port</param>
        /// <param name="timestamp">Packet timestamp</param>
        private void ReassembleTcp(ulong sequence, 
                                   ulong length, 
                                   byte[] data,
                                   ulong data_length, 
                                   bool synflag, 
                                   long net_src,
                                   long net_dst, 
                                   uint srcport, 
                                   uint dstport, 
                                   DateTime timestamp)
        {
            try
            {
                long srcx, dstx;
                int src_index, j;
                bool first = false;
                ulong newseq;
                tcp_frag tmp_frag;

                src_index = -1;

                /* Now check if the packet is for this connection. */
                srcx = net_src;
                dstx = net_dst;

                /* Check to see if we have seen this source IP and port before.
                (Yes, we have to check both source IP and port; the connection
                might be between two different ports on the same machine.) */
                for (j = 0; j < 2; j++)
                {
                    if (_srcAddr[j] == srcx && _srcPort[j] == srcport)
                    {
                        src_index = j;
                        break;
                    }
                }
                /* we didn't find it if src_index == -1 */
                if (src_index < 0)
                {
                    /* assign it to a src_index and get going */
                    for (j = 0; j < 2; j++)
                    {
                        if (_srcPort[j] == 0)
                        {
                            _srcAddr[j] = srcx;
                            _srcPort[j] = srcport;
                            src_index = j;
                            first = true;
                            break;
                        }
                    }
                }
                if (src_index < 0)
                {
                    throw new Exception("Too many addresses!");
                }

                /* Before adding data for this flow to the data_out_file, check whether
                 * this frame acks fragments that were already seen. This happens when
                 * frames are not in the capture file, but were actually seen by the 
                 * receiving host (Fixes bug 592).
                 */
                if (_frags[1 - src_index] != null)
                {
                    while (CheckFragments(net_src,
                                         net_dst,
                                         srcport,
                                         dstport,
                                         1 - src_index,
                                         timestamp));
                }

                /* now that we have filed away the srcs, lets get the sequence number stuff figured out */
                if (first)
                {
                    /* this is the first time we have seen this src's sequence number */
                    _seq[src_index] = sequence + length;
                    if (synflag)
                    {
                        _seq[src_index]++;
                    }

                    SavePacketData(net_src, 
                                   net_dst, 
                                   srcport, 
                                   dstport, 
                                   src_index, 
                                   data, 
                                   timestamp);
                    return;
                }
                /* if we are here, we have already seen this src, let's
                try and figure out if this packet is in the right place */
                if (sequence < _seq[src_index])
                {
                    /* this sequence number seems dated, but
                    check the end to make sure it has no more
                    info than we have already seen */
                    newseq = sequence + length;
                    if (newseq > _seq[src_index])
                    {
                        ulong new_len;

                        /* this one has more than we have seen. let's get the
                        payload that we have not seen. */

                        new_len = _seq[src_index] - sequence;

                        if (data_length <= new_len)
                        {
                            data = null;
                            data_length = 0;
                        }
                        else
                        {
                            data_length -= new_len;
                            byte[] tmpData = new byte[data_length];
                            for (ulong i = 0; i < data_length; i++)
                            {
                                tmpData[i] = data[i + new_len];
                            }

                            data = tmpData;
                        }
                        sequence = _seq[src_index];
                        length = newseq - _seq[src_index];

                        /* this will now appear to be right on time :) */
                    }
                }
                if (sequence == _seq[src_index])
                {
                    /* right on time */
                    _seq[src_index] += length;
                    if (synflag) _seq[src_index]++;
                    if (data != null)
                    {
                        SavePacketData(net_src, 
                                       net_dst, 
                                       srcport, 
                                       dstport, 
                                       src_index, 
                                       data, 
                                       timestamp);
                    }
                    /* done with the packet, see if it caused a fragment to fit */
                    while (CheckFragments(net_src, 
                                          net_dst, 
                                          srcport, 
                                          dstport, 
                                          src_index, 
                                          timestamp));
                }
                else
                {
                    /* out of order packet */
                    if (data_length > 0 && sequence > _seq[src_index])
                    {
                        tmp_frag = new tcp_frag();
                        tmp_frag.data = data;
                        tmp_frag.seq = sequence;
                        tmp_frag.len = length;
                        tmp_frag.data_len = data_length;

                        if (_frags[src_index] != null)
                        {
                            tmp_frag.next = _frags[src_index];
                        }
                        else
                        {
                            tmp_frag.next = null;
                        }

                        _frags[src_index] = tmp_frag;
                    }
                }
            }
            catch (Exception ex)
            {
                //OnError(ex.Message);
            }
        } 

        /// <summary>
        /// Search through all the frag we have collected to see if one fits 
        /// </summary>
        /// <param name="net_src"></param>
        /// <param name="net_dst"></param>
        /// <param name="srcport"></param>
        /// <param name="dstport"></param>
        /// <param name="index"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        private bool CheckFragments(long net_src,
                                    long net_dst,
                                    uint srcport,
                                    uint dstport, 
                                    int index,
                                    DateTime timestamp)
        {
            try
            {
                tcp_frag prev = null;
                tcp_frag current;
                ulong lowest_seq;
                current = _frags[index];
                if (current != null)
                {
                    lowest_seq = current.seq;
                    while (current != null)
                    {
                        if ((lowest_seq - current.seq) > 0)
                        {
                            lowest_seq = current.seq;
                        }

                        if (current.seq < _seq[index])
                        {
                            ulong newseq;
                            /* this sequence number seems dated, but
                               check the end to make sure it has no more
                               info than we have already seen */
                            newseq = current.seq + current.len;
                            if (newseq > _seq[index])
                            {
                                ulong new_pos;

                                /* this one has more than we have seen. let's get the
                                 payload that we have not seen. This happens when 
                                 part of this frame has been retransmitted */

                                new_pos = _seq[index] - current.seq;

                                if (current.data_len > new_pos)
                                {
                                    //sc->dlen = current.data_len - new_pos;

                                    SavePacketData(net_src,
                                                 net_dst,
                                                 srcport,
                                                 dstport,
                                                 index,
                                                 current.data,
                                                 timestamp);
                                }

                                _seq[index] += (current.len - new_pos);
                                if (prev != null)
                                {
                                    prev.next = current.next;
                                }
                                else
                                {
                                    _frags[index] = current.next;
                                }
                                return true;
                            }
                        }


                        if (current.seq == _seq[index])
                        {
                            /* this fragment fits the stream */
                            if (current.data != null)
                            {
                                SavePacketData(net_src,
                                               net_dst,
                                               srcport,
                                               dstport,
                                               index,
                                               current.data,
                                               timestamp);
                            }

                            _seq[index] += current.len;

                            if (prev != null)
                            {
                                prev.next = current.next;
                            }
                            else
                            {
                                _frags[index] = current.next;
                            }

                            current.data = null;
                            current = null;
                            return true;
                        }
                        prev = current;
                        current = current.next;
                    }
                }
                
                return false;
            }
            catch (Exception ex) 
            {
                //OnError(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Cleans the linked list
        /// </summary>
        private void ResetTcpReassembly()
        {
            try
            {
                tcp_frag current, next;

                for (int i = 0; i < 2; i++)
                {
                    _seq[i] = 0;
                    _srcAddr[i] = 0;
                    _srcPort[i] = 0;
                    _tcpPort[i] = 0;
                    current = _frags[i];

                    while (current != null)
                    {
                        next = current.next;
                        current.data = null;
                        current = null;
                        current = next;
                    }

                    _frags[i] = null;
                }
            }
            catch (Exception ex) 
            {
                //OnError(ex.Message);
            }
        }

        #region Properties
        /// <summary>
        /// 
        /// </summary>
        public string HttpMethods
        {
            get
            {
                _httpMethods.Sort();
                return string.Join(",", _httpMethods);
            }
        }
        #endregion

        #region Event Methods
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="message"></param>
        //private void OnExclamation(string message)
        //{
        //    var handler = Exclamation;
        //    if (handler != null)
        //    {
        //        handler(message);
        //    }
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="error"></param>
        //private void OnError(string error)
        //{
        //    var handler = Error;
        //    if (handler != null)
        //    {
        //        handler(error);
        //    }
        //}
        #endregion
    }
}
