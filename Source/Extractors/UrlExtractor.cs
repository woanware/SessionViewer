using CsvHelper.Configuration;
using System.IO.Compression;
using MimeKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using woanware;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SessionViewer.SessionProcessors
{
    /// <summary>
    /// 
    /// </summary>
    internal class UrlExtractor : InterfaceExtractor
    {
        #region Events
        public event woanware.Events.MessageEvent CompleteEvent;
        public event woanware.Events.MessageEvent ErrorEvent;
        public event woanware.Events.MessageEvent WarningEvent;
        public event woanware.Events.MessageEvent MessageEvent;
        #endregion

        public int Id { get; private set; }
        private CancellationTokenSource cancelSource = null;
        private BlockingCollection<Session> blockingCollection;
        private string dataDirectory = string.Empty;
        private string outputDirectory = string.Empty;
        private bool processed = false;
        private bool processing = false;

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

                    foreach (Session session in this.blockingCollection.GetConsumingEnumerable(cancelSource.Token))
                    {
                        try
                        {
                            this.processing = true;

                            string path = System.IO.Path.Combine(this.dataDirectory,
                                                                 session.Guid.Substring(0, 2),
                                                                 session.Guid + ".bin");

                            if (File.Exists(path) == false)
                            {
                                continue;
                            }

                            //byte[] temp = File.ReadAllBytes(System.IO.Path.Combine(dataDirectory, session.Guid.Substring(0, 2), session.Guid + ".bin"));
                            //ProcessAttachments(session, temp);
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
        /// <param name="id"></param>
        /// <param name="blockingCollection"></param>
        /// <param name="outputDirectory"></param>
        /// <param name="dataDirectory"></param>
        public void Initialise(int id,
                               BlockingCollection<Session> blockingCollection,
                               string outputDirectory,
                               string dataDirectory)
        {
            this.Id = id;
            this.blockingCollection = blockingCollection;
            this.outputDirectory = outputDirectory;
            this.dataDirectory = dataDirectory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataDirectory"></param>
        /// <param name="outputDirectory"></param>
        public void PreProcess(string dataDirectory, string outputDirectory)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataDirectory"></param>
        /// <param name="outputDirectory"></param>
        public void PostProcess(string dataDirectory, string outputDirectory)
        {

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

        /// <summary>
        /// 
        /// </summary>
        private void OnError(string text)
        {
            var handler = ErrorEvent;
            if (handler != null)
            {
                handler(text);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnWarning(string text)
        {
            var handler = WarningEvent;
            if (handler != null)
            {
                handler(text);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnMessage(string text)
        {
            var handler = MessageEvent;
            if (handler != null)
            {
                handler(text);
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get
            {
                return "URL Extractor";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Category
        {
            get
            {
                return "HTTP";
            }
        }
        #endregion
    }
}
