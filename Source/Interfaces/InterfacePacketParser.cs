using System.IO;
using PcapDotNet.Packets.IpV4;

namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    public interface InterfacePacketParser : InterfaceParser
    {
        #region Methods
        void Process(FileStream fileStream, string outputPath, IpV4Datagram packet);
        #endregion
    }
}
