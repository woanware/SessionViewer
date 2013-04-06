using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;

namespace SessionViewer
{
    /// <summary>
    /// Holds the connection information of the TCP session
    /// </summary>
    internal class Connection
    {
        #region Member/Properties Variables
        public uint SourceIpNumeric { get; private set; }
        public string SourceIp { get; private set; }
        public ushort SourcePort { get; private set; }
        public uint DestinationIpNumeric { get; private set; }
        public string DestinationIp { get; private set; }
        public ushort DestinationPort { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="packet"></param>
        public Connection(PcapDotNet.Packets.Packet packet)
        {
            IpV4Datagram ip = packet.Ethernet.IpV4;
            TcpDatagram tcp = ip.Tcp;

            SourceIp = ip.Source.ToString();
            SourceIpNumeric = ip.Source.ToValue();
            DestinationIp = ip.Destination.ToString();
            DestinationIpNumeric = ip.Destination.ToValue();
            SourcePort = (ushort)tcp.SourcePort;
            DestinationPort = (ushort)tcp.DestinationPort;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Overrided in order to catch both sides of the connection 
        /// with the same connection object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if ((obj is Connection) == false)
            {
                return false;
            }
                
            Connection connection = (Connection)obj;

            bool result = ((connection.SourceIp.Equals(SourceIp)) &&
                           (connection.SourcePort == SourcePort) &&
                           (connection.DestinationIp.Equals(DestinationIp)) &&
                           (connection.DestinationPort == DestinationPort)) ||
                          ((connection.SourceIp.Equals(DestinationIp)) &&
                           (connection.SourcePort == DestinationPort) &&
                           (connection.DestinationIp.Equals(SourceIp)) &&
                           (connection.DestinationPort == SourcePort));

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ((SourceIp.GetHashCode() ^ SourcePort.GetHashCode()) as object).GetHashCode() ^
                   ((DestinationIp.GetHashCode() ^ DestinationPort.GetHashCode()) as object).GetHashCode();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return string.Format(@"{0}.{1}-{2}.{3}", SourceIp, SourcePort, DestinationIp, DestinationPort);
        }
        #endregion
    }
}
