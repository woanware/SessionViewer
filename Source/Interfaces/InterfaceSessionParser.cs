namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    public interface InterfaceSessionParser : InterfaceParser
    {
        #region Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputPath">Path to the output data e.g. .bin files</param>
        /// <param name="session">Session object that holds the key data</param>
        void Process(string outputPath, Session session);
        #endregion

        #region Properties
        //bool WillProcessHtml;
        #endregion
    }
}
