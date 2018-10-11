using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Vivint.ServiceBus
{
    internal static class Helpers
    {
        public static T DecodeMessage<T>(Message m)
        {
            var json = Encoding.UTF8.GetString(m.Body);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static Message CreateMessage(object o)
        {
            var json = JsonConvert.SerializeObject(o);
            var bytes = Encoding.UTF8.GetBytes(json);
            return new Message(bytes);
        }
    }
}
