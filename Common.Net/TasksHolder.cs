using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Common;

public static class TasksHolder
{
    public static readonly List<Task> _tasks = new(1000);

    public static void Add(Task task) => Add(task, out _);

    public static void Add(Task task, out Task t)
    {
        t = task;
        lock (_tasks)
        {
            _tasks.Add(task);

            task.ContinueWith(static _t => 
            {
                lock (_tasks)
                    _tasks.Remove(_t);
            });
        }
    }

    public static void Add(Func<Task> func, out Task t)
    {
        t = null;
        lock (_tasks)
        {
            t = func();
            _tasks.Add(t);

            t.ContinueWith(static _t =>
            {
                lock (_tasks)
                    _tasks.Remove(_t);
            });
        }
    }
}
