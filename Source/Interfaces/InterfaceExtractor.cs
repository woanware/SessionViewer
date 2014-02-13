using System.Collections;
using System.Collections.Generic;

namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    public interface InterfaceExtractor
    {
        #region Events
        event woanware.Events.MessageEvent Complete;
        event woanware.Events.MessageEvent Error;
        event woanware.Events.MessageEvent Warning;
        event woanware.Events.MessageEvent Message;
        #endregion

        #region Methods
        void Run(ArrayList sessions, string dataDirectory, string outputDirectory);
        #endregion

        #region Properties
        string Name { get; }
        string Category { get; }
        #endregion
    }
}
