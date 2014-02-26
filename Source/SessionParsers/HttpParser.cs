using HtmlParserSharp;
using HttpKit;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;
using Trinet.Core.IO.Ntfs;
using woanware;

namespace SessionViewer.SessionParsers
{
    /// <summary>
    /// 
    /// </summary>
    public class HttpParser : InterfaceSessionParser
    {
        #region Member Variables
        private HttpKit.Parser parser;
        private Session session;
        private string outputPath = string.Empty;
        private string outputFile = string.Empty;
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public HttpParser()
        {
            parser = new HttpKit.Parser();
            Enabled = true;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="session"></param>
        /// <param name="db"></param>
        /// <param name="pk"></param>
        public void Process(string outputPath, 
                            Session session)
        {
            this.outputPath = outputPath;
            this.session = session;

            this.outputFile = System.IO.Path.Combine(outputPath, session.Guid.Substring(0, 2), session.Guid + ".bin");

            // Parse the HTTP session into its component requests and responses, and perform any required dechunking, gzipping etc
            using (FileStream fileStream = new FileStream(outputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                this.parser.Parse(fileStream, outputFile);
            }

            //string ret = woanware.IO.DeleteFile(this.outputFile);

            //if (ret.Length > 0)
            //{

            //}
            
            // Output the parsed HTTP data to a binary file
            using (FileStream fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                this.parser.WriteToFile(fs);
            }

            SaveToHtml();
            ParseHttpData(outputPath, session);
            UpdateDatabaseSession();
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        private void UpdateDatabaseSession()
        {
            List<string> methods = new List<string>();
            string host = string.Empty;
            foreach (Message message in this.parser.Messages)
            {
                // Ensure that the request object is valid
                if (message.Request.Method.Length == 0)
                {
                    continue;
                }

                host = message.Request.Host;

                if (methods.Contains(message.Request.Method) == false)
                {
                    methods.Add(message.Request.Method);
                }
            }

            using (DbConnection dbConnection = Db.GetOpenConnection(this.outputPath))
            using (var db = new NPoco.Database(dbConnection, NPoco.DatabaseType.SQLCe))
            {
                var obj = db.SingleOrDefaultById<Session>(this.session.Id);
                if (obj != null)
                {
                    methods.Sort();
                    this.session.HttpMethods = string.Join(",", methods);
                    this.session.HttpHost = host;
                    int ret = db.Update(this.session);  
                }
            }
        }

        /// <summary>
        /// Output the parsed HTTP data to a HTML file
        /// </summary>
        /// <param name="parser"></param>
        private void SaveToHtml()
        {
            FileInfo file = new FileInfo(outputFile);

            FileStream fs = null;
            try
            {
                if (file.AlternateDataStreamExists("html") == false)
                {
                    fs = file.GetAlternateDataStream("html").OpenWrite();
                }
                else
                {
                    // Delete the existing HTML ADS since we will replace it
                    file.DeleteAlternateDataStream("html");
                    fs = file.GetAlternateDataStream("html").OpenWrite();
                }

                this.parser.WriteToHtmlFile(fs);
            }
            finally
            {
                if (fs!= null)
                {
                    fs.Dispose();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="guid"></param>
        private void ParseHttpData(string outputPath, Session session)
        {
            //string fileName = string.Empty;
            //if (File.Exists(System.IO.Path.Combine(outputPath, session.Guid + ".txt")) == true)
            //{
            //    fileName = System.IO.Path.Combine(outputPath, session.Guid + ".txt");
            //}
            //else
            //{
            //    fileName = System.IO.Path.Combine(outputPath, session.Guid + ".bin");
            //}

            FileInfo file = new FileInfo(this.outputFile);

            FileStream fs = null;
            try
            {
                // Open up the "info" ADS
                if (file.AlternateDataStreamExists("info") == false)
                {
                    fs = file.GetAlternateDataStream("info").OpenWrite();
                }
                else
                {
                    AlternateDataStreamInfo s = file.GetAlternateDataStream("info", FileMode.Open);
                    fs = s.OpenWrite();
                }

                foreach (Message message in this.parser.Messages)
                { 
                    // Ensure that the response object is valid
                    if (message.Response.StatusCode == 0)
                    {
                        continue;
                    }

                    // Check the Content-Type header? e.g. contains "HTML" 
                    if (message.Response.ContentType.IndexOf("text/html", StringComparison.InvariantCultureIgnoreCase) == -1)
                    {
                        continue;
                    }

                    ParseLinks(fs, message.Response);
                }
            }
            finally
            {
                if (fs != null)
                {
                    fs.Dispose();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fs"></param>
        private void ParseLinks(FileStream fs, Response response)
        {
            try
            {
                if (File.Exists(response.TempFile) == false)
                {
                    return;
                }

                // Lets ensure that the first non-blank line has a HTML header!
                using (FileStream temp = System.IO.File.OpenRead(response.TempFile))
                {
                    using (LineReader lr = new LineReader(temp, 4096, Encoding.Default))
                    {
                        bool process = true;
                        while (process == true)
                        {
                            string line = lr.ReadLine();
                            if (line == null)
                            {
                                return;
                            }

                            line = line.Trim();

                            if (line.Length == 0)
                            {
                                continue;
                            }

                            bool validHtml = true;
                            if (line.IndexOf("<html>", StringComparison.InvariantCultureIgnoreCase) == -1)
                            {
                                if (line.IndexOf("<!doctype html", StringComparison.InvariantCultureIgnoreCase) == -1)
                                {
                                    validHtml = false;
                                }
                            }

                            if (validHtml == true)
                            {
                                break;
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                }

                using (TextReader tr = File.OpenText(response.TempFile))
                {
                    SimpleHtmlParser parser = new SimpleHtmlParser();
                    var doc = parser.Parse(tr);

                    List<string> links = new List<string>();
                    foreach (System.Xml.XmlElement link in doc.GetElementsByTagName("a"))
                    {
                        if (link.Attributes == null)
                        {
                            continue;
                        }

                        if (link.Attributes["href"] == null)
                        {
                            continue;
                        }

                        var href = link.Attributes["href"].Value.Trim();

                        string md5 = Text.ConvertByteArrayToHexString(Security.GenerateMd5Hash(href));
                        if (md5.ToLower() == "6666cd76f96956469e7be39d750cc7d9")
                        {
                            // Ignore "/"
                            continue;
                        }

                        if (links.Contains(md5) == false)
                        {
                            links.Add(md5);
                            woanware.IO.WriteToFileStream(fs, "LINK: " + href + Environment.NewLine);
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        #region Properties
        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get
            {
                return "HTTP";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ushort Port
        {
            get
            {
                return 80;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ParserType Type
        {
            get
            {
                return ParserType.Session;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Protocol
        {
            get
            {
                return "TCP";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Enabled { get; set; }
        #endregion
    }
}
