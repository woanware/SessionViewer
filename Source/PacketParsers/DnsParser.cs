using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using System;
using System.Collections.Generic;
using System.IO;

namespace SessionViewer.PacketProcessors
{
    /// <summary>
    /// 
    /// </summary>
    public class DnsParser : InterfacePacketParser
    {
        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public DnsParser()
        {
            Enabled = true;
        }
        #endregion

        #region Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="outputPath"></param>
        /// <param name="packet"></param>
        public void Process(FileStream fileStream, 
                            string outputPath, 
                            IpV4Datagram packet)
        {
            UdpDatagram udp = packet.Udp;
            if (udp.Dns != null)
            {
                List<string> queries = new List<string>();
                foreach (PcapDotNet.Packets.Dns.DnsQueryResourceRecord resourceRecord in udp.Dns.Queries)
                {
                    try
                    {
                        if (queries.Contains(resourceRecord.ToString()) == false)
                        {
                            queries.Add(resourceRecord.ToString());
                            woanware.IO.WriteToFileStream(fileStream, "DNS Query: " + resourceRecord.ToString() + Environment.NewLine);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Log().Error(ex.ToString());
                    }
                }

                List<string> answers = new List<string>();
                foreach (PcapDotNet.Packets.Dns.DnsDataResourceRecord resourceRecord in udp.Dns.Answers)
                {
                    try
                    {
                        PcapDotNet.Packets.Dns.DnsResourceData dataRecord = resourceRecord.Data;

                        if (dataRecord.GetType() != typeof(PcapDotNet.Packets.Dns.DnsResourceDataDomainName))
                        {
                            continue;
                        }

                        PcapDotNet.Packets.Dns.DnsResourceDataDomainName ab = (PcapDotNet.Packets.Dns.DnsResourceDataDomainName)dataRecord;

                        if (answers.Contains(resourceRecord.DnsType.ToString() + " " + ab.Data.ToString()) == false)
                        {
                            answers.Add(resourceRecord.ToString());
                            woanware.IO.WriteToFileStream(fileStream, "DNS Answer: " + resourceRecord.DnsType.ToString() + " " + ab.Data.ToString() + Environment.NewLine);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Log().Error(ex.ToString());
                    }
                }
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get
            {
                return "DNS";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ushort Port
        {
            get
            {
                return 53;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ParserType Type
        {
            get
            {
                return ParserType.Packet;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Protocol
        {
            get
            {
                return "UDP";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Enabled { get; set; }
        #endregion
    }
}
