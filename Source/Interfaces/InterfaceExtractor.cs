using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    public interface InterfaceExtractor
    {
        #region Events
        event woanware.Events.MessageEvent CompleteEvent;
        event woanware.Events.MessageEvent ErrorEvent;
        event woanware.Events.MessageEvent WarningEvent;
        event woanware.Events.MessageEvent MessageEvent;
        #endregion

        #region Methods
        void Initialise(int id, BlockingCollection<Session> blockingCollection, string dataDirectory, string outputDirectory);
        void PreProcess(string dataDirectory, string outputDirectory);
        void PostProcess(string dataDirectory, string outputDirectory);
        void Start();
        void Stop();
        void SetProcessed();
        #endregion

        #region Properties
        int Id { get; }
        string Name { get; }
        string Category { get; }
        #endregion
    }
}
