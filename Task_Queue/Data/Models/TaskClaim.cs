using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_Queue.Data.Models
{
	public class TaskClaim
	{
		public Guid Id { get; set; }
		public string Claim { get; set; }
		public DateTimeOffset CreatedAt { get; set; }
	}
}
