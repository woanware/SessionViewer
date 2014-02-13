using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Trinet.Core.IO.Ntfs;
using woanware;

namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    public static class Functions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="outBound"></param>
        /// <returns></returns>
        public static byte[] GenerateHtmlToBytes(string data, bool outBound)
        {
            string html = string.Empty;
            if (outBound == true)
            {
                html = "<font color=\"#006600\" size=\"2\">";
            }
            else
            {
                html = "<font color=\"#FF0000\" size=\"2\">";
            }

            if (html.EndsWith("\r\n\r\n") == true)
            {
                html = html.Substring(0, html.Length - 4);
            }
  
            string encodedData = HttpUtility.HtmlEncode(data);
            encodedData = encodedData.Replace("\r\n", "<br>");
            html += encodedData;
            html += @"</font>";
            html += @"<br>";

            if (outBound == false)
            {
                html += @"<br>";    
            }

            return Encoding.ASCII.GetBytes(html.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="outBound"></param>
        /// <returns></returns>
        public static string GenerateHtml(string data, bool outBound)
        {
            string html = string.Empty;
            if (outBound == true)
            {
                html = "<font color=\"#006600\" size=\"2\">";
            }
            else
            {
                html = "<font color=\"#FF0000\" size=\"2\">";
            }

            string encodedData = HttpUtility.HtmlEncode(data);
            encodedData = encodedData.Replace("\r\n", "<br>");
            html += encodedData;
            html += @"</font>";
            html += @"<br>";

            if (outBound == false)
            {
                html += @"<br>";
            }

            return html.ToString();
        }

        

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="fs"></param>
        ///// <param name="data"></param>
        //public static void WriteToFile(FileInfo file, string data, string ads)
        //{
           
        //    byte[] temp = ASCIIEncoding.ASCII.GetBytes(data);
        //    fs.Write(temp, 0, temp.Length);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="fs"></param>
        ///// <param name="data"></param>
        //public static void WriteToFile(FileInfo file, byte[] data, string ads)
        //{
        //    fs.Write(data, 0, data.Length);
        //}
    }
}
