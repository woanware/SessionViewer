using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using woanware;
using Trinet.Core.IO.Ntfs;

namespace SessionViewer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //FileStream fs = File.OpenRead(@"M:\Data\HTTP\woanware1.bin");
            //HttpKit.HttpParser httpP = new HttpKit.HttpParser(fs);
            //httpP.Test();
            //httpP.Parse();

            //FileStream fs = File.OpenRead(@"M:\Data\HTTP\woanware1.bin");
            //HttpKit.HttpParser httpP = new HttpKit.HttpParser();
            //httpP.Parse(fs);
            ////httpP.WriteToFile("test.txt", true);

            //FileInfo file = new FileInfo("test.txt");

            ////leStream fs;
            //if (file.AlternateDataStreamExists("html") == false)
            //{
            //    fs = file.GetAlternateDataStream("html").OpenWrite();
            //}
            //else
            //{
            //    AlternateDataStreamInfo s = file.GetAlternateDataStream("html", FileMode.Open);
            //    fs = s.OpenWrite();
            //}
            //httpP.WriteToHtmlFile(fs);

            //return;

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(Program.OnUnhandledException);
            Application.ThreadException += new ThreadExceptionEventHandler(Program.OnThreadException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormMain());
        }

        #region Unhandled Exception Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = (Exception)e.ExceptionObject;

            "Unhandled Exception".Log().Error(exception.ToString());            

            //IO.WriteTextToFile("An unhandled exception has occurred: " + exception.ToString() + Environment.NewLine, System.IO.Path.Combine(Misc.GetUserDataDirectory(), "Errors.txt"), true);
            //Misc.WriteToEventLog(Application.ProductName, "An unhandled exception has occurred: " + exception.ToString(), EventLogEntryType.Error);
            UserInterface.DisplayErrorMessageBox("An unhandled exception has occurred, check the Log.txt file for details: " + exception.Message);

            //IO.WriteTextToFile(exception.ToString() + Environment.NewLine, System.IO.Path.Combine(Misc.GetApplicationDirectory(), "Errors.txt"), true);
            //Misc.WriteToEventLog(Application.ProductName, "An unhandled exception has occurred: " + exception.ToString(), EventLogEntryType.Error);
            //UserInterface.DisplayErrorMessageBox("An unhandled exception has occurred, check the event log for details");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Exception exception = (Exception)e.Exception;
            "Unhandled Exception".Log().Error(exception.ToString());    
            //IO.WriteTextToFile("An unhandled exception has occurred: " + exception.ToString() + Environment.NewLine, System.IO.Path.Combine(Misc.GetUserDataDirectory(), "Errors.txt"), true);
            //Misc.WriteToEventLog(Application.ProductName, "An unhandled exception has occurred: " + Environment.NewLine + Environment.NewLine + exception.ToString(), EventLogEntryType.Error);
            UserInterface.DisplayErrorMessageBox("An unhandled exception has occurred, check the Errors.txt file for details: " + exception.Message);

            //IO.WriteTextToFile(exception.ToString() + Environment.NewLine, System.IO.Path.Combine(Misc.GetApplicationDirectory(), "Errors.txt"), true);
            //Misc.WriteToEventLog(Application.ProductName, "An unhandled exception has occurred: " + Environment.NewLine + Environment.NewLine + exception.ToString(), EventLogEntryType.Error);
            //UserInterface.DisplayErrorMessageBox("An unhandled exception has occurred, check the event log for details");
        }
        #endregion
    }
}
