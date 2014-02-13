namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    public class SessionTask
    {
        #region Member Variables
        public string Parser { get; set; }
        public Session Session { get; set; }
        public string OutputPath { get; set; }
        public int Pk { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="session"></param>
        /// <param name="outputPath"></param>
        /// <param name="pk"></param>
        public SessionTask(string parser,
                           Session session,
                           string outputPath, 
                           int pk)
        {
            this.Parser = parser;
            this.Session = session;
            this.OutputPath = outputPath;
            this.Pk = pk;
        }
        #endregion
    }
}
