using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task_Queue.Data;
using Task_Queue.Data.Models;
using Task_Queue.Data.Models.Enums;

namespace Task_Queue.InternalServices
{
	public class WorkerWrapper
	{
		public delegate void ProgressReportedHandler(WorkerWrapper worker, ProgressChangedEventArgs args);
		public event ProgressReportedHandler ProgressChanged;

		public delegate void WorkCompletedHandler(WorkerWrapper worker, RunWorkerCompletedEventArgs args);
		public event WorkCompletedHandler WorkCompleted;

		private readonly TaskDbContext context;
		private readonly int executionDuration;
		private readonly int progressUpdatePeriod;

		public BackgroundWorker Worker { get; set; }
		public CustomTask Task { get; set; }

		public WorkerWrapper(CustomTask task, TaskDbContext context, int executionDuration, int progressUpdatePeriod = 2000)
		{
			Task = task;
			this.context = context;
			this.executionDuration = executionDuration;
			this.progressUpdatePeriod = progressUpdatePeriod;
			RegisterWorkerEvents();
		}

		private void RegisterWorkerEvents()
		{
			Worker = new BackgroundWorker();

			Worker.WorkerReportsProgress = true;
			Worker.WorkerSupportsCancellation = true;
			
			Worker.ProgressChanged += (s, e) => ProgressChanged(this, e);
			Worker.RunWorkerCompleted += (s, e) => WorkCompleted(this, e);
			Worker.DoWork += this.DoWork;
		}

		public void StartWork() => Worker.RunWorkerAsync();

		private void DoWork(object sender, DoWorkEventArgs e)
		{
			Task.Status = CustomTaskStatus.InProgress;
			context.SaveChanges();

			int updateCount = executionDuration / progressUpdatePeriod;
			int percentagePerUpdate = (int)Math.Round(100.0 / updateCount);

			for (int i = 1; i <= updateCount; i++)
			{
				if (Worker.CancellationPending == true)
				{
					e.Cancel = true;
					break;
				}
				else
				{
					// Perform a time consuming operation and report progress.
					System.Threading.Thread.Sleep(progressUpdatePeriod);
					Task.Progress += percentagePerUpdate;
					context.SaveChanges();

					Worker.ReportProgress(Task.Progress, Task);
				}
			}

			e.Result = Task;
		}
	}
}
