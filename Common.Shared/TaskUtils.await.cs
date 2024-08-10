using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Common;

public static partial class TaskUtils
{
    public static void Ignore(this Task task)
    {
        if (task.IsCompleted)
        {
            _ = task.Exception;
            return;
        }

        task.ContinueWith(static t => _ = t.Exception,
            CancellationToken.None,
            TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    #region AwaitNoErr
    ///// <summary>
    ///// await $task.AwaitNoErr(); // no-throw
    ///// </summary>
    //public static Task AwaitNoErr(this Task task)
    //{
    //    if (task?.IsCompletedSuccessfully != false) return task;
    //    return task.ContinueWith(static t => _ = t.Exception,
    //        CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    //}

    /// <summary>
    /// await $task.AwaitNoErr(); // no-throw
    /// </summary>
    public static AwaitNoErrTaskAwaiter AwaitNoErr(this Task task) => new(task);
    #endregion AwaitNoErr

    public readonly struct AwaitNoErrTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {
        private readonly Task _task;

        public AwaitNoErrTaskAwaiter(Task task) => _task = task;
        public AwaitNoErrTaskAwaiter(Func<Task> func) => _task = Safe(func);

        public AwaitNoErrTaskAwaiter GetAwaiter() => this;

        public bool IsCompleted => _task.IsCompleted;

        public void UnsafeOnCompleted(Action continuation) => _task.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(continuation);
        public void OnCompleted(Action continuation) => _task.ConfigureAwait(false).GetAwaiter().OnCompleted(continuation);

        public void GetResult()
        {
            Debug.Assert(_task.IsCompleted);
            _ = _task.Exception;
        }
    }

    #region AwaitResOrErr
    ///// <summary>
    ///// var (_, ex) = await $task.AwaitResOrErr(); // no-throw
    ///// </summary>
    //public static Task<(object Result, Exception Error)> AwaitResOrErr(this Task task)
    //{
    //    return task.ContinueWith<(object, Exception)>(static t => (null, GetTaskExceptionIncludeCanceled(t)),
    //        CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    //}
    ///// <summary>
    ///// var (r, ex) = await $task.AwaitResOrErr(); // no-throw
    ///// </summary>
    //public static Task<(T Result, Exception Error)> AwaitResOrErr<T>(this Task<T> task)
    //{
    //    return task.ContinueWith(static t => 
    //        {
    //            var ex = GetTaskExceptionIncludeCanceled(t);
    //            return ((ex == null ? t.Result : default), ex);
    //        },
    //        CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    //}

    /// <summary>
    /// var (_, ex) = await $task.AwaitResOrErr(); // no-throw
    /// </summary>
    public static AwaitResOrErrTaskAwaiter AwaitResOrErr(this Task task) => new(task);
    /// <summary>
    /// var (r, ex) = await $task.AwaitResOrErr(); // no-throw
    /// </summary>
    public static AwaitResOrErrTaskAwaiter<T> AwaitResOrErr<T>(this Task<T> task) => new(task);

    public static AwaitResOrErrValueTaskAwaiter AwaitResOrErr(this ValueTask task) => new(ref task);
    public static AwaitResOrErrValueTaskAwaiter<T> AwaitResOrErr<T>(this ValueTask<T> task) => new(ref task);
    #endregion AwaitResOrErr

    public readonly struct AwaitResOrErrTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {
        private readonly Task _task;

        public AwaitResOrErrTaskAwaiter(Task task) => _task = task;
        public AwaitResOrErrTaskAwaiter(Func<Task> func) => _task = Safe(func);

        public AwaitResOrErrTaskAwaiter GetAwaiter() => this;

        public bool IsCompleted => _task.IsCompleted;

        public void UnsafeOnCompleted(Action continuation) => _task.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(continuation);
        public void OnCompleted(Action continuation) => _task.ConfigureAwait(false).GetAwaiter().OnCompleted(continuation);

        public (object Result, Exception Error) GetResult()
        {
            Debug.Assert(_task.IsCompleted);
            var ex = GetTaskExceptionIncludeCanceled(_task);
            return (null, ex);
        }
    }
    public readonly struct AwaitResOrErrTaskAwaiter<T> : ICriticalNotifyCompletion, INotifyCompletion
    {
        private readonly Task<T> _task;

        public AwaitResOrErrTaskAwaiter(Task<T> task) => _task = task;
        public AwaitResOrErrTaskAwaiter(Func<Task<T>> func) => _task = Safe(func);

        public AwaitResOrErrTaskAwaiter<T> GetAwaiter() => this;

        public bool IsCompleted => _task.IsCompleted;

        public void UnsafeOnCompleted(Action continuation) => _task.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(continuation);
        public void OnCompleted(Action continuation) => _task.ConfigureAwait(false).GetAwaiter().OnCompleted(continuation);

        public (T Result, Exception Error) GetResult()
        {
            Debug.Assert(_task.IsCompleted);
            var ex = GetTaskExceptionIncludeCanceled(_task);
            return ((ex == null ? _task.Result : default), ex);
        }
    }

    public readonly struct AwaitResOrErrValueTaskAwaiter : ICriticalNotifyCompletion
    {
        private readonly ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _awaiter;

        public AwaitResOrErrValueTaskAwaiter(ref ValueTask task) => _awaiter = task.ConfigureAwait(false).GetAwaiter();

        public AwaitResOrErrValueTaskAwaiter GetAwaiter() => this;

        public bool IsCompleted => _awaiter.IsCompleted;

        public void UnsafeOnCompleted(Action continuation) => _awaiter.UnsafeOnCompleted(continuation);
        public void OnCompleted(Action continuation) => _awaiter.OnCompleted(continuation);

        public (object Result, Exception Error) GetResult()
        {
            try { _awaiter.GetResult(); }
            catch (Exception ex) { return (null, ex); }
            return default;
        }
    }
    public readonly struct AwaitResOrErrValueTaskAwaiter<T> : ICriticalNotifyCompletion
    {
        private readonly ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter _awaiter;

        public AwaitResOrErrValueTaskAwaiter(ref ValueTask<T> task) => _awaiter = task.ConfigureAwait(false).GetAwaiter();

        public AwaitResOrErrValueTaskAwaiter<T> GetAwaiter() => this;

        public bool IsCompleted => _awaiter.IsCompleted;

        public void UnsafeOnCompleted(Action continuation) => _awaiter.UnsafeOnCompleted(continuation);
        public void OnCompleted(Action continuation) => _awaiter.OnCompleted(continuation);

        public (T Result, Exception Error) GetResult()
        {            
            try { return (_awaiter.GetResult(), null); }
            catch (Exception ex) { return (default, ex); }
        }
    }

    #region Safe(()=>{task}) func没用async语法会直接报错

    public static async Task Safe(Func<Task> func)
    {
        await func();
    }

    public static async Task<T> Safe<T>(Func<Task<T>> func)
    {
        return await func();
    }

    #endregion

    static Exception GetTaskExceptionIncludeCanceled(Task t)
    {
        var ex = t.Exception?.InnerExceptions?.FirstOrDefault() ?? t.Exception;
        if (ex != null) return ex;
        if (t.Status == TaskStatus.Canceled) return new TaskCanceledException(t);
        return null;
    }
}
