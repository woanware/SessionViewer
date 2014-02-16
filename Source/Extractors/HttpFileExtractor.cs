using CsvHelper.Configuration;
using Extractors;
using HttpKit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using woanware;

namespace SessionViewer.SessionProcessors
{
    /// <summary>
    /// 
    /// </summary>
    internal class FileSig
    {
        public string Extension { get; set; }
        public byte[] Sig { get; set; }
        public int OffsetSubHeader {get;set;}
        public byte[] SigSubHeader { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="sig"></param>
        public FileSig(string extension, byte[] sig)
        {
            this.Extension = extension;
            this.Sig = sig;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="sig"></param>
        /// <param name="offsetSubHeader"></param>
        /// <param name="sigSubHeader"></param>
        public FileSig(string extension, 
                       byte[] sig, 
                       int offsetSubHeader,
                       byte[] sigSubHeader) : this(extension, sig)
        {
            this.Extension = extension;
            this.Sig = sig;
            this.OffsetSubHeader = offsetSubHeader;
            this.SigSubHeader = sigSubHeader;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal class HttpFileExtractor : InterfaceExtractor
    {
        #region Win32 Imports
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int memcmp(byte[] b1, byte[] b2, long count);
        #endregion

        #region Events
        public event woanware.Events.MessageEvent CompleteEvent;
        public event woanware.Events.MessageEvent ErrorEvent;
        public event woanware.Events.MessageEvent WarningEvent;
        public event woanware.Events.MessageEvent MessageEvent;
        #endregion

        #region Member Variables
        public int Id { get; private set; }
        private CancellationTokenSource cancelSource = null;
        private string dataDirectory = string.Empty;
        private string outputDirectory = string.Empty;
        private BlockingCollection<Session> blockingCollection;
        private readonly object _lock = new object();
        private bool processed = false;
        private bool processing = false;
        private HttpKit.HttpParser parser;

        // List of the file sigs that we care about
        private List<FileSig> fileSigs = new List<FileSig>()
        {
            new FileSig("zip", new byte[] {0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06,0x00}),
            new FileSig("zip", new byte[] {0x50, 0x4B, 0x03, 0x04}),
            new FileSig("exe", new byte[] {0x4D, 0x5A}),
            new FileSig("gz", new byte[] {0x1F, 0x8B, 0x08}),
            new FileSig("pdf", new byte[] {0x25, 0x50, 0x44, 0x46}),
            new FileSig("7z", new byte[] {0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C}),
            new FileSig("doc", new byte[] {0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1}, 512, new byte[] {0xEC, 0xA5, 0xC1, 0x00}),
            new FileSig("xls", new byte[] {0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1}, 512, new byte[] {0xFD, 0xFF, 0xFF, 0xFF, 0x10}),
            new FileSig("xls", new byte[] {0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1}, 512, new byte[] {0xFD, 0xFF, 0xFF, 0xFF, 0x1F}),
            new FileSig("xls", new byte[] {0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1}, 512, new byte[] {0xFD, 0xFF, 0xFF, 0xFF, 0x22}),
            new FileSig("xls", new byte[] {0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1}, 512, new byte[] {0xFD, 0xFF, 0xFF, 0xFF, 0x23}),
            new FileSig("xls", new byte[] {0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1}, 512, new byte[] {0xFD, 0xFF, 0xFF, 0xFF, 0x28}),
            new FileSig("xls", new byte[] {0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1}, 512, new byte[] {0xFD, 0xFF, 0xFF, 0xFF, 0x29}),
            new FileSig("rar", new byte[] {0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 }),
            new FileSig("zip", new byte[] {0x57, 0x69, 0x6E, 0x5A, 0x69, 0x70 }) 
        };
        #endregion

        #region Public Methods
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

                            // Parse the HTTP session into its component requests and responses, and perform any required dechunking, gzipping etc
                            using (FileStream fileStream = new FileStream(path, 
                                                                          FileMode.Open, 
                                                                          FileAccess.Read, 
                                                                          FileShare.ReadWrite))
                            {
                                this.parser.Parse(fileStream, path);
                            }

                            foreach (Message message in this.parser.Messages)
                            {
                                // Ensure we have a valid response
                                if (message.Response.StatusCode == 0)
                                {
                                    continue;
                                }

                                if (message.Response.TempFileSize == 0)
                                {
                                    continue;
                                }

                                ProcessFiles(session, message);
                            }
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
                               string dataDirectory,
                               string outputDirectory)
        {
            this.Id = id;
            this.blockingCollection = blockingCollection;
            this.outputDirectory = outputDirectory;
            this.dataDirectory = dataDirectory;

            this.parser = new HttpKit.HttpParser();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataDirectory"></param>
        /// <param name="outputDirectory"></param>
        public void PreProcess(string dataDirectory, 
                               string outputDirectory)
        {
            IO.WriteTextToFile("\"MD5\",\"File\",\"Src IP\",\"Src Port\",\"Dst IP\",\"Dst Port\"" + Environment.NewLine, System.IO.Path.Combine(outputDirectory, "File.Hashes.csv"), false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataDirectory"></param>
        /// <param name="outputDirectory"></param>
        public void PostProcess(string dataDirectory, 
                                string outputDirectory)
        {
            CsvConfiguration csvConfiguration = new CsvConfiguration();
            csvConfiguration.QuoteAllFields = true;

            using (FileStream fileStream = new FileStream(System.IO.Path.Combine(outputDirectory, "File.Hashes.csv"), FileMode.Append, FileAccess.Write, FileShare.Read))
            using (StreamWriter streamWriter = new StreamWriter(fileStream))
            using (CsvHelper.CsvWriter csvWriter = new CsvHelper.CsvWriter(streamWriter, csvConfiguration))
            {
                // Now MD5 the files
                foreach (string file in System.IO.Directory.EnumerateFiles(outputDirectory,
                                                                           "*.xml",
                                                                           SearchOption.AllDirectories))
                {
                    string fileName = System.IO.Path.GetFileName(file);
                    if (fileName.StartsWith("Download.Details.") == false)
                    {
                        continue;
                    }

                    DownloadDetails downloadDetails = new DownloadDetails();
                    string ret = downloadDetails.Load(file);
                    if (ret.Length == 0)
                    {
                        csvWriter.WriteField(downloadDetails.Md5);
                        csvWriter.WriteField(downloadDetails.File);
                        csvWriter.WriteField(downloadDetails.SrcIp);
                        csvWriter.WriteField(downloadDetails.SrcPort);
                        csvWriter.WriteField(downloadDetails.DstIp);
                        csvWriter.WriteField(downloadDetails.DstPort);
                        csvWriter.NextRecord();
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
        public void SetProcessed()
        {
            this.processed = true;

            if (this.processing == false & this.blockingCollection.Count == 0)
            {
                Stop();
            }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        private void ProcessFiles(Session session, Message message)
        {
            byte[] header1 = woanware.IO.ReadFileHeader(message.Response.TempFile, 4);
            if (header1 == null)
            {
                return;
            }

            byte[] header2 = null;
            if (message.Response.TempFileSize > 520)
            {
                header2 = woanware.IO.ReadFileHeader(message.Response.TempFile, 6, 512);
            }
             
            if (header2 == null)
            {
                return;
            }

            foreach (FileSig sig in this.fileSigs)
            {
                // Do we have a match on file type
                if (ByteArrayCompare(sig.Sig, header1) == false)
                {
                    continue;
                }

                if (sig.OffsetSubHeader > 0)
                {
                    // Do we have a match on file type
                    if (ByteArrayCompare(sig.SigSubHeader, header2) == false)
                    {
                        continue;
                    }
                }

                // Now extract the contents
                string dir = session.SrcIpText + "." + session.SourcePort + "-" + session.DstIpText + "." + session.DestinationPort;
                if (System.IO.Directory.Exists(System.IO.Path.Combine(this.outputDirectory, dir)) == false)
                {
                    IO.CreateDirectory(System.IO.Path.Combine(this.outputDirectory, dir));
                }

                string fileName = message.Response.GetContentDispositionFileName;
                // Cannot determine a file name from the Content
                // Disposition HTTP header so lets make one up
                if (fileName.Length == 0)
                {
                    fileName = Guid.NewGuid().ToString() + "." + sig.Extension + ".safe";
                }
                else
                {
                    fileName += ".safe";
                }

                File.Copy(message.Response.TempFile, System.IO.Path.Combine(this.outputDirectory, dir, fileName), true);

                DownloadDetails downloadDetails = new DownloadDetails();
                downloadDetails.SrcIp = session.SrcIpText;
                downloadDetails.SrcPort = session.SourcePort;
                downloadDetails.DstIp = session.DstIpText;
                downloadDetails.DstPort = session.DestinationPort;
                downloadDetails.File = fileName;

                try
                {
                    // Not sure if BufferedStream should be wrapped in using block
                    using (var stream = new BufferedStream(File.OpenRead(System.IO.Path.Combine(this.outputDirectory, dir, fileName)), 1200000))
                    {
                        MD5 md5 = new MD5CryptoServiceProvider();
                        byte[] hashMd5 = md5.ComputeHash(stream);

                        downloadDetails.Md5 = woanware.Text.ConvertByteArrayToHexString(hashMd5);
                    }
                }
                catch (Exception) { }

                downloadDetails.Save(System.IO.Path.Combine(this.outputDirectory, dir, "Download.Details." + fileName + ".xml"));
                break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b1">Sig</param>
        /// <param name="b2">Header</param>
        /// <returns></returns>
        private bool ByteArrayCompare(byte[] b1, byte[] b2)
        {
            return memcmp(b1, b2, b1.Length) == 0;
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
                return "File Extractor (Sig)";
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
