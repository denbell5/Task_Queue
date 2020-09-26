using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task_Queue.Data.Models;
using Task_Queue.Data.Models.Enums;

namespace Task_Queue.InternalServices
{
	class WorkerWrapper
	{
		public delegate void ProgressReportedHandler(WorkerWrapper worker, ProgressChangedEventArgs args);
		public event ProgressReportedHandler ProgressChanged;

		public delegate void WorkCompletedHandler(WorkerWrapper worker, RunWorkerCompletedEventArgs args);
		public event WorkCompletedHandler WorkCompleted;

		public BackgroundWorker Worker { get; set; }
		public CustomTask Task { get; set; }
		public Logger Logger { get; set; }

		public WorkerWrapper(CustomTask task)
		{
			Task = task; 
			
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

			for (int i = 1; i <= 10; i++)
			{
				if (Worker.CancellationPending == true)
				{
					e.Cancel = true;
					break;
				}
				else
				{
					// Perform a time consuming operation and report progress.
					System.Threading.Thread.Sleep(500);
					Task.Progress = i * 10;
					Worker.ReportProgress(Task.Progress, Task);
				}
			}

			e.Result = Task;
		}
	}
}
