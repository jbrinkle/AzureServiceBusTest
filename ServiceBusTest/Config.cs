using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ServiceBusTest
{
    internal class ConfigOption
    {
        public string FlagId { get; set; }

        public string FlagVerbose { get; set; }

        public string HelpText { get; set; }

        public Action<string> SetOption { get; set; }
    }

    internal static class Config
    {
        const string DefaultServiceBusConnectionString = "{connection string goes here}";
        const string DefaultSendQueueName = "thequeue";
        const string DefaultRespondQueueName = "theotherqueue";

        private static TextWriter stdout;
        public static string ConnectionString { get; private set; } = DefaultServiceBusConnectionString;
        public static string QueueSendName { get; private set; } = DefaultSendQueueName;
        public static string QueueRespondName { get; private set; } = DefaultRespondQueueName;
        public static string[] ActionArgs { get; private set; }
        private static string _action;

        private enum ActionId
        {
            Send,
            Respond,
            Help
        }

        private static List<ConfigOption> configOptions = new List<ConfigOption>
        {
            new ConfigOption { FlagId = "s", FlagVerbose = "sbconn", HelpText = "Service Bus connection string", SetOption = opt => ConnectionString = opt },
            new ConfigOption { FlagId = "n", FlagVerbose = "qname", HelpText = "Name of the queue to use for sending requests", SetOption = opt => QueueSendName = opt },
            new ConfigOption { FlagId = "r", FlagVerbose = "qrespond", HelpText = "Name of queue to use for receiving responses ", SetOption = opt => QueueRespondName = opt }
        };

        public static bool ParseArgs(string[] args, TextWriter output)
        {
            stdout = output;

            if (args.Length < 1)
            {
                WriteErrorMessage("Specify an action");
                return false;
            }

            if (!Enum.TryParse<ActionId>(args[0], true, out ActionId action))
            {
                WriteErrorMessage($"Invalid action: '{args[0]}'");
                return false;
            }
            else _action = action.ToString().ToLower();

            if (args.Length > 1)
            {
                var optionargs = args.Skip(1).ToArray();

                int index = 0;
                dynamic[] optionIdentifiers;
                while (index < optionargs.Length)
                {
                    if (!optionargs[index].StartsWith("-"))
                        break;

                    // remove dashes
                    var nondashchars = optionargs[index].SkipWhile(c => c == '-').ToArray();
                    var optionarg = new string(nondashchars).ToLower();

                    // get list of possible matches based on number of dashes
                    if (optionargs[index].StartsWith("--")) optionIdentifiers = configOptions.Select(co => new { Id = co.FlagVerbose.ToLower(), Opt = co }).ToArray();
                    else if (optionargs[index].StartsWith("-")) optionIdentifiers = configOptions.Select(co => new { Id = co.FlagId.ToLower(), Opt = co }).ToArray();
                    else
                    {
                        WriteErrorMessage($"Unrecognized option: '{optionargs[index]}'");
                        return false;
                    }

                    // get possible match
                    var configOption = (ConfigOption)optionIdentifiers.FirstOrDefault(id => optionarg == id.Id)?.Opt;

                    if (configOption == null)
                    {
                        WriteErrorMessage($"Unrecognized option: '{optionargs[index]}'");
                        return false;
                    }

                    if (index + 1 >= optionargs.Length)
                    {
                        WriteErrorMessage($"Value missing for argument '{optionargs[index]}'");
                        return false;
                    }

                    configOption.SetOption(optionargs[index + 1]);
                    index += 2;
                }

                ActionArgs = index < optionargs.Length ? optionargs.Skip(index).ToArray() : new string[0];
            }
            else ActionArgs = new string[0];

            if (_action.ToLower().Contains("help"))
            {
                if (ActionArgs.Length > 0)
                {
                    if (ActionArgs[0].ToLower() == "send")
                    {
                        stdout.WriteLine(string.Format("Usage: {0} send [options] [number of requests to send]", System.Diagnostics.Process.GetCurrentProcess().ProcessName));
                    }

                    if (ActionArgs[0].ToLower() == "respond")
                    {
                        stdout.WriteLine(string.Format("Usage: {0} respond [options] [number of consumers to run]", System.Diagnostics.Process.GetCurrentProcess().ProcessName));
                    }
                }
                else stdout.WriteLine(GetHelp());

                return false;
            }

            return true;
        }

        public static string GetHelp()
        {
            var help = new StringBuilder();
            help.AppendFormat("Usage: {0} [ 'send' | 'respond' ] [options] [action arguments]", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            help.AppendLine().AppendLine();

            foreach (var option in configOptions)
            {
                help.AppendFormat("\t-{0}\t--{1}\t\t{2}", option.FlagId, option.FlagVerbose, option.HelpText).AppendLine();
            }

            return help.ToString();
        }

        public static bool SendRequest => _action.ToLower().StartsWith("send");

        public static bool SendResponse => _action.ToLower().StartsWith("res");

        private static void WriteErrorMessage(string message)
        {
            stdout.WriteLine("Error: " + message);
        }
    }
}
