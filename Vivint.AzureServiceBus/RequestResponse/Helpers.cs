using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Vivint.ServiceBus
{
    public static class Helpers
    {
        public static T DecodeMessage<T>(Message m)
        {
            var json = Encoding.UTF8.GetString(m.Body);
            return Deserialize<T>(json);
        }

        public static Message CreateMessage(object o)
        {
            var json = JsonConvert.SerializeObject(o);
            var bytes = Encoding.UTF8.GetBytes(json);
            return new Message(bytes);
        }

        public static T Deserialize<T>(string body) => DeserializeImpl<T>(body);

        private static T DeserializeImpl<T>(string body, bool firstattempt = true)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(body);
            }
            catch (Exception e)
            {
                if (!firstattempt)
                {
                    throw; 
                }

                // back compat with older ServiceBus library that uses XmlSerialization
                return DeserializeImpl<T>(body.Substring(body.IndexOf(",") + 1), false);
            }
        }
    }
}
