using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public static partial class TaskUtils
    {
        /// <summary>
        /// </summary>
		public static async Task<bool> DoOrTimeout(this Task task, TimeSpan timeout)
		{
			if (task == null || task.IsCompleted)
				return true;

			CancellationTokenSource cts = null;
			Task<Task> taskWhenAny = null;
			if (timeout >= TimeSpan.FromSeconds(10))
			{
				cts = new CancellationTokenSource();
				taskWhenAny = Task.WhenAny(task, Task.Delay(timeout, cts.Token));
			}
			else
			{
				taskWhenAny = Task.WhenAny(task, Task.Delay(timeout));
			}

			if (task == await taskWhenAny.ConfigureAwait(false))
			{
				cts?.Cancel();
				return true;   
			}
			return false;      
		}
    }
}
