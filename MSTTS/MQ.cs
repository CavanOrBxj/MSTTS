using System;
using System.Collections.Generic;
using System.Text;

using Apache.NMS;
using Apache.NMS.ActiveMQ;

namespace MSTTS
{
    public struct Property
    {
        public string name;
        public string value;
    }

    class MQ
    {
        private string URI;
        private string TOPIC;
        private string USERNAME;
        private string PASSWORD;
        private IConnectionFactory factory;
        private IConnection connection;
        private ISession session;
        private IMessageProducer producer;

        public string uri
        {
            set { URI = value; }
            get { return URI; }
        }

        public string topic
        {
            set { TOPIC = value; }
            get { return TOPIC; }
        }

        public string username
        {
            set { USERNAME = value; }
        }

        public string password
        {
            set { PASSWORD = value; }
        }

        public MQ()
        {
            producer = null;
            factory = null;
            connection = null;
            session = null;
        }

        ~MQ()
        {
            if (producer != null)
            {
                producer.Dispose();
            }

            Close();
        }

        public void Start()
        {
            factory = new ConnectionFactory(URI);

            if (USERNAME != "")
            {
                connection = factory.CreateConnection(USERNAME, PASSWORD);
            }
            else
            {
                connection = factory.CreateConnection();
            }
            connection.Start();
            session = connection.CreateSession();
            //GlobalFunction.MsgBox("与MQ服务器连接成功!");
        }

        public void Close()
        {
            if (session != null)
            {
                session.Close();
            }
            //if (connection != null)
            if ((connection != null)&&(connection.IsStarted))   //2017-01-22
            {
                connection.Stop();
                connection.Close();
            }
        }

        public void CreateProducer(bool blnTopic, string strTopicName)
        {
            if (blnTopic)
            {
                producer = session.CreateProducer(new Apache.NMS.ActiveMQ.Commands.ActiveMQTopic(strTopicName));
            }
            else
            {
                producer = session.CreateProducer(new Apache.NMS.ActiveMQ.Commands.ActiveMQQueue(strTopicName));
            }
        }

        public void CreateProducer(bool blnTopic)
        {
            if (blnTopic)
            {
                producer = session.CreateProducer(new Apache.NMS.ActiveMQ.Commands.ActiveMQTopic(TOPIC));
            }
            else
            {
                producer = session.CreateProducer(new Apache.NMS.ActiveMQ.Commands.ActiveMQQueue(TOPIC));
            }
        }

        public IMessageConsumer CreateConsumer(bool blnTopic, string strTopicName)
        {
            if (blnTopic)
            {
                return session.CreateConsumer(new Apache.NMS.ActiveMQ.Commands.ActiveMQTopic(strTopicName));
            }
            else
            {
                return session.CreateConsumer(new Apache.NMS.ActiveMQ.Commands.ActiveMQQueue(strTopicName));
            }
        }

        public IMessageConsumer CreateConsumer(bool blnTopic, string strTopicName, string strSelector)
        {
            if (strSelector == "")
            {
               // GlobalFunction.MsgBox("MQ selector不能为空");
                return null;
            }

            if (blnTopic)
            {
                return session.CreateConsumer(new Apache.NMS.ActiveMQ.Commands.ActiveMQTopic(strTopicName), strSelector, false);
            }
            else
            {
                return session.CreateConsumer(new Apache.NMS.ActiveMQ.Commands.ActiveMQQueue(strTopicName), strSelector, false);
            }
        }

        public void SendMQMessage(string strText)
        {
            ITextMessage msg = producer.CreateTextMessage();
            msg.Text = strText;
            producer.Send(msg, Apache.NMS.MsgDeliveryMode.NonPersistent, Apache.NMS.MsgPriority.Normal, TimeSpan.MinValue);
        }

        public void SendMQMessage(string strText, List<Property> lstProperty)
        {
            try
            {
                ITextMessage msg = producer.CreateTextMessage();
                msg.Text = strText;

                foreach (Property prop in lstProperty)
                {
                    msg.Properties.SetString(prop.name, prop.value);
                }
                producer.Send(msg, Apache.NMS.MsgDeliveryMode.NonPersistent, Apache.NMS.MsgPriority.Normal, TimeSpan.MinValue);
            }
            catch (System.Exception ex)
            {
               // GlobalFunction.MsgBoxException(ex.Message, "SendMQMessage");
            }
        }
    }
}
