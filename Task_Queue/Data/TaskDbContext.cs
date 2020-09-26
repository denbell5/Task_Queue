using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task_Queue.Data.Models;

namespace Task_Queue.Data
{
	class TaskDbContext
	{
		public List<TaskClaim> TaskClaims { get; set; } = new List<TaskClaim>
		{
			new TaskClaim
			{
				Id = Guid.NewGuid(),
				Claim = $"Task_0001",
				CreatedAt = DateTimeOffset.Now.AddMinutes(5)
			},
			new TaskClaim
			{
				Id = Guid.NewGuid(),
				Claim = $"Task_0002",
				CreatedAt = DateTimeOffset.Now.AddMinutes(4)
			},
			new TaskClaim
			{
				Id = Guid.NewGuid(),
				Claim = $"Task_0003",
				CreatedAt = DateTimeOffset.Now.AddMinutes(3)
			},
			new TaskClaim
			{
				Id = Guid.NewGuid(),
				Claim = $"Task_0004",
				CreatedAt = DateTimeOffset.Now.AddMinutes(2)
			},
			new TaskClaim
			{
				Id = Guid.NewGuid(),
				Claim = $"Task_0005",
				CreatedAt = DateTimeOffset.Now.AddMinutes(1)
			},
		};

		public List<CustomTask> CustomTasks { get; set; }
	}
}
