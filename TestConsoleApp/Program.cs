using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Task_Queue.Data;
using Task_Queue.Data.Models;
using Task_Queue.Data.Models.Enums;
using Task_Queue.InternalServices;

namespace TestConsoleApp
{
	class Program
	{
		private static readonly TaskDbContext context = new TaskDbContext();
		private static readonly ILogger logger = new ConsoleLogger();
		private static Timer claimTimer = new Timer(5000);

		static void Main(string[] args)
		{
			claimTimer.Elapsed += (s, e) =>
			{
				logger.Log("claimTimer elapsed event");
				CheckClaims();
			};
			claimTimer.Start();

			Console.ReadLine();
		}

		static void CheckClaims()
		{
			if (context.TaskClaims.Count() == 0)
			{
				logger.Log($"No claims in database.");
				return;
			}

			var earliestDate = context.TaskClaims.Min(
				claim => claim.CreatedAt
			);

			var earliestClaim = context.TaskClaims.FirstOrDefault(
				claim => claim.CreatedAt == earliestDate
			);

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
			CheckTasks();
		}

		private static void CheckTasks()
		{
			var inProgressTaskCount = context.CustomTasks.Count(
				task => task.Status == CustomTaskStatus.InProgress
			);

			if (inProgressTaskCount > 0)
			{
				logger.Log("No available slots for new task to work.");
				return;
			}

			var highestPriorityTask = GetHighestPriorityTask();

			if (highestPriorityTask == null)
			{
				logger.Log("highestPriorityTask is null.");
				return;
			}

			WorkerWrapper worker = new WorkerWrapper(highestPriorityTask);
			worker.ProgressChanged += OnProgressChanged;
			worker.WorkCompleted += OnRunWorkerCompleted;
			worker.StartWork();
			logger.Log($"Task {worker.Task} started");
		}

		private static CustomTask GetHighestPriorityTask()
		{
			var queuedTasks = context.CustomTasks.Where(
				task => task.Status == CustomTaskStatus.Queued
			);

			if(queuedTasks.Count() == 0)
			{
				return null;
			}

			var highestPriority = queuedTasks.Min(task => task.Priority);

			var highestPriorityTask = queuedTasks.FirstOrDefault(
				task => task.Priority == highestPriority
			);

			return highestPriorityTask;
		}

		private static void OnProgressChanged(WorkerWrapper worker, ProgressChangedEventArgs e)
		{
			logger.Log($"{worker.Task.Name} {worker.Task.Progress}");
		}

		private static void OnRunWorkerCompleted(WorkerWrapper worker, RunWorkerCompletedEventArgs e)
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
				logger.Log($"Done {worker.Task.Name}");
			}

			worker.Task.Status = CustomTaskStatus.Completed;
			CheckTasks();
		}
	}
}
