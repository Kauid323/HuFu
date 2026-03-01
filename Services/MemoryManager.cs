using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace HuFu.Services;

public static class MemoryManager
{
    private static Timer? _gcTimer;
    private const long MemoryThreshold = 100 * 1024 * 1024; // 100MB

    public static void StartMonitoring()
    {
        if (_gcTimer != null) return;

        _gcTimer = new Timer(5000); // 每5秒检查一次
        _gcTimer.Elapsed += OnCheckMemory;
        _gcTimer.AutoReset = true;
        _gcTimer.Enabled = true;
    }

    private static void OnCheckMemory(object? sender, ElapsedEventArgs e)
    {
        long currentMemory = GC.GetTotalMemory(false);
        if (currentMemory > MemoryThreshold)
        {
            // 强制进行完整垃圾回收
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            
            // 尝试释放进程工作集
            EmptyWorkingSet(GetCurrentProcess());
        }
    }

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();
}
