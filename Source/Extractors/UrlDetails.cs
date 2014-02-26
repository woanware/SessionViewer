using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Extractors
{
    /// <summary>
    /// Allows us to save/load the HTTP URL details to/from XML
    /// </summary>
    public class UrlDetails
    {
        #region Member Variables
        public string SrcIp { get; set; }
        public int SrcPort { get; set; }
        public string DstIp { get; set; }
        public int DstPort { get; set; }
        public List<string> Urls { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public UrlDetails()
        {
            SrcIp = string.Empty;
            DstIp = string.Empty;
            Urls = new List<string>();
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

                XmlSerializer serializer = new XmlSerializer(typeof(UrlDetails));

                FileInfo info = new FileInfo(path);
                using (FileStream stream = info.OpenRead())
                {
                    UrlDetails urlDetails = (UrlDetails)serializer.Deserialize(stream);
                    this.SrcIp = urlDetails.SrcIp;
                    this.SrcPort = urlDetails.SrcPort;
                    this.DstIp = urlDetails.DstIp;
                    this.DstPort = urlDetails.DstPort;

                    foreach (string temp in urlDetails.Urls)
                    {
                        this.Urls.Add(temp);
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
                this.Urls.Sort();

                XmlSerializer serializer = new XmlSerializer(typeof(UrlDetails));
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
