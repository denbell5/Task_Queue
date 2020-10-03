using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Timers = System.Timers;
using Task_Queue.Data;
using Task_Queue.Data.Models;
using Task_Queue.Data.Models.Enums;
using Task_Queue.InternalServices;
using Task_Queue.Data.Constants;

namespace Task_Queue
{
	public partial class Service1 : ServiceBase
	{
		private readonly TaskService taskService;

		public Service1()
		{
			InitializeComponent();

			taskService = new TaskService(
				new TaskDbContext(),
				new Logger()
			);
		}

		protected override void OnStart(string[] args)
		{
			taskService.StartClaimTimer();
		}

		protected override void OnStop()
		{

		}
	}
}
