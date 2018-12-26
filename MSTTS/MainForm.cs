using Apache.NMS;
using SpeechLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
        private MQ m_mq = null;     
        private IMessageConsumer m_consumer;
        private bool isConn = false; //是否已与MQ服务器正常连接
        public delegate void LogAppendDelegate(string text);
        private System.Timers.Timer tm;

        public MainForm()
        {
            InitializeComponent();
            this.Load += MainForm_Load;
        }

        void MainForm_Load(object sender, EventArgs e)
        {
            CheckIniConfig();

            Thread.Sleep(SingletonInfo.GetInstance().StartDelay);
            LogHelper.WriteLog(typeof(Program), "语音服务启动！");
            LogMessage("语音服务启动！");
            DealMqConnection();
            if (isConn)
            {
                tm = new System.Timers.Timer();
                tm.Interval = SingletonInfo.GetInstance().CheckMQInterval;
                tm.Enabled = true;
                tm.Elapsed += tm_Elapsed;
            }

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
                    //连接异常
                    m_consumer.Close();
                    m_mq.Close();
                    m_mq = null;
                    GC.Collect();
                    DealMqConnection();

                }
            }
            catch 
            {
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
                m_mq.CreateProducer(false, SingletonInfo.GetInstance().TopicName2);//创建消息生产者   //Queue

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
            richTextRebackMsg.AppendText("\n");
            richTextRebackMsg.AppendText(text);
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
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(typeof(MainForm), "配置文件打开失败");//日志测试  20180319
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
                m_mq = null;//多次连接后会建立多个消费者
                GC.Collect();
                m_mq = new MQ();
                m_mq.uri = txtURI;
                m_mq.username = _MQUser;
                m_mq.password = _MQPWD;
                m_mq.Start();
                isConn = true;
            }
            catch (System.Exception ex)
            {
                isConn = false;
                LogHelper.WriteLog(typeof(MainForm), "连接MQ服务器异常，请检查端口号、IP地址、用户名及密码是否正确！");//日志测试  20180319
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
                m_consumer = m_mq.CreateConsumer(false, _MQRecTopic);
                m_consumer.Listener += new MessageListener(consumer_listener);
            }
            catch (System.Exception ex)
            {
                LogHelper.WriteLog(typeof(MainForm), "MQ生产者、消费者初始化失败！");//日志测试  20180319
            }
        }


        private void consumer_listener(IMessage message)
        {
            string strMsg;
            try
            {
                ITextMessage msg = (ITextMessage)message;
                strMsg = msg.Text;
                LogHelper.WriteLog(typeof(Program), "MQ接收信息打印：" + strMsg);
                LogMessage("MQ接收信息打印：" + strMsg);
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
            catch (System.Exception ex)
            {
                m_consumer.Close();
                LogHelper.WriteLog(typeof(Program), "MQ数据处理异常：" + ex.ToString());
                GC.Collect();
            }
        }

        private void SaveFile(string FileName, string Content)
        {
            try
            {

                DelFile(FileName);
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
                string filenamesignal = filepathname[filepathname.Length - 1];
                string senddata = "PACKETTYPE~TTS|FILE~" + filenamesignal + "|TIME~" + ((uint)dirTime).ToString();
                LogMessage(senddata);

                SendMQMessage(senddata);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(typeof(Program), "SaveFile处理异常：" + ex.ToString());
            }
        }


        private void SendMQMessage(string str)
        {
            try
            {
                if (str != null)
                {
                    m_mq.SendMQMessage(str);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(typeof(Program), "MQ通讯异常");
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
                LogHelper.WriteLog(typeof(Program), "删除中间文件：" + str + "失败！");
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

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response != null)
                {
                    response.Close();
                    request.Abort();
                    flag = true;
                }
            }
            catch
            {
                return false;
            }

            return flag;
        }
    }
}
