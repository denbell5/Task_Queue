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

namespace Task_Queue
{
	public partial class Service1 : ServiceBase
	{
		private readonly TaskDbContext context;
		private readonly ILogger logger;
		private Timers.Timer claimTimer;
		private int counter = 0;

		public Service1()
		{
			InitializeComponent();

			logger = new Logger();
			context = new TaskDbContext();
			claimTimer = new Timers.Timer(5000);
		}

		protected override void OnStart(string[] args)
		{
			Thread claimThread = new Thread((timer) =>
			{
				var claimTimer = timer as Timers.Timer;
				claimTimer.Elapsed += (s, e) =>
				{
					CheckClaims();
				};
				claimTimer.Start();
				CheckClaims();
			});

			claimThread.Start(claimTimer);
		}

		protected override void OnStop()
		{
			claimTimer.Stop();
			logger.Log("Service stopped");
		}


		private void CheckClaims()
		{
			logger.Log("Checking: Claims.");

			if (context.TaskClaims.Count() == 0)
			{
				logger.Log($"Empty: Claims.");
				return;
			}

			var earliestClaim = context.TaskClaims
				.OrderBy(claim => claim.CreatedAt)
				.FirstOrDefault();

			logger.Log($"Got: Claim {earliestClaim.Claim}");

			if (!Regex.IsMatch(earliestClaim.Claim, "Task_[0-9]{4}"))
			{
				logger.Log($"Incorrect syntax: {earliestClaim.Claim}.");
				return;
			}

			if (context.CustomTasks.Any(task => task.Name == earliestClaim.Claim))
			{
				logger.Log($"Task already exists: {earliestClaim.Claim}.");
				return;
			}

			var newTask = new CustomTask
			{
				Name = earliestClaim.Claim,
				Priority = Convert.ToInt32(earliestClaim.Claim.Replace("Task_", "")),
				Status = CustomTaskStatus.Queued
			};

			context.TaskClaims.Remove(earliestClaim);
			context.CustomTasks.Add(newTask);
			context.SaveChanges();
			logger.Log($"Сreated: Task {newTask.Name}.");

			CheckTasks();
		}

		private void CheckTasks()
		{
			logger.Log("Checking: Claims.");

			var inProgressTaskCount = context.CustomTasks.Count(
				task => task.Status == CustomTaskStatus.InProgress
			);

			if (inProgressTaskCount > 0)
			{
				logger.Log("No available slots.");
				return;
			}

			var highestPriorityTask = GetHighestPriorityTask();

			if (highestPriorityTask == null)
			{
				logger.Log("Null: highestPriorityTask.");
				return;
			}

			WorkerWrapper worker = new WorkerWrapper(highestPriorityTask);
			worker.ProgressChanged += OnProgressChanged;
			worker.WorkCompleted += OnRunWorkerCompleted;
			worker.StartWork();

			logger.Log($"Started: Task {worker.Task.Name}");
		}

		private CustomTask GetHighestPriorityTask()
		{
			var queuedTasks = context.CustomTasks.Where(
				task => task.Status == CustomTaskStatus.Queued
			);

			if (queuedTasks.Count() == 0)
			{
				return null;
			}

			var highestPriority = queuedTasks.Min(task => task.Priority);

			var highestPriorityTask = queuedTasks.FirstOrDefault(
				task => task.Priority == highestPriority
			);

			return highestPriorityTask;
		}

		private void OnProgressChanged(WorkerWrapper worker, ProgressChangedEventArgs e)
		{
			logger.Log($"{worker.Task.Name} {worker.Task.Progress}");
		}

		private void OnRunWorkerCompleted(WorkerWrapper worker, RunWorkerCompletedEventArgs e)
		{
			if (e.Cancelled == true)
			{
				logger.Log($"Canceled: {worker.Task.Name}");
			}
			else if (e.Error != null)
			{
				logger.Log("Error: " + e.Error.Message);
				logger.Log("Error: " + e.Error.StackTrace);
			}
			else
			{
				logger.Log($"Done: {worker.Task.Name}");
			}

			worker.Task.Status = CustomTaskStatus.Completed;
			context.SaveChanges();

			CheckTasks();
		}
	}
}
