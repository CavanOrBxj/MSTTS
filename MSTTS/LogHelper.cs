using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace MSTTS
{
    public class LogHelper
    {
        /// <summary>
        /// 输出日志到Log4Net
        /// </summary>
        /// <param name="t"></param>
        /// <param name="ex"></param>
        #region static void WriteLog(Type t, Exception ex)

        public static void WriteLog(Type t, Exception ex)
        {
            log4net.ILog log = log4net.LogManager.GetLogger(t);
            log.Error("Error", ex);
        }

        #endregion

        /// <summary>
        /// 输出日志到Log4Net
        /// </summary>
        /// <param name="t"></param>
        /// <param name="msg"></param>
        /// <param name="type">1-debug 2-Error 3-Fatal 4-Info 5-Warn </param>
        #region static void WriteLog(Type t, string msg,string type)
        public static void WriteLog(Type t, string msg,string type)
        {
            log4net.ILog log = log4net.LogManager.GetLogger(t);
            switch (type)
            {
                case "1":
                    log.Debug(msg);
                    break;

                case "2":
                    log.Error(msg);
                    break;

                case "3":
                    log.Fatal(msg);
                    break;

                case "4":
                    log.Info(msg);
                    break;

                case "5":
                    log.Warn(msg);
                    break;
            }

        }

        #endregion


    }
}
