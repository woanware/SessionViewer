namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    public enum ParserType
    {
        Packet = 0,
        Session = 1
    }

    /// <summary>
    /// 
    /// </summary>
    public interface InterfaceParser
    {
        #region Properties
        string Name { get; }
        ushort Port { get; }
        ParserType Type { get; }
        string Protocol { get; }
        bool Enabled { get; set; }
        #endregion
    }
}
