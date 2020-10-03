using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Task_Queue.Data;
using Task_Queue.Data.Constants;
using Task_Queue.Data.Models;
using Task_Queue.Data.Models.Enums;
using Timers = System.Timers;

namespace Task_Queue.InternalServices
{
	public class TaskService
	{
		private readonly ILogger logger;
		private readonly TaskDbContext context;
		private readonly Timers.Timer claimTimer;

		public int ClaimCheckPeriod { get; set; }
		public int ExecutionDuration { get; set; }
		public int ExecutionQuantity { get; set; }

		public TaskService(TaskDbContext context, ILogger logger)
		{
			ClaimCheckPeriod = 1000 * (int)RegistryService.GetParameterValue(
				RegistryParameterKeys.Path,
				RegistryParameterKeys.TaskClaimCheckPeriodKey
			);

			ExecutionDuration = 1000 * (int)RegistryService.GetParameterValue(
				RegistryParameterKeys.Path,
				RegistryParameterKeys.TaskExecutionDurationKey
			);

			ExecutionQuantity = (int)RegistryService.GetParameterValue(
				RegistryParameterKeys.Path,
				RegistryParameterKeys.TaskExecutionQuantityKey
			);

			claimTimer = new Timers.Timer(ClaimCheckPeriod);

			this.logger = logger;
			this.context = context;
		}

		public void StartClaimTimer()
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

			context.TaskClaims.Remove(earliestClaim);
			

			if (!Regex.IsMatch(earliestClaim.Claim, "Task_[0-9]{4}"))
			{
				logger.Log($"Incorrect syntax: {earliestClaim.Claim}.");
				context.SaveChanges();
				CheckClaims();
				return;
			}

			if (context.CustomTasks.Any(task => task.Name == earliestClaim.Claim))
			{
				logger.Log($"Task already exists: {earliestClaim.Claim}.");
				context.SaveChanges();
				CheckClaims();
				return;
			}

			var newTask = new CustomTask
			{
				Name = earliestClaim.Claim,
				Priority = Convert.ToInt32(earliestClaim.Claim.Replace("Task_", "")),
				Status = CustomTaskStatus.Queued
			};

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

			if (inProgressTaskCount >= ExecutionQuantity)
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

			WorkerWrapper worker = new WorkerWrapper(highestPriorityTask, context, ExecutionDuration);
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
