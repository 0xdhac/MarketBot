using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace MarketBot
{
	public delegate void CommandCallback(string[] args);

	public class Commands
	{
		public static Dictionary<string, CommandCallback> RegisteredCommands = new Dictionary<string, CommandCallback>();
		public static void Register(string command, CommandCallback callback)
		{
			RegisteredCommands.Add(command, callback);
		}

		public static bool Execute(string command)
		{
			Regex r = new Regex(@"\w+");
			MatchCollection collection = r.Matches(command);

			if(collection.Count > 0)
			{
				string[] args = new string[collection.Count];
				for(int i = 0; i < args.Length; i++)
				{
					args[i] = collection[i].Value;
				}

				foreach (var cmd in RegisteredCommands)
				{
					if (cmd.Key == args[0])
					{
						CommandCallback cb = cmd.Value;
						cb(args);
						return true;
					}
				}
			}

			return false;
		}
	}
}
