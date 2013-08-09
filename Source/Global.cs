namespace SessionViewer
{
    /// <summary>
    /// A global place to store stuff!
    /// </summary>
    public class Global
    {
        #region Event Delegates
        public delegate void MessageEvent(string message);
        public delegate void DefaultEvent();
        #endregion

        #region Constants
        public const string HTML_HEADER = @"<html><body><FONT FACE=""courier"">";
        public const string HTML_FOOTER = @"</FONT></body></html>";
        public const string DB_FILE = "SessionViewer.sv";
        #endregion

        #region Public Enums
        /// <summary>
        /// 
        /// </summary>
        public enum FieldsCopy : int
        {
            SourceIp = 0,
            SourcePort = 1,
            DestinationIp = 2,
            DestinationPort = 3,
            Size = 4,
            TimestampFirstPacket = 5,
            TimestampLastPacket = 6,
            SourceCountry = 7,
            DestinationCountry = 8
        }
        #endregion
    }
}
