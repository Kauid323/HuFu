using System;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Timers;

namespace HuFu.Services;

public static class MemoryManager
{
    private static Timer? _gcTimer;
    private const long MemoryThreshold = 50 * 1024 * 1024; // 50MB 强制清理阈值
    private const long CriticalThreshold = 100 * 1024 * 1024; // 100MB 严重阈值
    private const int CheckIntervalMs = 5000; // 5秒检查一次

    public static void StartMonitoring()
    {
        if (_gcTimer != null) return;

        // 配置 GC 为服务器模式和低延迟
        GCSettings.LatencyMode = GCLatencyMode.Interactive;

        _gcTimer = new Timer(CheckIntervalMs);
        _gcTimer.Elapsed += OnCheckMemory;
        _gcTimer.AutoReset = true;
        _gcTimer.Enabled = true;

        System.Diagnostics.Debug.WriteLine("MemoryManager: Monitoring started");
    }

    public static void StopMonitoring()
    {
        if (_gcTimer != null)
        {
            _gcTimer.Stop();
            _gcTimer.Dispose();
            _gcTimer = null;
            System.Diagnostics.Debug.WriteLine("MemoryManager: Monitoring stopped");
        }
    }

    private static void OnCheckMemory(object? sender, ElapsedEventArgs e)
    {
        try
        {
            long currentMemory = GC.GetTotalMemory(false);
            long workingSet = Environment.WorkingSet;

            System.Diagnostics.Debug.WriteLine($"MemoryManager: Managed={currentMemory / 1024 / 1024}MB, WorkingSet={workingSet / 1024 / 1024}MB");

            // 严重内存压力：立即执行完整 GC
            if (currentMemory > CriticalThreshold)
            {
                System.Diagnostics.Debug.WriteLine("MemoryManager: Critical memory pressure detected, forcing aggressive GC");
                PerformFullGC();
            }
            // 超过 80MB：强制执行优化 GC
            else if (currentMemory > MemoryThreshold)
            {
                System.Diagnostics.Debug.WriteLine($"MemoryManager: Memory exceeded {MemoryThreshold / 1024 / 1024}MB threshold, forcing cleanup");
                PerformOptimizedGC();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MemoryManager: Error during memory check: {ex.Message}");
        }
    }

    private static void PerformOptimizedGC()
    {
        // 温和的 GC：只回收 Gen0 和 Gen1
        GC.Collect(1, GCCollectionMode.Optimized, false);
        
        long afterGC = GC.GetTotalMemory(false);
        System.Diagnostics.Debug.WriteLine($"MemoryManager: Optimized GC completed, memory={afterGC / 1024 / 1024}MB");
    }

    private static void PerformFullGC()
    {
        // 完整 GC：回收所有代
        GC.Collect(2, GCCollectionMode.Aggressive, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);

        // 压缩大对象堆
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

        // 尝试释放进程工作集（仅 Windows）
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                EmptyWorkingSet(GetCurrentProcess());
            }
            catch
            {
                // 忽略失败
            }
        }

        long afterGC = GC.GetTotalMemory(false);
        System.Diagnostics.Debug.WriteLine($"MemoryManager: Full GC completed, memory={afterGC / 1024 / 1024}MB");
    }

    /// <summary>
    /// 手动触发内存清理（可在页面切换等场景调用）
    /// </summary>
    public static void RequestCleanup()
    {
        System.Diagnostics.Debug.WriteLine("MemoryManager: Manual cleanup requested");
        PerformOptimizedGC();
    }

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();
}
