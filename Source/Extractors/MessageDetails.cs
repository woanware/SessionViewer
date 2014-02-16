using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using woanware;

namespace Extractors
{
    /// <summary>
    /// 
    /// </summary>
    public class AttachmentDetails
    {
        public string File { get; set; }
        public string Md5 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public AttachmentDetails()
        {
            File = string.Empty;
            Md5 = string.Empty;
        }
    }

    /// <summary>
    /// Allows us to save/load the SMTP message details to/from XML
    /// </summary>
    public class MessageDetails
    {
        #region Member Variables
        public string SrcIp { get;set; }
        public int SrcPort { get;set; }
        public string DstIp { get;set; }
        public int DstPort { get;set; }
        public string To { get;set; }
        public string From  { get;set; }
        public string MailFrom { get;set; }
        public string Sender { get;set; }
        public string Subject { get;set; }
        public string Date { get; set; }
        public List<AttachmentDetails> Attachments { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public MessageDetails()
        {
            SrcIp = string.Empty;
            DstIp = string.Empty;
            To = string.Empty;
            From = string.Empty;
            MailFrom = string.Empty;
            Sender = string.Empty;
            Subject = string.Empty;
            Date = string.Empty;
            Attachments = new List<AttachmentDetails>();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string Load(string path)
        {
            try
            {
                if (System.IO.File.Exists(path) == false)
                {
                    return string.Empty;
                }

                XmlSerializer serializer = new XmlSerializer(typeof(MessageDetails));

                FileInfo info = new FileInfo(path);
                using (FileStream stream = info.OpenRead())
                {
                    MessageDetails messageDetails = (MessageDetails)serializer.Deserialize(stream);
                    this.SrcIp = messageDetails.SrcIp;
                    this.SrcPort = messageDetails.SrcPort;
                    this.DstIp = messageDetails.DstIp;
                    this.DstPort = messageDetails.DstPort;
                    this.From = messageDetails.From;
                    this.To = messageDetails.To;
                    this.MailFrom = messageDetails.MailFrom;
                    this.Sender = messageDetails.Sender;
                    this.Subject = messageDetails.Subject;
                    this.Date = messageDetails.Date;

                    foreach (AttachmentDetails temp in messageDetails.Attachments)
                    {
                        AttachmentDetails attachmentDetails = new AttachmentDetails();
                        attachmentDetails.File = temp.File;
                        attachmentDetails.Md5 = temp.Md5;
                        this.Attachments.Add(attachmentDetails);
                    }

                    return string.Empty;
                }
            }
            catch (FileNotFoundException fileNotFoundEx)
            {
                return fileNotFoundEx.Message;
            }
            catch (UnauthorizedAccessException unauthAccessEx)
            {
                return unauthAccessEx.Message;
            }
            catch (IOException ioEx)
            {
                return ioEx.Message;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string Save(string path)
        {
            try
            { 
                XmlSerializer serializer = new XmlSerializer(typeof(MessageDetails));
                using (StreamWriter writer = new StreamWriter(path, false))
                {
                    serializer.Serialize((TextWriter)writer, this);
                    return string.Empty;
                }
            }
            catch (FileNotFoundException fileNotFoundEx)
            {
                return fileNotFoundEx.Message;
            }
            catch (UnauthorizedAccessException unauthAccessEx)
            {
                return unauthAccessEx.Message;
            }
            catch (IOException ioEx)
            {
                return ioEx.Message;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        #endregion
    }
}
