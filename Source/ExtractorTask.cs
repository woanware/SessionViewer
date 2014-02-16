namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    public class ExtractorTask
    {
        #region Member Variables
        public Session Session { get; set; }
        public string DataDir { get; set; }
        public string OutputDir { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="dataDir"></param>
        /// <param name="outputDir"></param>
        public ExtractorTask(Session session,
                            string dataDir,
                            string outputDir)
        {
            this.Session = session;
            this.DataDir = dataDir;
            this.OutputDir = outputDir;
        }
        #endregion
    }
}
