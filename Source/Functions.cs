using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="guid"></param>
        public static bool GzipDecodeSession(string outputPath, string guid)
        {
            try
            {
                Regex regexHttpRequest = new Regex(@"^.*\s.*HTTP/1.[0,1]", RegexOptions.Compiled);
                Regex regexUnprintable = new Regex(@"[^\u0000-\u007F]");
                Regex regexHttpResponse = new Regex(@"^HTTP/1\.[0,1]\s+\d*\s\w*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

                using (FileStream fileHtml = new FileStream(System.IO.Path.Combine(outputPath, guid + ".gzipped.html"), FileMode.Create))
                using (FileStream fileStream = new FileStream(System.IO.Path.Combine(outputPath, guid + ".bin"), FileMode.Open))
                using (BinaryReader streamReader = new BinaryReader(fileStream))
                {
                    byte[] htmlHeader = Encoding.ASCII.GetBytes(Global.HTML_HEADER);
                    fileHtml.Write(htmlHeader, 0, htmlHeader.Length);

                    string line = string.Empty;
                    while ((line = ReadLine(streamReader)) != null)
                    {
                        if (regexHttpRequest.Match(line).Success == true)
                        {
                            string request = ParseRequest(streamReader, line);
                            string sanitised = regexUnprintable.Replace(woanware.Text.ReplaceNulls(request), ".");
                            byte[] html = Functions.GenerateHtmlToBytes(sanitised, true);
                            fileHtml.Write(html, 0, html.Length);
                            continue;
                        }

                        if (regexHttpResponse.Match(line).Success == true)
                        {
                            string response = ParseResponse(streamReader, line);
                            if (response == string.Empty)
                            {
                                break;
                            }

                            string sanitised = regexUnprintable.Replace(woanware.Text.ReplaceNulls(response), ".");
                            byte[] html = Functions.GenerateHtmlToBytes(sanitised, false);
                            fileHtml.Write(html, 0, html.Length);
                        }
                    }

                    byte[] htmlFooter = Encoding.ASCII.GetBytes(Global.HTML_FOOTER);
                    fileHtml.Write(htmlFooter, 0, htmlFooter.Length);
                }

                return true;
            }
            catch (Exception ex)
            {
                IO.DeleteFile(System.IO.Path.Combine(outputPath, guid + ".gzipped.html"));
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="binaryReader"></param>
        /// <param name="previousLine"></param>
        private static string ParseRequest(BinaryReader binaryReader, 
                                           string previousLine)
        {
            StringBuilder request = new StringBuilder();
            request.AppendLine(previousLine);

            string line = string.Empty;
            while ((line = ReadLine(binaryReader)) != null)
            {
                if (line == string.Empty)
                {
                    return request.ToString();
                }

                request.AppendLine(line);
            }

            return request.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="binaryReader"></param>
        /// <param name="previousLine"></param>
        /// <returns></returns>
        private static string ParseResponse(BinaryReader binaryReader,
                                            string previousLine)
        {  
            Regex regexHttpContentLength = new Regex(@"^content-length:\s*(\d*)", RegexOptions.IgnoreCase);
            Regex regexHttpContentEncoding = new Regex(@"^content-encoding:\s*(\w*)", RegexOptions.IgnoreCase);
            Regex regexChunkedEncoding = new Regex(@"^transfer-encoding:\s*chunked", RegexOptions.IgnoreCase);

            StringBuilder response = new StringBuilder();
            response.AppendLine(previousLine);

            string line = string.Empty;
            int contentLength = 0;
            string contentType = string.Empty;
            bool chunked = false;
            while ((line = ReadLine(binaryReader)) != null)
            {
                if (line == string.Empty)
                {
                    response.Append(Environment.NewLine);

                    byte[] body = new byte[contentLength];
                    int ret = binaryReader.Read(body, 0, contentLength);

                    if (contentType.ToLower() == "gzip" & chunked == false)
                    {
                        byte[] decompressedTemp = Decompress(body);
                        string decompressed = Encoding.ASCII.GetString(decompressedTemp);
                        return response.Append(decompressed).ToString();
                    }
                    else if (contentType.ToLower() == "gzip" & chunked == true)
                    {
                        while ((line = ReadLine(binaryReader)) != null)
                        {
                            string length = "0x" + line;
                        }
                    }
                    else
                    {
                        string decompressed = Encoding.ASCII.GetString(body);
                        return response.Append(decompressed).ToString();
                    }
                }

                if (contentLength == 0)
                {
                    Match match = regexHttpContentLength.Match(line);
                    if (match.Success == true)
                    {
                        if (int.TryParse(match.Groups[1].Value, out contentLength) == false)
                        {
                            return string.Empty;
                        }
                    }
                }

                if (contentType.Length == 0)
                {
                    Match match = regexHttpContentEncoding.Match(line);
                    if (match.Success == true)
                    {
                        contentType = match.Groups[1].Value;
                    }
                }

                if (chunked == false)
                {
                    Match match = regexChunkedEncoding.Match(line);
                    if (match.Success == true)
                    {
                        chunked = true;
                    }
                }

                response.AppendLine(line);
            }

            return response.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        private static string ReadLine(BinaryReader streamReader)
        {
            StringBuilder builder = new StringBuilder();
            while (true)
            {
                int num = streamReader.Read();
                switch (num)
                {
                    case -1:
                        if (builder.Length > 0)
                        {
                            return builder.ToString();
                        }

                        return null;

                    case 13:
                    case 10:
                        if (num == 13)
                        {
                            num = streamReader.Read();
                            if (num == 10)
                            {
                                return builder.ToString();
                            }

                            builder.Append((char)num);
                        }
                        break;
                }

                builder.Append((char)num);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gzip"></param>
        /// <returns></returns>
        private static byte[] Decompress(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip),
                                                      CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }
    }
}
