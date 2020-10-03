using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task_Queue.Data;
using Task_Queue.Data.Models;

namespace ClaimMenu
{
	class Program
	{
		static TaskDbContext context = new TaskDbContext();

		static async Task Main(string[] args)
		{
			while (true)
			{
				Console.WriteLine("Add new task claim. Syntax: \"Task_1234\"");
				var claimName = Console.ReadLine();
				await AddNewClaim(claimName);
			}
		}

		static async Task AddNewClaim(string claimName)
		{
			if (await context.TaskClaims.AnyAsync(claim => claim.Claim == claimName))
			{
				throw new ArgumentException($"Claim for {claimName} already exists");
			}

			context.TaskClaims.Add(new TaskClaim
			{
				Claim = claimName,
				CreatedAt = DateTime.UtcNow
			});

			await context.SaveChangesAsync();
		}
	}
}
