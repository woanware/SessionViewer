using System;
using System.Collections.Generic;

namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    internal class SubjectRecipents
    {
        public string Subject { get; set; }
        public List<string> Recipients { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public SubjectRecipents()
        {
            Subject = string.Empty;
            Recipients = new List<string>();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal class Attachment
    {
        public string Md5 { get; set; }
        public List<string> FileNames { get; set; }
        public List<string> Subjects { get; set; }
        public List<string> Recipients { get; set; }
        public List<string> Senders { get; set; }
        public string DateSent { get; set; }
        public List<SubjectRecipents> SubjectRecipents { get; set; }

        public Attachment()
        {
            Md5 = string.Empty;
            FileNames = new List<string>();
            Subjects = new List<string>();
            Recipients = new List<string>();
            Senders = new List<string>();
            DateSent = string.Empty;
            SubjectRecipents = new List<SubjectRecipents>();
        }
    }
}
