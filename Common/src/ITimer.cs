using System;

namespace Lytec.Common;

public interface ITimer
{
    bool IsRunning { get; }
    /// <summary>
    /// 触发时间
    /// </summary>
    int Interval { get; set; }
    /// <summary>
    /// 触发事件
    /// </summary>
    Action OnTimer { get; set; }

    /// <summary>
    /// 在触发定时器后自动停止定时器
    /// </summary>
    bool OneShot { get; set; }

    void Start();
    void Stop();
}

public static class TimerUtils
{
    public static void Restart(this ITimer timer)
    {
        timer.Stop();
        timer.Start();
    }
}
