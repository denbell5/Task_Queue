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

			this.logger = new Logger();
			this.context = new TaskDbContext();
		}

		protected override void OnStart(string[] args)
		{
			this.logger.Log("Service started");
			this.claimTimer = new Timer(5000);
			this.claimTimer.Elapsed += (s, e) =>
			{
				this.logger.Log("claimTimer elapsed event");
				this.CheckClaims();
			};
			this.claimTimer.Start();
		}

		protected override void OnStop()
		{
			this.claimTimer.Stop();
			this.logger.Log("Service stopped");
		}


		private void StartBackgroundWork(object obj)
		{
			BackgroundWorker worker = new BackgroundWorker();
			worker.WorkerReportsProgress = true;
			worker.WorkerSupportsCancellation = true;
			worker.DoWork += DoWork;
			worker.ProgressChanged += OnProgressChanged;
			worker.RunWorkerCompleted += OnRunWorkerCompleted;
			worker.RunWorkerAsync(obj);
		}

		private void DoWork(object sender, DoWorkEventArgs e)
		{
			this.logger.Log($"Work {this.counter} started");

			var worker = sender as BackgroundWorker;
			var task = e.Argument as CustomTask;
			task.Status = CustomTaskStatus.InProgress;

			for (int i = 1; i <= 10; i++)
			{
				if (worker.CancellationPending == true)
				{
					e.Cancel = true;
					break;
				}
				else
				{
					// Perform a time consuming operation and report progress.
					System.Threading.Thread.Sleep(500);
					task.Progress = i * 10;
					worker.ReportProgress(task.Progress, task);
				}
			}

			e.Result = task;
		}

		private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			this.logger.Log($"{(e.UserState as CustomTask).Name} {e.ProgressPercentage}");
		}

		private void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Cancelled == true)
			{
				this.logger.Log("Canceled!");
			}
			else if (e.Error != null)
			{
				this.logger.Log("Error: " + e.Error.Message);
				this.logger.Log("Error: " + e.Error.StackTrace);
			}
			else
			{
				this.logger.Log("Done!");
				this.logger.Log(e.Result.GetHashCode().ToString());
			}

			var completedTask = e.Result as CustomTask;
			completedTask.Status = CustomTaskStatus.Completed;
			counter++;
			// mark task completed
			this.CheckTasks();
		}


		private void CheckClaims()
		{
			// look in db
			
			// add new task to Tasks
			// this.CheckTasks()

			var earliestDate = this.context.TaskClaims
				.Min(claim => claim.CreatedAt);
			var earliestClaim = this.context.TaskClaims
				.FirstOrDefault(claim => claim.CreatedAt == earliestDate);

			this.context.TaskClaims = this.context.TaskClaims
				.Where(claim => claim.Id != earliestClaim.Id)
				.ToList();
			// TODO: check claim null

			this.logger.Log($"Got earliest Claim {earliestClaim.Claim}");

			// TODO: check claim name syntax

			var newTask = new CustomTask
			{
				Id = Guid.NewGuid(),
				Name = earliestClaim.Claim,
				Priority = Convert.ToInt32(earliestClaim.Claim.Replace("Task_", "")),
				Status = Data.Models.Enums.CustomTaskStatus.Queued
			};

			this.context.CustomTasks = this.context.CustomTasks.Append(newTask);

			this.logger.Log("Before CheckTasks");
			this.CheckTasks();
		}

		private void CheckTasks()
		{
			this.logger.Log("In CheckTasks");
			// look in db
			// check tasks that are in progress
			// if slots are available, get highest priority task
			// start working on that task
			//
			var inProgressTaskCount = this.context.CustomTasks
				.Count(task => task.Status == CustomTaskStatus.InProgress);

			if(inProgressTaskCount > 0)
			{
				this.logger.Log("No available slots for new task to work.");
				return;
			}

			var highestPriorityTask = this.GetHighestPriorityTask();

			if(highestPriorityTask == null)
			{
				this.logger.Log("highestPriorityTask is null.");
				return;
			}

			this.logger.Log($"Got {highestPriorityTask.Name} to work.");
			this.StartBackgroundWork(highestPriorityTask);
		}

		private CustomTask GetHighestPriorityTask()
		{
			var queuedTasks = this.context.CustomTasks
				.Where(task => task.Status == CustomTaskStatus.Queued);

			var highestPriority = queuedTasks.Min(task => task.Priority);

			var highestPriorityTask = queuedTasks
				.FirstOrDefault(task => task.Priority == highestPriority);

			return highestPriorityTask;
		}
	}
}
