using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionViewer
{
    public class Extractor
    {
        #region Events
        public event woanware.Events.DefaultEvent CompleteEvent;
        #endregion

        #region Member Variables
        private InterfaceExtractor interfaceExtractor;
        private BlockingCollection<Session> blockingCollection;
        private List<InterfaceExtractor> workers;
        private int maxThreads = 4;
        private string outputDirectory;
        private string dataDirectory;
        private readonly object _lock = new object();
        #endregion

        #region Contructor
        /// <summary>
        /// 
        /// </summary>
        public Extractor(){}
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxThreads"></param>
        /// <param name="interfaceExtractor"></param>
        /// <param name="dataDirectory"></param>
        /// <param name="outputDirectory"></param>
        public void Initialise(int maxThreads,
                               InterfaceExtractor interfaceExtractor,
                               string dataDirectory,
                               string outputDirectory)
        {
            this.maxThreads = maxThreads;
            this.interfaceExtractor = interfaceExtractor;
            this.dataDirectory = dataDirectory;
            this.outputDirectory = outputDirectory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="batchCheck"></param>
        public void Start()
        {
            this.blockingCollection = new BlockingCollection<Session>();

            var type = this.interfaceExtractor.GetType();
            var preProcess = (InterfaceExtractor)Activator.CreateInstance(type);
            preProcess.PreProcess(this.dataDirectory, this.outputDirectory);

            this.workers = new List<InterfaceExtractor>(maxThreads);
            for (int index = 0; index < maxThreads; index++)
            {
                var e = (InterfaceExtractor)Activator.CreateInstance(type);
                e.Initialise(index + 1, this.blockingCollection, this.dataDirectory, this.outputDirectory);
                e.Start();
                e.CompleteEvent += Worker_CompleteEvent;
                this.workers.Add(e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        public void Add(Session session)
        {
            this.blockingCollection.Add(session);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="batchCheck"></param>
        public void Stop()
        {
            for (int index = 0; index < this.workers.Count; index++)
            {
                this.workers[index].Stop();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="batchCheck"></param>
        public void SetProcessed()
        {
            this.blockingCollection.CompleteAdding();
            //for (int index = 0; index < this.workers.Count; index++)
            //{
            //    this.workers[index].SetProcessed();
            //}
        }
        #endregion

        #region Worker Event Methods
        /// <summary>
        /// 
        /// </summary>
        private void Worker_CompleteEvent(string id)
        {
            lock (_lock)
            {
                var worker = workers.Single(w => w.Id.ToString() == id);
                worker.CompleteEvent -= Worker_CompleteEvent;
                this.workers.Remove(worker);
            }

            if (this.workers.Count == 0)
            {
                var type = this.interfaceExtractor.GetType();
                var postProcess = (InterfaceExtractor)Activator.CreateInstance(type);
                postProcess.PostProcess(this.dataDirectory, this.outputDirectory);
                CompleteEvent();
            }
        }
        #endregion
    }
}
