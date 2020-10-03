using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_Queue.Data.Constants
{
	public static class RegistryParameterKeys
	{
		public const string Path = @"SOFTWARE\Task_Queue\Parameters";

		public const string TaskClaimCheckPeriodKey = "Task_Claim_Check_Period";
		public const string TaskExecutionDurationKey = "Task_Execution_Duration";
		public const string TaskExecutionQuantityKey = "Task_Execution_Quantity";
	}
}
