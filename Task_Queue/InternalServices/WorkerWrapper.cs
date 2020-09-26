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
		public BackgroundWorker Worker { get; set; }
		public CustomTask Task { get; set; }
		public Logger Logger { get; set; }

		private void StartBackgroundWork(object obj)
		{
			Worker = new BackgroundWorker();
			Worker.WorkerReportsProgress = true;
			Worker.WorkerSupportsCancellation = true;
			Worker.DoWork += DoWork;
			Worker.ProgressChanged += OnProgressChanged;
			Worker.RunWorkerCompleted += OnRunWorkerCompleted;
			Worker.RunWorkerAsync(obj);
		}

		private void DoWork(object sender, DoWorkEventArgs e)
		{
			Logger.Log($"Task {Task.Name} started");
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
