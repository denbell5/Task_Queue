using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_Queue.Data.Models
{
	public class TaskClaim
	{
		public int Id { get; set; }
		public string Claim { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
