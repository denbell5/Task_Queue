using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Timers = System.Timers;
using Task_Queue.Data;
using Task_Queue.Data.Models;
using Task_Queue.Data.Models.Enums;
using Task_Queue.InternalServices;
using System.Text.RegularExpressions;

namespace TestConsoleApp
{
	class Program
	{
		private static readonly TaskDbContext context = new TaskDbContext();
		private static readonly ILogger logger = new ConsoleLogger();

		static void Main(string[] args)
		{
			context.TaskClaims.Add(new TaskClaim
			{
				Claim = "Task_0000",
				CreatedAt = DateTime.Now
			});
			context.SaveChanges();

			TaskService taskService = new TaskService(
				context,
				logger
			);
			taskService.StartClaimTimer();

			Console.ReadLine();
		}
	}
}
