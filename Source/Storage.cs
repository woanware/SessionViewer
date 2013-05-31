using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    [ProtoContract]
    public class Storage
    {
        [ProtoMember(1)]
        public List<byte> Hex { get; set; }
        [ProtoMember(2)]
        public string Ascii { get; set; }
        [ProtoMember(3)]
        public string Html { get; set; }
        [ProtoMember(4)]
        public string Gzip { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Storage()
        {
            Hex = new List<byte>();
            Ascii = string.Empty;
            Html = string.Empty;
            Gzip = string.Empty;
        }
    }
}
