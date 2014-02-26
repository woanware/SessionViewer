using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    public class SessionParser
    {
        #region Events
        public event woanware.Events.DefaultEvent CompleteEvent;
        public event woanware.Events.MessageEvent MessageEvent;
        #endregion

        #region Member Variables
        private BlockingCollection<SessionTask> blockingCollection;
        private List<SessionWorker> workers;
        private int maxThreads = 1;
        private readonly object locked = new object();
        private long totalSessions = 0;
        #endregion

        #region Contructor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxThreads"></param>
        public SessionParser(int maxThreads)
        {
            this.maxThreads = maxThreads;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionTask"></param>
        public void Add(SessionTask sessionTask)
        {
            totalSessions++;
            this.blockingCollection.Add(sessionTask);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="batchCheck"></param>
        public void Start()
        {
            this.blockingCollection = new BlockingCollection<SessionTask>();
            this.workers = new List<SessionWorker>(maxThreads);
            for (int index = 0; index < maxThreads; index++)
            {
                SessionWorker sw = new SessionWorker(index + 1, this.blockingCollection);
                sw.Start();
                sw.CompleteEvent += Worker_CompleteEvent;

                this.workers.Add(sw);
            }
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
            Console.WriteLine("ALL PROCESSED");
        }
        #endregion

        #region Worker Event Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void Worker_CompleteEvent(string message)
        {
            lock (this.locked)
            {
                Console.WriteLine("WORKER REMOVE: " + message);
                var worker = workers.Single(w => w.Id.ToString() == message);
                worker.CompleteEvent -= Worker_CompleteEvent;
                this.workers.Remove(worker);
            }

            if (this.workers.Count == 0)
            {
                Console.WriteLine("PARSER EXIT");
                OnComplete();
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// 
        /// </summary>
        public long QueueCount
        {
            get
            {
                return this.blockingCollection.Count;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public long TotalSessions
        {
            get
            {
                return this.totalSessions;
            }
        }
        #endregion

        #region Event Methods
        /// <summary>
        /// 
        /// </summary>
        private void OnComplete()
        {
            var handler = CompleteEvent;
            if (handler != null)
            {
                handler();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnMessage(string message)
        {
            var handler = MessageEvent;
            if (handler != null)
            {
                handler(message);
            }
        }
        #endregion
    }
}
