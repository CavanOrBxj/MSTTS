using Apache.NMS;
using SpeechLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MSTTS
{
    public partial class MainForm : Form
    {
        public static IniFiles ini; 
        private IMessageConsumer m_consumer;
        private bool isConn = false; //是否已与MQ服务器正常连接
        public delegate void LogAppendDelegate(string text);
        private System.Timers.Timer tm;
       // private static object _lock = new object();

        public MainForm()
        {
            InitializeComponent();

            //--------------------------只运行一个--------------------------------------------------
            bool flag = CheckSameProcess();
            if (flag)
            {
                MessageBox.Show("只能运行一个程序！", "请确定", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Environment.Exit(0);//退出程序  
               
            }
            this.Load += MainForm_Load;
        }


        /// <summary>
        /// 检查是否存在同名进程
        /// </summary>
        /// <returns></returns>
        private bool CheckSameProcess()
        {
            bool flag = false;
            Process[] ps = Process.GetProcessesByName("MSTTS");
            if (ps.Length>1)
            {
                flag = true;
            }
            return flag;
        }

        void MainForm_Load(object sender, EventArgs e)
        {
            CheckIniConfig();
            if (SingletonInfo.GetInstance().FTPEnable)
            {
                InitFTPServer();
            }
            Thread.Sleep(SingletonInfo.GetInstance().StartDelay);
            LogHelper.WriteLog(typeof(MainForm), "语音服务启动！","4");
            LogMessage("语音服务启动！");
            DealMqConnection();
            //if (isConn)
            //{
                tm = new System.Timers.Timer();
                tm.Interval = SingletonInfo.GetInstance().CheckMQInterval;
                tm.Enabled = true;
                tm.Elapsed += tm_Elapsed;
           // }
            this.Text = "语音服务V_" + Application.ProductVersion;
        }


        private void InitFTPServer()
        {
            string ftpserver = ini.ReadValue("FTPServer", "ftpserver");
            string ftpusername = ini.ReadValue("FTPServer", "ftpusername");
            string ftppwd = ini.ReadValue("FTPServer", "ftppwd");
            SingletonInfo.GetInstance().ftphelper = new FTPHelper(ftpserver, ftpusername, ftppwd);
        }

        void tm_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (GetActiveMQConnection(SingletonInfo.GetInstance().CheckMQURL))
                {
                    //连接正常
                    if (!isConn)
                    {
                        DealMqConnection();
                    }
                }
                else
                {
                    LogMessage("MQ服务器未连接！");
                    LogHelper.WriteLog(typeof(MainForm), "打开MQ网站出错","2");//日志测试  20180319
                    isConn = false;
                    //连接异常
                    m_consumer.Close();
                    if (SingletonInfo.GetInstance().m_mq!=null)
                    {
                        SingletonInfo.GetInstance().m_mq.Close();
                    }
                    SingletonInfo.GetInstance().m_mq = null;
                    GC.Collect();
                }
            }
            catch(Exception  ex) 
            {
                isConn = false;
                //连接异常
                m_consumer.Close();
                if (SingletonInfo.GetInstance().m_mq != null)
                {
                    SingletonInfo.GetInstance().m_mq.Close();
                }
                SingletonInfo.GetInstance().m_mq = null;
                GC.Collect();
            }
        }

        /// <summary>
        /// 处理MQ登录信息
        /// </summary>
        private void DealMqConnection()
        {

            if (OpenMQ(SingletonInfo.GetInstance().MQURL, SingletonInfo.GetInstance().MQUSER, SingletonInfo.GetInstance().MQWD))
            {
                Open_consumer(SingletonInfo.GetInstance().TopicName1);             //创建消息消费者
                SingletonInfo.GetInstance().m_mq.CreateProducer(false, SingletonInfo.GetInstance().TopicName2);//创建消息生产者   //Queue
                LogMessage("MQ连接成功！");
                isConn = true;
            }
            else
            {
                LogMessage("MQ连接失败！");
                isConn = false ;
            }
        }

        /// <summary> 
        /// 追加显示文本 
        /// </summary> 
        /// <param name="color">文本颜色</param> 
        /// <param name="text">显示文本</param> 
        public void LogAppend(string text)
        {
            if (this.IsHandleCreated)
            {
                richTextRebackMsg.AppendText("\n");
                richTextRebackMsg.AppendText(text);
            }
        }


        public void LogMessage(string text)
        {
            LogAppendDelegate la = new LogAppendDelegate(LogAppend);
            richTextRebackMsg.Invoke(la,text);
        }
        
        private bool CheckIniConfig()
        {
            try
            {
                string iniPath = Path.Combine(Application.StartupPath, "MSTTS.ini");
                ini = new IniFiles(iniPath);
                SingletonInfo.GetInstance().MQURL = ini.ReadValue("MQ", "URL");
                SingletonInfo.GetInstance().CheckMQURL = ini.ReadValue("MQ", "MQtestURL");
                SingletonInfo.GetInstance().MQUSER = ini.ReadValue("MQ", "MQUSER");
                SingletonInfo.GetInstance().MQWD = ini.ReadValue("MQ", "MQPWD");
                SingletonInfo.GetInstance().TopicName1 = ini.ReadValue("MQ", "RECTOPIC");
                SingletonInfo.GetInstance().TopicName2 = ini.ReadValue("MQ", "SENDTOPIC");
                SingletonInfo.GetInstance().StartDelay = Convert.ToInt32(ini.ReadValue("MQ", "StartDelay").ToString()) * 1000;
                SingletonInfo.GetInstance()._path = ini.ReadValue("MQ", "path");
                SingletonInfo.GetInstance()._rate = Convert.ToInt32(ini.ReadValue("MQ", "rate"));
                SingletonInfo.GetInstance()._nFrontPackCnt = Convert.ToInt32(ini.ReadValue("MQ", "nFrontPackCnt"));
                SingletonInfo.GetInstance()._nTailPackCnt = Convert.ToInt32(ini.ReadValue("MQ", "nTailPackCnt"));
                SingletonInfo.GetInstance().CheckMQInterval = Convert.ToInt32(ini.ReadValue("MQ", "CheckMQInterval")) * 1000;
                SingletonInfo.GetInstance().FTPEnable = ini.ReadValue("FTPServer", "ftpEnable") == "0" ? false : true;
                SingletonInfo.GetInstance().FaultTime = Convert.ToInt32(ini.ReadValue("FaultTime", "time")) * 60;

                SingletonInfo.GetInstance().IsNationFlag= ini.ReadValue("ProtocalType", "type") == "0" ? false : true;
              
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(typeof(MainForm), "配置文件打开失败","2");//日志测试  20180319
                return false;
            }
            return true;
        }


        private bool OpenMQ(string _MQURL,string _MQUser, string _MQPWD)
        {
            string txtURI = "";
            txtURI = _MQURL;
            try
            {
                SingletonInfo.GetInstance().m_mq = null;//多次连接后会建立多个消费者
                GC.Collect();
                SingletonInfo.GetInstance().m_mq = new MQ();
                SingletonInfo.GetInstance().m_mq.uri = txtURI;
                SingletonInfo.GetInstance().m_mq.username = _MQUser;
                SingletonInfo.GetInstance().m_mq.password = _MQPWD;
                SingletonInfo.GetInstance().m_mq.Start();
                isConn = true;
            }
            catch (System.Exception ex)
            {
                isConn = false;
                LogHelper.WriteLog(typeof(MainForm), "连接MQ服务器异常，请检查端口号、IP地址、用户名及密码是否正确！","2");//日志测试  20180319
            }
            return isConn;
        }

        private void Open_consumer(string _MQRecTopic)
        {
            try
            {
                if (m_consumer != null)
                {
                    m_consumer.Close();
                    m_consumer = null;
                    GC.Collect();
                }
                m_consumer = SingletonInfo.GetInstance().m_mq.CreateConsumer(false, _MQRecTopic);//表示是queue模式 20190215
                m_consumer.Listener += new MessageListener(consumer_listener);
            }
            catch (System.Exception ex)
            {
                LogHelper.WriteLog(typeof(MainForm), "MQ生产者、消费者初始化失败！","2");//日志测试  20180319
            }
        }

        /// <summary>
        /// 消费MQ消息
        /// </summary>
        /// <param name="message"></param>
        private void consumer_listener(IMessage message)
        {
            string strMsg;
            try
            {
                DateTime pp = message.NMSTimestamp;
                ITextMessage msg = (ITextMessage)message;
                strMsg = msg.Text;
                TimeSpan ts1 = new TimeSpan(pp.Ticks);
                TimeSpan ts2 = new TimeSpan(DateTime.Now.Ticks);
                TimeSpan ts3 = ts2.Subtract(ts1); //ts2-ts1
                int sumSeconds = Convert.ToInt32(ts3.TotalSeconds.ToString().Split('.')[0]); //得到相差秒数  
                if (sumSeconds > SingletonInfo.GetInstance().FaultTime) //判断时间差是不是大于给定值
                {
                    LogHelper.WriteLog(typeof(MainForm), "MQ过时信息打印：" + strMsg,"4");
                    return;
                }

                LogHelper.WriteLog(typeof(MainForm), "MQ接收信息打印：" + strMsg,"4");
                LogMessage("MQ接收信息打印：" + strMsg);
                Application.DoEvents();
                ThreadPool.QueueUserWorkItem(new WaitCallback(DealMessage), strMsg);

            }
            catch (System.Exception ex)
            {
                m_consumer.Close();
                LogHelper.WriteLog(typeof(MainForm), "MQ数据处理异常：" + ex.ToString(),"2");
                GC.Collect();
            }
        }

        private void DealMessage(object str)
        {
            try
            {
                string strMsg = (string)str;
                string contenettmp = "";
                string file = "";

                if (strMsg.Contains("PACKTYPE") && strMsg.Contains("CONTENT") && strMsg.Contains("FILE"))//防止误收
                {
                    string[] commandsection = strMsg.Split('|');
                    foreach (string item in commandsection)
                    {
                        if (item.Contains("CONTENT"))
                        {
                            contenettmp = item.Split('~')[1];
                        }
                        if (item.Contains("FILE"))
                        {
                            file = SingletonInfo.GetInstance()._path + "\\" + item.Split('~')[1];
                        }
                    }
                    SaveFile(file, contenettmp);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(typeof(MainForm), "SaveFile：" + ex.ToString(),"2");
            }
        }

        private void SaveFile(string FileName, string Content)
        {
            try
            {
                DelFile(FileName);

                if (SingletonInfo.GetInstance().IsNationFlag)
                {
                    string[] FileNamesp = FileName.Split('.');
                    FileName = FileNamesp[0] + ".wav";

                }
               

                SpeechVoiceSpeakFlags SpFlags = SpeechVoiceSpeakFlags.SVSFlagsAsync;
                SpVoice Voice = new SpVoice();
                Voice.Rate = SingletonInfo.GetInstance()._rate;
                string filename = FileName;
                string filenametmp = FileName.Split('.')[0] + "tmp.wav";

                string texttxt = Content;
                SpeechStreamFileMode SpFileMode = SpeechStreamFileMode.SSFMCreateForWrite;

                SpFileStream SpFileStream = new SpFileStream();

                SpFileStream.Open(filenametmp, SpFileMode, false);

                Voice.AudioOutputStream = SpFileStream;
                Voice.Speak(texttxt, SpFlags);
                Voice.WaitUntilDone(Timeout.Infinite);

                SpFileStream.Close();

                int nFrontPackCnt = SingletonInfo.GetInstance()._nFrontPackCnt;
                int nTailPackCnt = SingletonInfo.GetInstance()._nTailPackCnt;
                int de = NativeMethods.InsertBlankAudio(filename, filenametmp, nFrontPackCnt, nTailPackCnt);


                DelFile(filenametmp);
                FileInfo MyFileInfo = new FileInfo(filename);
                float dirTime = (float)MyFileInfo.Length / 32000;

                string[] filepathname = FileName.Split('\\');
                string filenamesignal = "";
                if (SingletonInfo.GetInstance().IsNationFlag)
                {
                    filenamesignal = filepathname[filepathname.Length - 1].Split('.')[0] + ".mp3";
                }
                else
                {
                    filenamesignal= filepathname[filepathname.Length - 1];
                }
                 

                
                string senddata = "PACKETTYPE~TTS|FILE~" + filenamesignal + "|TIME~" + ((uint)dirTime).ToString();
                LogMessage(senddata);

                #region  如果是需要mp3文件  则需要调用ffmepg
                if (SingletonInfo.GetInstance().IsNationFlag)
                {
                    //转换MP3 
                    string wavfilename = filenamesignal.Replace(".mp3", ".wav");
                    string fromMusic = SingletonInfo.GetInstance()._path + "\\" + wavfilename;//转换音乐路径 
                    string toMusic = SingletonInfo.GetInstance()._path + "\\" + filenamesignal;//转换后音乐路径 
                    int bitrate = 128 * 1000;//恒定码率 
                    string Hz = "44100";//采样频率 
                    ExcuteProcess("ffmpeg.exe", "-y -ab " + bitrate + " -ar " + Hz + " -i \"" + fromMusic + "\" \"" + toMusic + "\"");

                  //  Thread.Sleep(100);
                    File.Delete(fromMusic);
                    //转换完成 
                }
                #endregion


                #region ftp文件传输  20190107新增
                if (SingletonInfo.GetInstance().FTPEnable)
                {
                    string ftppath = filenamesignal;
                    string path = SingletonInfo.GetInstance()._path + "\\" + filenamesignal;
                    SingletonInfo.GetInstance().ftphelper.UploadFile(path, ftppath);//阻塞式非线程模式
                }
                #endregion
                SendMQMessage(senddata);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(typeof(MainForm), "SaveFile处理异常：" + ex.ToString()+"----------"+ex.InnerException+"-------"+ex.StackTrace+"-------"+ex.Message,"2");
            }
        }


        public void ExcuteProcess(string exe, string arg)
        {
            using (var p = new Process())
            {
                p.StartInfo.FileName = exe;
                p.StartInfo.Arguments = arg;
                p.StartInfo.UseShellExecute = false;    //输出信息重定向 
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();                    //启动线程 

                p.WaitForExit();//等待进程结束   
            }
        }


        private void SendMQMessage(string str)
        {
            try
            {
                if (str != null)
                {
                    SingletonInfo.GetInstance().m_mq.SendMQMessage(str);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(typeof(MainForm), "MQ通讯异常:" + ex.ToString(),"2");
            }
        }


        private void DelFile(string str)
        {
            try
            {
                if (File.Exists(str))
                {
                    //存在 
                    File.Delete(str);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(typeof(MainForm), "删除中间文件：" + str + "失败！","2");
            }

        }

        public bool GetActiveMQConnection(string Url)
        {
            bool flag = false;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                request.Proxy = null;
                request.KeepAlive = false;
                request.Method = "GET";
                request.ContentType = "application/json; charset=UTF-8";
                request.AutomaticDecompression = DecompressionMethods.GZip;
                request.Timeout = 5000;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response != null)
                {
                    response.Close();
                    request.Abort();
                    flag = true;
                }
            }
            catch(Exception ex)
            {
                return false;
            }

            return flag;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string pp = "D:\\media\\131999423101079411.mp3";


            string content = "现在是文转语测试现在是文转语测试现在是文转语测试现在是文转语测试现在是文转语测试现在是文转语测试现在是文转语测试现在是文转语测试现在是文转语测试现在是文转语测试现在是文转语测试";

            SaveFile(pp, content);
        }
    }
}
