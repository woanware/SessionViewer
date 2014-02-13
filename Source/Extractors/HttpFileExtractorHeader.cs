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

namespace SessionViewer.SessionProcessors
{
    /// <summary>
    /// 
    /// </summary>
    internal class HttpFileExtractorHeader : InterfaceExtractor
    {
        public event woanware.Events.MessageEvent Complete;
        public event woanware.Events.MessageEvent Error;
        public event woanware.Events.MessageEvent Warning;
        public event woanware.Events.MessageEvent Message;

        private string _dataDirectory = string.Empty;
        private string _outputDirectory = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="directory"></param>
        public void Run(ArrayList sessions, 
                        string dataDirectory, 
                        string outputDirectory)
        {
            _dataDirectory = dataDirectory;
            _outputDirectory = outputDirectory;

            (new Thread(() =>
            {
                //// Write CSV header
                //IO.WriteTextToFile("\"MD5\",\"File\",\"Src IP\",\"Src Port\",\"Dst IP\",\"Dst Port\",\"To\",\"From\",\"Mail From\",\"Sender\",\"Subject\",\"Date\"" + Environment.NewLine, System.IO.Path.Combine(_outputDirectory, "Attachment.Hashes.csv"), false);
                //foreach (Session session in sessions)
                //{
                //    if (File.Exists(System.IO.Path.Combine(dataDirectory, session.Guid.Substring(0, 2), session.Guid + ".bin")) == false)
                //    {
                //        continue;
                //    }

                //    byte[] temp = File.ReadAllBytes(System.IO.Path.Combine(dataDirectory, session.Guid.Substring(0, 2), session.Guid + ".bin"));
                //    ProcessAttachments(session, temp);
                //}

                //ProcessAttachmentHashes();

                OnComplete();

            })).Start();   
        }

        #region Event Methods
        /// <summary>
        /// 
        /// </summary>
        private void OnComplete()
        {
            var handler = Complete;
            if (handler != null)
            {
                handler("SMTP processing complete");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnError(string text)
        {
            var handler = Error;
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
            var handler = Warning;
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
            var handler = Message;
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
                return "File Extractor (Header)";
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
