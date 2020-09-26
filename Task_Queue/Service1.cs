using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Task_Queue.Data;
using Task_Queue.Data.Models;
using Task_Queue.Data.Models.Enums;
using Task_Queue.InternalServices;

namespace Task_Queue
{
	public partial class Service1 : ServiceBase
	{
		private readonly TaskDbContext context;
		private readonly Logger logger;
		private Timer claimTimer;
		private int counter = 0;

		public Service1()
		{
			InitializeComponent();

			logger = new Logger();
			context = new TaskDbContext();
		}

		protected override void OnStart(string[] args)
		{
			logger.Log("Service started");
			claimTimer = new Timer(5000);
			claimTimer.Elapsed += (s, e) =>
			{
				logger.Log("claimTimer elapsed event");
				CheckClaims();
			};
			claimTimer.Start();
		}

		protected override void OnStop()
		{
			claimTimer.Stop();
			logger.Log("Service stopped");
		}

		private void OnProgressChanged(WorkerWrapper worker, ProgressChangedEventArgs e)
		{
			logger.Log($"{worker.Task.Name} {worker.Task.Progress}");
		}

		private void OnRunWorkerCompleted(WorkerWrapper worker, RunWorkerCompletedEventArgs e)
		{
			if (e.Cancelled == true)
			{
				logger.Log("Canceled!");
			}
			else if (e.Error != null)
			{
				logger.Log("Error: " + e.Error.Message);
				logger.Log("Error: " + e.Error.StackTrace);
			}
			else
			{
				logger.Log("Done!");
				logger.Log(e.Result.GetHashCode().ToString());
			}

			var completedTask = e.Result as CustomTask;
			completedTask.Status = CustomTaskStatus.Completed;
			counter++;
			// mark task completed
			this.CheckTasks();
		}


		private void CheckClaims()
		{
			var earliestDate = context.TaskClaims.Min(
				claim => claim.CreatedAt
			);

			var earliestClaim = context.TaskClaims.FirstOrDefault(
				claim => claim.CreatedAt == earliestDate
			);

			if (earliestClaim == null)
			{
				logger.Log($"No claims in database.");
				return;
			}

			logger.Log($"Got earliest Claim {earliestClaim.Claim}");
			context.TaskClaims.Remove(earliestClaim);
			// TODO: check claim name syntax

			var newTask = new CustomTask
			{
				Id = Guid.NewGuid(),
				Name = earliestClaim.Claim,
				Priority = Convert.ToInt32(earliestClaim.Claim.Replace("Task_", "")),
				Status = CustomTaskStatus.Queued
			};

			context.CustomTasks.Add(newTask);

			logger.Log("Before CheckTasks");
			CheckTasks();
		}

		private void CheckTasks()
		{
			logger.Log("In CheckTasks");
			// look in db
			// check tasks that are in progress
			// if slots are available, get highest priority task
			// start working on that task
			//
			var inProgressTaskCount = context.CustomTasks.Count(
				task => task.Status == CustomTaskStatus.InProgress
			);

			if (inProgressTaskCount > 0)
			{
				logger.Log("No available slots for new task to work.");
				return;
			}

			var highestPriorityTask = GetHighestPriorityTask();

			if(highestPriorityTask == null)
			{
				logger.Log("highestPriorityTask is null.");
				return;
			}

			logger.Log($"Got {highestPriorityTask.Name} to work.");
			WorkerWrapper worker = new WorkerWrapper(highestPriorityTask);
			worker.ProgressChanged += OnProgressChanged;
			worker.WorkCompleted += OnRunWorkerCompleted;
			worker.StartWork();
			logger.Log($"Task {worker.Task} started");
		}

		private CustomTask GetHighestPriorityTask()
		{
			var queuedTasks = this.context.CustomTasks.Where(
				task => task.Status == CustomTaskStatus.Queued
			);

			var highestPriority = queuedTasks.Min(task => task.Priority);

			var highestPriorityTask = queuedTasks.FirstOrDefault(
				task => task.Priority == highestPriority
			);

			return highestPriorityTask;
		}
	}
}
