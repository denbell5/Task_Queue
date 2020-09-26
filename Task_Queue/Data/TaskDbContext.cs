using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task_Queue.Data.Models;

namespace Task_Queue.Data
{
	[DbConfigurationType(typeof(MySql.Data.Entity.MySqlEFConfiguration))]
	public class TaskDbContext : DbContext
	{
		public TaskDbContext()
			: base("server=192.168.5.101; port=3306; database=Pinokkio; user=root; password=123456; CharSet=UTF8; Convert Zero Datetime=true;")
		{ }

		public DbSet<TaskClaim> TaskClaims { get; set; }

		public DbSet<CustomTask> CustomTasks { get; set; }
	}
}
