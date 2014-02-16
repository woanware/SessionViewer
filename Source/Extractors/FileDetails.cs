using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using woanware;

namespace Extractors
{
    /// <summary>
    /// Allows us to save/load the HTTP download details to/from XML
    /// </summary>
    public class DownloadDetails
    {
        #region Member Variables
        public string SrcIp { get; set; }
        public int SrcPort { get; set; }
        public string DstIp { get; set; }
        public int DstPort { get; set; }
        public string File { get; set; }
        public string Md5 { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public DownloadDetails()
        {
            SrcIp = string.Empty;
            DstIp = string.Empty;
            File = string.Empty;
            Md5 = string.Empty;
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

                XmlSerializer serializer = new XmlSerializer(typeof(DownloadDetails));

                FileInfo info = new FileInfo(path);
                using (FileStream stream = info.OpenRead())
                {
                    DownloadDetails downloadDetails = (DownloadDetails)serializer.Deserialize(stream);
                    this.SrcIp = downloadDetails.SrcIp;
                    this.SrcPort = downloadDetails.SrcPort;
                    this.DstIp = downloadDetails.DstIp;
                    this.DstPort = downloadDetails.DstPort;
                    this.File = downloadDetails.File;
                    this.Md5 = downloadDetails.Md5;

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
                XmlSerializer serializer = new XmlSerializer(typeof(DownloadDetails));
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
