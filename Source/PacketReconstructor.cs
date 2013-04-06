﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using PcapDotNet.Packets.Http;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;

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
        public event Global.MessageEvent Exclamation;
        public event Global.MessageEvent Error;
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
        private System.IO.FileStream _dataFileHex = null;
        private System.IO.FileStream _dataFileHtml = null;
        private System.IO.FileStream _dataFileText = null;
        private bool _lastPacketOutbound = true;
        public long DataSize { get; private set; }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputPath"></param>
        public PacketReconstructor(string outputPath)
        {
            this.Guid = System.Guid.NewGuid().ToString();
            ResetTcpReassembly();
            _dataFileHex = new System.IO.FileStream(Path.Combine(outputPath, Guid + ".bin"), System.IO.FileMode.Create);
            _dataFileText = new System.IO.FileStream(Path.Combine(outputPath, Guid + ".txt"), System.IO.FileMode.Create);
            _dataFileHtml = new System.IO.FileStream(Path.Combine(outputPath, Guid + ".html"), System.IO.FileMode.Create);

            byte[] html = ASCIIEncoding.ASCII.GetBytes(Global.HTML_HEADER);
            _dataFileHtml.Write(html, 0, html.Length);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _dataFileHex.Close();
            _dataFileHex.Dispose();
            _dataFileText.Close();
            _dataFileText.Dispose();

            byte[] html = ASCIIEncoding.ASCII.GetBytes(Global.HTML_FOOTER);
            _dataFileHtml.Write(html, 0, html.Length);
            _dataFileHtml.Close();
            _dataFileHtml.Dispose();
        }

        /// <summary>
        /// The main function of the class receives a tcp packet and reconstructs the stream
        /// </summary>
        /// <param name="tcpPacket"></param>
        public void ReassemblePacket(PcapDotNet.Packets.Packet packet)
        {
            try
            {
                IpV4Datagram ip = packet.Ethernet.IpV4;
                TcpDatagram tcp = ip.Tcp;
                
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
                OnError(ex.Message);
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
                byte[] temp = woanware.Text.ReplaceNulls(data);

                // Ignore empty packets
                if (temp.Length == 0)
                {
                    return;
                }

                if (temp.Length == 1)
                {
                    if (temp[0] == 0)
                    {
                        return;
                    }
                }

                bool isOutBound = false;
                if (index == 0)
                {
                    isOutBound = true;
                }

                DataSize += temp.Length; 

                // Hex
                _dataFileHex.Write(temp, 0, temp.Length);

                // Remove unprintable characters
                Regex rgx = new Regex(@"[^\u0000-\u007F]");
                string presanitised = woanware.Text.ByteArrayToString((byte[])temp, woanware.Text.EncodingType.Ascii);
                string sanitised = rgx.Replace(woanware.Text.ReplaceNulls(presanitised), ".");

                // Text
                byte[] textTemp = ASCIIEncoding.ASCII.GetBytes(sanitised);
                _dataFileText.Write(textTemp, 0, textTemp.Length);

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
                    html.Append(@"<br>");
                    _lastPacketOutbound = isOutBound;
                }

                string tempHtml = HttpUtility.HtmlEncode(sanitised);
                tempHtml = tempHtml.Replace("\r\n", "<br>");
                html.Append(tempHtml);
                html.Append(@"</font>");

                byte[] htmlTemp = ASCIIEncoding.ASCII.GetBytes(html.ToString());
                _dataFileHtml.Write(htmlTemp, 0, htmlTemp.Length);
            }
            catch (Exception ex) 
            {
                OnError(ex.Message);
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
                OnError(ex.Message);
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
                current = _frags[index];
                while (current != null)
                {
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
                return false;
            }
            catch (Exception ex) 
            {
                OnError(ex.Message);
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
                OnError(ex.Message);
            }
        }

        #region Event Methods
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
