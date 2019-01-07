using System.Threading;
using System.Collections.Generic;
using System.Data;

namespace MSTTS
{
    public class SingletonInfo
    {
        private static SingletonInfo _singleton;


        public bool  IsMQConnection;//MQ连接是否正常
        public string _path;// 生成的音频文件保存路径
        public int _rate;//

        public string MQURL;// = "";
        public string CheckMQURL;
        public string MQUSER;//= "";
        public string MQWD;//= "";
        public int StartDelay;// 启动延迟时间

        public string TopicName1;// = "";
        public string TopicName2;// = "";

        public int _nFrontPackCnt;//生成的音频文件的开始的空白期的长度
        public int _nTailPackCnt;// 生成的音频文件的结束的空白期的长度

        public int CheckMQInterval;//检查MQ是否工作的周期
        public bool FTPEnable;//FTP传输使能


        private SingletonInfo()                                                                 
        {
            IsMQConnection = false;
            _path = "";
            MQURL = "";
            CheckMQURL = "";
            MQUSER = "";
            MQWD = "";
            TopicName1 = "";
            TopicName2 = "";
            _rate = 0;
            StartDelay = 0;
            _nFrontPackCnt = 0;
            _nTailPackCnt = 0;
            CheckMQInterval = 0;
            FTPEnable = false;
        }
        public static SingletonInfo GetInstance()
        {
            if (_singleton == null)
            {
                Interlocked.CompareExchange(ref _singleton, new SingletonInfo(), null);
            }
            return _singleton;
        }
    }
}