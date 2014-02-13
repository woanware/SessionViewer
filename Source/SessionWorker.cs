using HttpKit;
using SessionViewer.PacketProcessors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SessionViewer
{
    class SessionWorker
    {
        public event woanware.Events.MessageEvent CompleteEvent;

        public int Id { get; private set; }
        private CancellationTokenSource cancelSource = null;
        private BlockingCollection<SessionTask> blockingCollection;
        private bool processed = false;
        private bool processing = false;
        private List<InterfaceSessionParser> parsers;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="blockingCollection"></param>
        public SessionWorker(int id, BlockingCollection<SessionTask> blockingCollection)
        {
            this.Id = id;
            this.blockingCollection = blockingCollection;

            this.parsers = new List<InterfaceSessionParser>();
            //this.parsers.Add(new DnsParser());
            this.parsers.Add(new SessionViewer.SessionParsers.HttpParser());
        }

        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            Task task = Task.Factory.StartNew(() =>
            {
                try
                {
                    cancelSource = new CancellationTokenSource();

                    foreach (SessionTask sessionTask in this.blockingCollection.GetConsumingEnumerable(cancelSource.Token))
                    {
                        try
                        {
                            this.processing = true;

                            //Console.WriteLine("Processing: (" +this.Id + ") " + System.IO.Path.Combine(sessionTask.OutputPath, sessionTask.Session.Guid.Substring(0, 2), sessionTask.Session.Guid + ".bin"));
                            var parser = (from p in this.parsers where sessionTask.Parser.ToLower() == sessionTask.Parser select p).SingleOrDefault();
                            if (parser == null)
                            {
                                continue;
                            }

                            parser.Process(sessionTask.OutputPath, sessionTask.Session);
                        }
                        finally
                        {
                            if (this.processed == true & this.blockingCollection.Count == 0)
                            {
                                Thread.Sleep(new TimeSpan(0, 0, 5));
                                this.cancelSource.Cancel();
                            }

                            this.processing = false;
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    //System.Console.WriteLine(ex.ToString());
                }
                finally
                {
                    OnComplete(Id.ToString());
                }

            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            this.cancelSource.Cancel();
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetProcessed()
        {
            this.processed = true;

            if (this.processing == false & this.blockingCollection.Count == 0)
            {
                Stop();
            }
        }

        #region Event Methods
        /// <summary>
        /// 
        /// </summary>
        private void OnComplete(string id)
        {
            var handler = CompleteEvent;
            if (handler != null)
            {
                handler(id);
            }
        }
        #endregion
    }
}
