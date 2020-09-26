using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task_Queue.Data.Models.Enums;

namespace Task_Queue.Data.Models
{
	public class CustomTask
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public int Priority { get; set; }
		public CustomTaskStatus Status { get; set; }
		public int Progress { get; set; }
	}
}
