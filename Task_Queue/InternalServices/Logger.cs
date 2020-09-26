using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_Queue.InternalServices
{
	class Logger
	{
		private object locker = new object();

		public string Path { get; set; } = @"C:\Windows\Logs\TaskQueue.log";
		public string FileName { get; set; }

		public Logger()
		{ }

		public Logger(string path)
		{
			Path = path;
		}

		public void Log(string message)
		{
			lock(locker)
			{
				using (StreamWriter writer = new StreamWriter(this.Path, true))
				{
					writer.WriteLine(DateTime.Now);
					writer.WriteLine(message + "\n");
				}
			}
		}
	}
}
