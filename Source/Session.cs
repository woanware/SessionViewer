using System;
using System.Net;

namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    [NPoco.TableName("Sessions")]
    [NPoco.PrimaryKey("Id")]
    public class Session
    {
        #region Member Variables
        public string Guid { get; set; }
        public string HttpHost { get; set; }
        public long Id { get; set; }
        public long DataSize { get; set; }
        public DateTime? TimestampFirstPacket { get; set; }
        public DateTime? TimestampLastPacket { get; set; }
        public uint SourceIp { get; set; }
        public uint DestinationIp { get; set; }
        public int SourcePort { get; set; }
        public int DestinationPort { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public Session(){}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="guid"></param>
        public Session(string guid)
        {
            Guid = guid;
            HttpHost = string.Empty;
        }
        #endregion

        #region Properties
        /// <summary>
        /// 
        /// </summary>
        [NPoco.Ignore]
        public IPAddress SrcIp
        {
            get
            {
                long temp = (UInt32)IPAddress.HostToNetworkOrder((Int32)SourceIp);
                return new IPAddress(temp);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [NPoco.Ignore]
        public string SrcIpText
        {
            get
            {
                long temp = (UInt32)IPAddress.HostToNetworkOrder((Int32)SourceIp);
                return new IPAddress(temp).ToString();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [NPoco.Ignore]
        public IPAddress DstIp
        {
            get
            {
                long temp = (UInt32)IPAddress.HostToNetworkOrder((Int32)DestinationIp);
                return new IPAddress(temp);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [NPoco.Ignore]
        public string DstIpText
        {
            get
            {
                long temp = (UInt32)IPAddress.HostToNetworkOrder((Int32)DestinationIp);
                return new IPAddress(temp).ToString();
            }
        }
        #endregion
    }
}
