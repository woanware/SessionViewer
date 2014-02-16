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
using Extractors;

namespace SessionViewer.SessionProcessors
{
    /// <summary>
    /// 
    /// </summary>
    internal class SmtpExtractor : InterfaceExtractor
    {
        #region Events
        public event woanware.Events.MessageEvent CompleteEvent;
        public event woanware.Events.MessageEvent ErrorEvent;
        public event woanware.Events.MessageEvent WarningEvent;
        public event woanware.Events.MessageEvent MessageEvent;
        #endregion

        public int Id { get; private set; }
        private CancellationTokenSource cancelSource = null;
        private string dataDirectory = string.Empty;
        private string outputDirectory = string.Empty;
        private BlockingCollection<Session> blockingCollection;
        private readonly object _lock = new object();
        private bool processed = false;
        private bool processing = false;

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="blockingCollection"></param>
        /// <param name="dataDirectory"></param>
        /// <param name="outputDirectory"></param>
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

                            byte[] temp = File.ReadAllBytes(path);
                            ProcessAttachments(session, temp);
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
        /// <param name="dataDirectory"></param>
        /// <param name="outputDirectory"></param>
        public void PreProcess(string dataDirectory, string outputDirectory)
        {
            // Write CSV header
            IO.WriteTextToFile("\"MD5\",\"File\",\"Src IP\",\"Src Port\",\"Dst IP\",\"Dst Port\",\"To\",\"From\",\"Mail From\",\"Sender\",\"Subject\",\"Date\"" + Environment.NewLine, System.IO.Path.Combine(outputDirectory, "Attachment.Hashes.csv"), false);
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

            using (FileStream fileStream = new FileStream(System.IO.Path.Combine(outputDirectory, "Attachment.Hashes.csv"), FileMode.Append, FileAccess.Write, FileShare.Read))
            using (StreamWriter streamWriter = new StreamWriter(fileStream))
            using (CsvHelper.CsvWriter csvWriter = new CsvHelper.CsvWriter(streamWriter, csvConfiguration))
            {
                // Now MD5 the files
                foreach (string file in System.IO.Directory.EnumerateFiles(outputDirectory,
                                                                           "*.xml",
                                                                           SearchOption.AllDirectories))
                {
                    string fileName = System.IO.Path.GetFileName(file);
                    if (fileName.StartsWith("Message.Details.") == false)
                    {
                        continue;
                    }

                    MessageDetails messageDetails = new MessageDetails();
                    string ret = messageDetails.Load(file);
                    if (ret.Length == 0)
                    {
                        foreach (AttachmentDetails attachment in messageDetails.Attachments)
                        {
                            csvWriter.WriteField(attachment.Md5);
                            csvWriter.WriteField(attachment.File);
                            csvWriter.WriteField(messageDetails.SrcIp);
                            csvWriter.WriteField(messageDetails.SrcPort);
                            csvWriter.WriteField(messageDetails.DstIp);
                            csvWriter.WriteField(messageDetails.DstPort);
                            csvWriter.WriteField(messageDetails.To);
                            csvWriter.WriteField(messageDetails.From);
                            csvWriter.WriteField(messageDetails.MailFrom);
                            csvWriter.WriteField(messageDetails.Sender);
                            csvWriter.WriteField(messageDetails.Subject);
                            csvWriter.WriteField(messageDetails.Date);
                            csvWriter.NextRecord();
                        }
                    }
                }
            }

            ProcessAttachmentHashes(outputDirectory);
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
        /// <param name="outputDirectory"></param>
        private void ProcessAttachmentHashes(string outputDirectory)
        {
            if (File.Exists(System.IO.Path.Combine(outputDirectory, "Attachment.Hashes.csv")) == false)
            {
                OnError("Cannot locate the \"Attachment.Hashes.csv\" files");
                return;
            }

            CsvConfiguration csvConfig = new CsvConfiguration();

            Regex regex = new Regex("<(.*?)>", RegexOptions.IgnoreCase);
            using (StreamReader sr = new StreamReader(System.IO.Path.Combine(outputDirectory, "Attachment.Hashes.csv")))
            using (CsvHelper.CsvReader csvReader = new CsvHelper.CsvReader(sr, csvConfig))
            {
                List<Attachment> attachments = new List<Attachment>();
                while (csvReader.Read())
                {
                    var md5 = csvReader.GetField(0);
                    var file = csvReader.GetField(1);
                    var fileName = System.IO.Path.GetFileName(file);
                    var srcIp = csvReader.GetField(2);
                    var srcPort = csvReader.GetField(3);
                    var dstIp = csvReader.GetField(4);
                    var dstPort = csvReader.GetField(5);
                    var to = csvReader.GetField(6);
                    var dateSent = csvReader.GetField(11);

                    List<string> tempTo = new List<string>(to.Split(','));
                    for (int index = tempTo.Count() - 1; index > -1; index--)
                    {
                        string person = tempTo[index].Trim().ToLower();
                        if (person.IndexOf("@") == -1)
                        {
                            tempTo.RemoveAt(index);
                            continue;
                        }

                        Match match = regex.Match(person);
                        if (match.Success == true)
                        {
                            person = match.Groups[1].Value;
                        }

                        person = person.Replace(@"""", string.Empty);

                        tempTo[index] = person;
                    }

                    var from = csvReader.GetField(6);
                    var sender = csvReader.GetField(7);
                    var subject = csvReader.GetField(10);

                    var attachment = (from a in attachments where a.Md5 == md5.ToLower() select a).SingleOrDefault();
                    if (attachment == null)
                    {
                        attachment = new Attachment();
                        attachment.Md5 = md5.ToLower();
                        attachment.Recipients.AddRange(tempTo);
                        attachment.Subjects.Add(subject);
                        attachment.Senders.Add(sender);
                        if (fileName.Length > 0)
                        {
                            attachment.FileNames.Add(fileName);
                        }
                        attachment.DateSent = dateSent;
                        attachments.Add(attachment);

                        SubjectRecipents subjectRecipient = new SubjectRecipents();
                        subjectRecipient.Subject = subject;
                        subjectRecipient.File = file;
                        subjectRecipient.Sender = sender;

                        foreach (string person in tempTo)
                        {
                            subjectRecipient.Recipients.Add(person);
                        }

                        attachment.SubjectRecipents.Add(subjectRecipient);
                    }
                    else
                    {
                        foreach (string person in tempTo)
                        {
                            var tempPerson = from r in attachment.Recipients where r == person select r;
                            if (tempPerson.Any() == false)
                            {
                                attachment.Recipients.Add(person);
                            }
                        }

                        var tempFileName = from s in attachment.FileNames where s == fileName select s;
                        if (tempFileName.Any() == false)
                        {
                            attachment.FileNames.Add(fileName);
                        }

                        var tempSubject = from s in attachment.Subjects where s.ToLower() == subject select s;
                        if (tempSubject.Any() == false)
                        {
                            attachment.Subjects.Add(subject);
                        }

                        var tempSender = from s in attachment.Senders where s.ToLower() == sender select s;
                        if (tempSender.Any() == false)
                        {
                            attachment.Senders.Add(sender);
                        }

                        var subjectRecipient = (from s in attachment.SubjectRecipents where s.Subject.ToLower() == subject.ToLower() select s).SingleOrDefault();
                        if (subjectRecipient == null)
                        {
                            subjectRecipient = new SubjectRecipents();
                            subjectRecipient.Subject = subject;
                            subjectRecipient.File = file;
                            subjectRecipient.Sender = sender;

                            foreach (string person in tempTo)
                            {
                                subjectRecipient.Recipients.Add(person);
                            }

                            attachment.SubjectRecipents.Add(subjectRecipient);
                        }
                        else
                        {
                            foreach (string person in tempTo)
                            {
                                var tempPerson = from r in subjectRecipient.Recipients where r == person select r;
                                if (tempPerson.Any() == false)
                                {
                                    subjectRecipient.Recipients.Add(person);
                                }
                            }
                        }
                    }
                }

                OutputSummary(outputDirectory, attachments);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputDirectory"></param>
        /// <param name="attachments"></param>
        private void OutputSummary(string outputDirectory, List<Attachment> attachments)
        {
            string summaryFile = System.IO.Path.Combine(outputDirectory, "Attachment.Summary.txt");
            string recipientFile = System.IO.Path.Combine(outputDirectory, "Recipient.Summary.txt");

            attachments.Sort((x, y) => x.Md5.CompareTo(y.Md5));
            foreach (Attachment attachment in attachments)
            {
                IO.WriteTextToFile("MD5: " + attachment.Md5 + Environment.NewLine, summaryFile, true);
                IO.WriteTextToFile("Date Sent: " + attachment.DateSent + Environment.NewLine, summaryFile, true);

                attachment.SubjectRecipents.Sort((x, y) => x.Subject.CompareTo(y.Subject));
                foreach (SubjectRecipents subjectRecipents in attachment.SubjectRecipents)
                {
                    IO.WriteTextToFile("Subject: " + subjectRecipents.Subject + Environment.NewLine, summaryFile, true);
                    IO.WriteTextToFile("File: " + subjectRecipents.File + Environment.NewLine, summaryFile, true);
                    IO.WriteTextToFile("Sender: " + subjectRecipents.Sender + Environment.NewLine, summaryFile, true);
                    foreach (string person in subjectRecipents.Recipients)
                    {
                        IO.WriteTextToFile("Recipient: " + person + Environment.NewLine, summaryFile, true);
                    }
                }
                
                IO.WriteTextToFile(Environment.NewLine, summaryFile, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputDir"></param>
        /// <param name="session"></param>
        /// <param name="data"></param>
        /// <param name="mailFrom"></param>
        private void ProcessAttachments(Session session,
                                        byte[] data)
        {
            try
            {
                string dir = session.SrcIpText + "." + session.SourcePort + "-" + session.DstIpText + "." + session.DestinationPort;

                using (var stream = new MemoryStream(data))
                {
                    MimeMessage message = MimeMessage.Load(stream);
                    ProcessMessage(session, message, dir);

                    HashFiles(session,
                              message,
                              this.outputDirectory,
                              System.IO.Path.Combine(this.outputDirectory, dir));
                }
            }
            catch (Exception ex)
            {
                this.Log().Error(ex.Message.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputFile"></param>
        /// <returns></returns>
        private string GetSmtpMailFrom(string inputFile)
        {
            Regex regex = new Regex(@"MAIL FROM:\s*<(.*)>", RegexOptions.IgnoreCase);
            string line = string.Empty;
            using (System.IO.StreamReader streamReader = new System.IO.StreamReader(inputFile))
            {
                while ((line = streamReader.ReadLine()) != null)
                {
                    Match match = regex.Match(line);
                    if (match.Success == true)
                    {
                        return match.Groups[1].Value.Trim();
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Method used to provide recursive processing
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="outputDir"></param>
        /// <param name="dir"></param>
        private void ProcessMessage(Session session,
                                    MimeKit.MimeMessage message,
                                    string dir)
        {
            if (message.Body is MimeKit.MessagePart)
            {
                var temp = (MimeKit.MessagePart)message.Body;
                ProcessMessage(session, temp.Message, dir);
            }
            else if (message.Body is MimeKit.Multipart)
            {
                var multipart = (Multipart)message.Body;
                ProcessMultipart(session, message, multipart, dir);
            }
            else
            {
                var part = (MimePart)message.Body;
                ProcessAttachment(session, message, part, dir);
            }
        }

        /// <summary>
        /// Method used to provide recursive processing
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="multipart"></param>
        /// <param name="outputDir"></param>
        /// <param name="dir"></param>
        private void ProcessMultipart(Session session,
                                      MimeKit.MimeMessage message,
                                      MimeKit.Multipart multipart,
                                      string dir)
        {
            foreach (var subpart in multipart)
            {
                if (subpart is MimeKit.Multipart)
                {
                    var temp = (Multipart)subpart;
                    ProcessMultipart(session, message, temp, dir);
                    continue;
                }

                if (subpart is MimeKit.MessagePart)
                {
                    var temp = (MimeKit.MessagePart)subpart;
                    ProcessMessage(session, temp.Message, dir);
                    continue;
                }

                ProcessAttachment(session, message, subpart, dir);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="mimeEntity"></param>
        /// <param name="outputDir"></param>
        /// <param name="dir"></param>
        private void ProcessAttachment(Session session,
                                       MimeKit.MimeMessage message,
                                       MimeKit.MimeEntity mimeEntity,
                                       string dir)
        {
            var part = (MimePart)mimeEntity;

            if (part.IsAttachment == false)
            {
                return;
            }

            string path = string.Empty;
            string fileName = string.Empty;
            try
            {
                // It appears that it is possible for filenames to have new line characters in them!
                path = woanware.Path.ReplaceIllegalPathChars(part.FileName + ".safe");
                path = path.Replace("\n", string.Empty);
                path = path.Replace("\r", string.Empty);
                path = System.IO.Path.Combine(this.outputDirectory, dir, path);
                fileName = part.FileName.Replace("\n", string.Empty);
                fileName = part.FileName.Replace("\r", string.Empty);
                fileName += ".safe";

                if (System.IO.Directory.Exists(System.IO.Path.Combine(this.outputDirectory, dir)) == false)
                {
                    IO.CreateDirectory(System.IO.Path.Combine(this.outputDirectory, dir));
                }

                if (File.Exists(path) == true)
                {
                    fileName = Guid.NewGuid().ToString() + fileName;
                    path = System.IO.Path.Combine(this.outputDirectory, dir, fileName);
                }

                using (var stream = File.Create(path))
                {
                    part.ContentObject.DecodeTo(stream);
                }
            }
            catch (Exception ex)
            {
                this.Log().Error(ex.ToString());            
            }

            // Check that the file didn't get quaratined by AV
            if (File.Exists(path) == false)
            {
                return;
            }

            PerformArchiveDecompression(path,
                                        fileName,
                                        System.IO.Path.Combine(this.outputDirectory, dir));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="outputDir"></param>
        private void PerformArchiveDecompression(string path,
                                                 string fileName,
                                                 string outputDir)
        {
            try
            {
                byte[] fileHeader = IO.ReadFileHeader(path, 4);
                if (fileHeader == null)
                {
                    return;
                }

                if (woanware.IO.IsZip(fileHeader) == true)
                {
                    string temp = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    if (System.IO.Directory.Exists(System.IO.Path.Combine(outputDir, temp)) == false)
                    {
                        woanware.IO.CreateDirectory(System.IO.Path.Combine(outputDir, temp));
                    }

                    using (ZipArchive archive = ZipFile.OpenRead(path))
                    {
                        foreach (ZipArchiveEntry zae in archive.Entries)
                        {
                            zae.ExtractToFile(System.IO.Path.Combine(outputDir, temp, zae.Name + ".safe"));
                        }
                    }
                }
            }
            catch (Exception ex) 
            {
                this.Log().Error(ex.ToString());            
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="outputDir"></param>
        private void HashFiles(Session session,
                               MimeKit.MimeMessage message,
                               string parentDir,
                               string outputDir)
        {
            if (System.IO.Directory.Exists(outputDir) == false)
            {
                return;
            }

            string mailFrom = GetSmtpMailFrom(System.IO.Path.Combine(this.dataDirectory, 
                                                                     session.Guid.Substring(0, 2), 
                                                                     session.Guid + ".bin"));
            string from = string.Empty;

            if (message.From != null)
            {
                List<string> temp = new List<string>();
                Regex regex = new Regex("<(.*?)>", RegexOptions.IgnoreCase);
                for(int index = 0; index < message.From.Count; index++)
                {
                    Match match = regex.Match(message.From[index].ToString());
                    if (match.Success == true)
                    {
                        temp.Add(match.Groups[1].Value);
                    }
                    else
                    {
                        temp.Add(message.From[index].ToString());
                    }
                    
                }
                from = string.Join(",", temp);
            }

            string to = string.Empty;
            if (message.To != null)
            {
                List<string> temp = new List<string>();
                Regex regex = new Regex("<(.*?)>", RegexOptions.IgnoreCase);
                for (int index = 0; index < message.From.Count; index++)
                {
                    Match match = regex.Match(message.To[index].ToString());
                    if (match.Success == true)
                    {
                        temp.Add(match.Groups[1].Value);
                    }
                    else
                    {
                        temp.Add(message.To[index].ToString());
                    }

                }
                to = string.Join(",", temp);
            }

            string sender = string.Empty;
            if (message.Sender != null)
            {
                sender = message.Sender.Address;
            }

            MessageDetails messageDetails = new MessageDetails();
            messageDetails.SrcIp = session.SrcIpText;
            messageDetails.SrcPort = session.SourcePort;
            messageDetails.DstIp = session.DstIpText;
            messageDetails.DstPort = session.DestinationPort;
            messageDetails.From = from;
            messageDetails.To = to;
            messageDetails.MailFrom = mailFrom;
            messageDetails.Sender = sender;
            messageDetails.Subject = message.Subject;
            messageDetails.Date = message.Date.ToString("o");
           
            // Now MD5 the files
            foreach (string file in System.IO.Directory.EnumerateFiles(outputDir,
                                                                        "*.*",
                                                                        SearchOption.AllDirectories))
            {
                if (System.IO.Path.GetFileName(file) == "Message.Info.txt")
                {
                    continue;
                }

                try
                {
                    // Not sure if BufferedStream should be wrapped in using block
                    using (var stream = new BufferedStream(File.OpenRead(file), 1200000))
                    {
                        MD5 md5 = new MD5CryptoServiceProvider();
                        byte[] hashMd5 = md5.ComputeHash(stream);

                        AttachmentDetails attachmentDetails = new AttachmentDetails();
                        attachmentDetails.File = file;
                        attachmentDetails.Md5 = woanware.Text.ConvertByteArrayToHexString(hashMd5);
                        messageDetails.Attachments.Add(attachmentDetails);
                    }
                }
                catch (Exception) { }
            }

             messageDetails.Save(System.IO.Path.Combine(outputDir, "Message.Details." + message.MessageId + ".xml"));
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
                return "SMTP";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Category
        {
            get
            {
                return string.Empty;
            }
        }
        #endregion
    }
}
