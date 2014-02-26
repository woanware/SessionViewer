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
using Trinet.Core.IO.Ntfs;
using Extractors;

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
                            string path = System.IO.Path.Combine(this.dataDirectory,
                                                                 session.Guid.Substring(0, 2),
                                                                 session.Guid + ".bin");

                            if (File.Exists(path) == false)
                            {
                                continue;
                            }

                            FileInfo fileInfo = new FileInfo(path);

                            // Info
                            if (fileInfo.AlternateDataStreamExists("info") == false)
                            {
                                continue;
                            }

                            ProcessUrls(session, fileInfo);
                        }
                        catch (Exception) { }
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
                               string dataDirectory,
                               string outputDirectory)
        {
            this.Id = id;
            this.blockingCollection = blockingCollection;
            this.dataDirectory = dataDirectory;
            this.outputDirectory = outputDirectory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataDirectory"></param>
        /// <param name="outputDirectory"></param>
        public void PreProcess(string dataDirectory, string outputDirectory)
        {
            IO.WriteTextToFile("\"URL\",\"Src IP\",\"Src Port\",\"Dst IP\",\"Dst Port\"" + Environment.NewLine, System.IO.Path.Combine(outputDirectory, "Urls.csv"), false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataDirectory"></param>
        /// <param name="outputDirectory"></param>
        public void PostProcess(string dataDirectory, string outputDirectory)
        {
            CsvConfiguration csvConfiguration = new CsvConfiguration();
            csvConfiguration.QuoteAllFields = true;

            using (FileStream fileStream = new FileStream(System.IO.Path.Combine(outputDirectory, "Urls.csv"), FileMode.Append, FileAccess.Write, FileShare.Read))
            using (StreamWriter streamWriter = new StreamWriter(fileStream))
            using (CsvHelper.CsvWriter csvWriter = new CsvHelper.CsvWriter(streamWriter, csvConfiguration))
            {
                foreach (string file in System.IO.Directory.EnumerateFiles(outputDirectory,
                                                                           "*.xml",
                                                                           SearchOption.AllDirectories))
                {
                    string fileName = System.IO.Path.GetFileName(file);
                    if (fileName.StartsWith("Url.Details.") == false)
                    {
                        continue;
                    }

                    UrlDetails urlDetails = new UrlDetails();
                    string ret = urlDetails.Load(file);
                    if (ret.Length == 0)
                    {
                        foreach (string url in urlDetails.Urls)
                        {
                            csvWriter.WriteField(url);
                            csvWriter.WriteField(urlDetails.SrcIp);
                            csvWriter.WriteField(urlDetails.SrcPort);
                            csvWriter.WriteField(urlDetails.DstIp);
                            csvWriter.WriteField(urlDetails.DstPort);
                            csvWriter.NextRecord();
                        }
                    }
                }
            }
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
        /// <param name="session"></param>
        /// <param name="fileInfo"></param>
        private void ProcessUrls(Session session, FileInfo fileInfo)
        {
            AlternateDataStreamInfo ads = fileInfo.GetAlternateDataStream("info", FileMode.Open);
            using (TextReader reader = ads.OpenText())
            {
                UrlDetails urlDetails = new UrlDetails();
                urlDetails.SrcIp = session.SrcIpText;
                urlDetails.SrcPort = session.SourcePort;
                urlDetails.DstIp = session.DstIpText;
                urlDetails.DstPort = session.DestinationPort;

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("LINK: ") == false)
                    {
                        continue;
                    }

                    string url = line.Substring(6);

                    if (urlDetails.Urls.Contains(session.HttpHost + url) == false)
                    {
                        urlDetails.Urls.Add(session.HttpHost + url);
                    }
                }

                if (urlDetails.Urls.Count > 0)
                {
                    string dir = session.SrcIpText + "." + session.SourcePort + "-" + session.DstIpText + "." + session.DestinationPort;
                    if (System.IO.Directory.Exists(System.IO.Path.Combine(this.outputDirectory, dir)) == false)
                    {
                        IO.CreateDirectory(System.IO.Path.Combine(this.outputDirectory, dir));
                    }

                    urlDetails.Save(System.IO.Path.Combine(this.outputDirectory, dir, "Url.Details." + session.Guid + ".xml"));
                }
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
