using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lytec.Common.Threading
{
    public class LoopTask : IDisposable
    {
        private const int DefaultInterval = 500;

        /// <summary>
        /// 是否已被销毁
        /// </summary>
        public bool IsDisposed { get; protected set; }

        /// <summary>
        /// 内部任务
        /// </summary>
        protected Task? Task { get; private set; }

        /// <summary>
        /// 停止任务Token源
        /// </summary>
        protected CancellationTokenSource CancelTokenSource { get; set; }
        protected CancellationTokenSource? PauseTokenSource { get; set; }

        /// <summary>
        /// 暂停/继续任务事件源
        /// </summary>
        protected ManualResetEvent ResetEvent { get; }

        /// <summary>
        /// 是否繁忙中
        /// </summary>
        public bool IsBusy { get; protected set; }

        public bool IsAlive { get; protected set; } = true;

        public bool IsRunning { get; protected set; }

        /// <summary>
        /// 慢循环的间隔时间（毫秒）
        /// </summary>
        public int Interval { get; set; }

        public ThreadPriority Priority { get; set; }

        /// <summary>
        /// 创建无限循环任务
        /// </summary>
        /// <param name="interval">慢循环的等待（间隔）时间（毫秒）</param>
        /// <param name="startNow">立即启动任务</param>
        protected LoopTask(int interval = DefaultInterval, bool startNow = false)
        {
            Interval = interval;
            CancelTokenSource = new CancellationTokenSource();
            ResetEvent = new ManualResetEvent(startNow);
        }

        private void StartTask(Func<CancellationToken?, Task<bool>> Func)
        {
            Task = Task.Factory.StartNew(async () =>
            {
                var originPriority = Thread.CurrentThread.Priority;
                try
                {
                    var priorityCache = Priority;
                    while (!CancelTokenSource.Token.IsCancellationRequested)
                    {
                        if (!ResetEvent.WaitOne(50) || !IsRunning)
                        {
                            if (IsBusy && (PauseTokenSource?.IsCancellationRequested ?? true))
                                IsBusy = false;
                            continue;
                        }
                        if (priorityCache != Priority)
                            Thread.CurrentThread.Priority = priorityCache = Priority;
                        if (CancelTokenSource.Token.IsCancellationRequested)
                            break;
                        IsBusy = true;
                        try
                        {
                            if (!await Func(PauseTokenSource?.Token))
                            {
                                IsBusy = false;
                                await Task.Delay(Interval);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            IsBusy = false;
                        }
                    }
                }
                finally
                {
                    Thread.CurrentThread.Priority = originPriority;
                    IsAlive = false;
                }
            }, CancelTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        /// <summary>
        /// 创建无限循环任务
        /// </summary>
        /// <param name="Func">要循环执行的任务，任务返回true则立即进入下一次循环，否则等待一定时间再进入</param>
        /// <param name="interval">慢循环的等待（间隔）时间（毫秒）</param>
        /// <param name="startNow">立即启动任务</param>
        public LoopTask(Func<bool> Func, int interval = DefaultInterval, bool startNow = false) : this(cancel => Func(), interval, startNow) { }
        /// <summary>
        /// 创建无限循环任务
        /// </summary>
        /// <param name="Func">要循环执行的任务，任务返回true则立即进入下一次循环，否则等待一定时间再进入</param>
        /// <param name="interval">慢循环的等待（间隔）时间（毫秒）</param>
        /// <param name="startNow">立即启动任务</param>
        public LoopTask(Func<Task<bool>> Func, int interval = DefaultInterval, bool startNow = false) : this(cancel => Func(), interval, startNow) { }
        /// <summary>
        /// 创建无限循环任务
        /// </summary>
        /// <param name="Action">要循环执行的任务</param>
        /// <param name="startNow">立即启动任务</param>
        public LoopTask(Action<CancellationToken?> Action, bool startNow = false) : this(cancel => { Action(cancel); return true; }, DefaultInterval, startNow) { }
        /// <summary>
        /// 创建无限循环任务
        /// </summary>
        /// <param name="Action">要循环执行的任务</param>
        /// <param name="startNow">立即启动任务</param>
        public LoopTask(Action Action, bool startNow = false) : this(cancel => Action(), startNow) { }
        /// <summary>
        /// 创建慢无限循环任务
        /// </summary>
        /// <param name="Action">要循环执行的任务</param>
        /// <param name="interval">慢循环的等待（间隔）时间（毫秒）</param>
        /// <param name="startNow">立即启动任务</param>
        public LoopTask(Action<CancellationToken?> Action, int interval, bool startNow = false) : this(cancel => { Action(cancel); return false; }, interval, startNow) { }
        /// <summary>
        /// 创建慢无限循环任务
        /// </summary>
        /// <param name="Action">要循环执行的任务</param>
        /// <param name="interval">慢循环的等待（间隔）时间（毫秒）</param>
        /// <param name="startNow">立即启动任务</param>
        public LoopTask(Action Action, int interval, bool startNow = false) : this(cancel => Action(), interval, startNow) { }
        /// <summary>
        /// 创建无限循环任务
        /// </summary>
        /// <param name="Action">要循环执行的任务</param>
        /// <param name="startNow">立即启动任务</param>
        public LoopTask(Func<Task> Action, bool startNow = false) : this(async cancel => { await Action(); return true; }, DefaultInterval, startNow) { }
        /// <summary>
        /// 创建慢无限循环任务
        /// </summary>
        /// <param name="Action">要循环执行的任务</param>
        /// <param name="interval">慢循环的等待（间隔）时间（毫秒）</param>
        /// <param name="startNow">立即启动任务</param>
        public LoopTask(Func<Task> Action, int interval, bool startNow = false) : this(async cancel => { await Action(); return false; }, interval, startNow) { }

        /// <summary>
        /// 创建无限循环任务
        /// </summary>
        /// <param name="Func">要循环执行的任务，任务返回true则立即进入下一次循环，否则等待一定时间再进入</param>
        /// <param name="interval">慢循环的等待（间隔）时间（毫秒）</param>
        /// <param name="startNow">立即启动任务</param>
        public LoopTask(Func<CancellationToken?, Task<bool>> Func, int interval = DefaultInterval, bool startNow = false) : this(interval, startNow)
        => StartTask(Func);

        /// <summary>
        /// 创建无限循环任务
        /// </summary>
        /// <param name="Func">要循环执行的任务，任务返回true则立即进入下一次循环，否则等待一定时间再进入</param>
        /// <param name="interval">慢循环的等待（间隔）时间（毫秒）</param>
        /// <param name="startNow">立即启动任务</param>
        public LoopTask(Func<CancellationToken?, bool> Func, int interval = DefaultInterval, bool startNow = false) : this(interval, startNow)
        => StartTask(async c =>
        {
            await Task.CompletedTask;
            return Func(c);
        });

        /// <summary>
        /// 启动/继续任务，此函数与<see cref="Resume"/>等效
        /// </summary>
        public void Start() => Resume();
        /// <summary>
        /// 停止任务并释放所有资源，此函数与<see cref="IDisposable.Dispose"/>等效
        /// </summary>
        public void Stop() => Dispose();
        /// <summary>
        /// 启动/继续任务，此函数与<see cref="Start"/>等效
        /// </summary>
        public void Resume()
        {
            if (IsRunning)
                return;
            var src = new CancellationTokenSource();
            (PauseTokenSource, src) = (src, PauseTokenSource);
            src?.Dispose();
            ResetEvent.Set();
            IsRunning = true;
        }
        /// <summary>
        /// 暂停任务
        /// </summary>
        public void Pause()
        {
            if (!IsRunning)
                return;
            PauseTokenSource?.Cancel();
            ResetEvent.Reset();
            while (IsBusy)
                Thread.Sleep(100);
            PauseTokenSource?.Dispose();
            PauseTokenSource = null;
            IsRunning = false;
        }

        #region IDisposable

        /// <summary>
        /// 停止任务并释放使用的所有资源
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    CancelTokenSource.Cancel();
                    Resume();
                    while (IsAlive)
                        Thread.Sleep(100);
                    CancelTokenSource.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                IsDisposed = true;
            }
        }

        // // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~LoopTask()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        /// <summary>
        /// 停止任务并释放使用的所有资源
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
