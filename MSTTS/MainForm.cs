using Apache.NMS;
using SpeechLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
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


      
        private string _path = "";
        private int _rate = 0;

        private string MQIP = "";
        private string MQPORT = "";
        private string MQUSER = "";
        private string MQWD = "";
        private int  StartDelay=0;

        private string TopicName1 = "";
        private string TopicName2 = "";

        private int _nFrontPackCnt = 0;
        private int _nTailPackCnt = 0;

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

            Thread.Sleep(StartDelay);
            LogHelper.WriteLog(typeof(Program), "语音服务启动！");
            LogMessage("语音服务启动！");

            DealMqConnection();
            tm = new System.Timers.Timer();
            tm.Interval = 15000;
            tm.Enabled = true;
            tm.Elapsed += tm_Elapsed;
        }

        void tm_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Open_consumer(TopicName1);  
        }


        private void DealMqConnection()
        {

            if (OpenMQ(MQIP, MQPORT, MQUSER, MQWD))
            {
                Open_consumer(TopicName1);             //创建消息消费者
                m_mq.CreateProducer(false, TopicName2);//创建消息生产者   //Queue
            }
            else
            {
                LogMessage("MQ连接失败！");
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
                MQIP = ini.ReadValue("MQ", "MQIP");
                MQPORT = ini.ReadValue("MQ", "MQPORT");
                MQUSER = ini.ReadValue("MQ", "MQUSER");
                MQWD = ini.ReadValue("MQ", "MQPWD");
                TopicName1 = ini.ReadValue("MQ", "RECTOPIC");
                TopicName2 = ini.ReadValue("MQ", "SENDTOPIC");

                StartDelay = Convert.ToInt32(ini.ReadValue("MQ", "StartDelay").ToString())*1000;

                _path = ini.ReadValue("MQ", "path");
                _rate = Convert.ToInt32(ini.ReadValue("MQ", "rate"));
                _nFrontPackCnt = Convert.ToInt32(ini.ReadValue("MQ", "nFrontPackCnt"));
                _nTailPackCnt = Convert.ToInt32(ini.ReadValue("MQ", "nTailPackCnt"));
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(typeof(MainForm), "配置文件打开失败");//日志测试  20180319
                return false;
            }
            return true;
        }


        private bool OpenMQ(string _MQIP, string _MQPort, string _MQUser, string _MQPWD)
        {
            string txtURI = "";
            txtURI = "tcp://" + _MQIP + ":" + _MQPort;
            try
            {
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
                            file = _path + "\\" + item.Split('~')[1];
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
                Voice.Rate = _rate;
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

                int nFrontPackCnt = _nFrontPackCnt;
                int nTailPackCnt =  _nTailPackCnt;
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
    }
}
