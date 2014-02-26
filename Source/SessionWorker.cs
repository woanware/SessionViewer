using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SessionViewer
{
    internal class SessionWorker
    {
        public event woanware.Events.MessageEvent CompleteEvent;

        public int Id { get; private set; }
        private CancellationTokenSource cancelSource = null;
        private BlockingCollection<SessionTask> blockingCollection;
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
                            var parser = (from p in this.parsers where sessionTask.Parser.ToLower() == sessionTask.Parser.ToLower() select p).SingleOrDefault();
                            if (parser == null)
                            {
                                continue;
                            }

                            parser.Process(sessionTask.OutputPath, sessionTask.Session);
                        }
                        catch (Exception){}
                    }

                    Console.WriteLine("WORKER EXIT");
                }
                catch (OperationCanceledException) 
                {
                    Console.WriteLine("WORKER CANCELL");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("WORKER ERORR" + ex.ToString());
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
