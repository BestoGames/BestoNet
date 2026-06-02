/*
 *  Author: Rin Iota
 *  Class Description: More accurately locks Unity game's FPS to a stable 60FPS, 16.666ms, without use of Thread.Sleep.
 */

using UnityEngine;
using UnityEngine.LowLevel;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Diagnostics;
using System.Threading;

public class RateLockStart {}
public class RateLockEnd {}

public class FPSLock : MonoBehaviour
{
    private const long FRAME_DUR_TARGET = 166600;
    private long FrameExtension = 0;

    public bool EnableRateLock = false;
    public long FrameStartTime = 0;
    public static FPSLock Instance;
    private Stopwatch stopwatch = new Stopwatch();

    private long next;

    private PlayerLoopSystem playerLoop;
    private List<string> excludeSystems = new List<string>{
        
    };

    private void Awake()
    {
        // Create rate locking systems
        var rateLockStart = new PlayerLoopSystem
        {
            subSystemList = null,
            updateDelegate = RateLockStart,
            type = typeof(RateLockStart),
        };
        var rateLockEnd = new PlayerLoopSystem
        {
            subSystemList = null,
            updateDelegate = RateLockEnd,
            type = typeof(RateLockEnd),
        };

        // Create a holder for custom player loop system list
        List<PlayerLoopSystem> newSystemList = new List<PlayerLoopSystem>();

        // Get default system
        PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());
        playerLoop = PlayerLoop.GetCurrentPlayerLoop();
        
        //// Add Systems
        {
            newSystemList.Add(rateLockStart);

            // Filter out unneeded default systems (like XR and shit)
            for (int i = 0; i < playerLoop.subSystemList.Length; i++)
                playerLoop.subSystemList[i].subSystemList = ReturnSystemsFiltered(playerLoop.subSystemList[i].subSystemList);
            
            // Default systems 
            for (int i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                newSystemList.Add(playerLoop.subSystemList[i]);
            }

            // 
            newSystemList.Add(rateLockEnd);
        }
        ////
        ///

        // Put our new list in to the player loop
        playerLoop.subSystemList = newSystemList.ToArray();

        PlayerLoop.SetPlayerLoop(playerLoop);

        Instance = this;

        if (Stopwatch.IsHighResolution == false)
        {
            EnableRateLock = false;
        }

        // Comment
        QualitySettings.vSyncCount = 0;
        GarbageCollector.incrementalTimeSliceNanoseconds = 3000;

        if (EnableRateLock)
        {
            Application.targetFrameRate = 0;
            stopwatch = new Stopwatch();
            stopwatch.Start();
            GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
            QualitySettings.maxQueuedFrames = 0;
        }
        else
        {
            Application.targetFrameRate = 60;
            GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
        }

    }
    private void RateLockStart()
    {   
        FrameStartTime = stopwatch.ElapsedTicks;
    }

    private void RateLockEnd()
    {
        if (EnableRateLock)
        {   
            if (stopwatch.IsRunning == false)
            {
                Application.targetFrameRate = 0;
                stopwatch.Start();
                next = 0;
                // GarbageCollector.GCMode = GarbageCollector.Mode.Manual;
            }

            // if (next - stopwatch.ElapsedTicks > 4000)
            // {
            //     GarbageCollector.CollectIncremental(1000);
            // }
            
            while(EnableRateLock && stopwatch.ElapsedTicks < next) {}

            next = stopwatch.ElapsedTicks + FRAME_DUR_TARGET + FrameExtension;
        }
        else
        {
            if (Application.targetFrameRate != 60)
            {
                stopwatch.Stop();
                Application.targetFrameRate = 60;
                GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
            }
        }
    }

    private PlayerLoopSystem[] ReturnSystemsFiltered(PlayerLoopSystem[] thisSystemList)
    {
        List<PlayerLoopSystem> newSystemList = new List<PlayerLoopSystem>();

        foreach(PlayerLoopSystem system in thisSystemList)
            if (excludeSystems.Contains(system.ToString()) == false)
                newSystemList.Add(system);

        return newSystemList.ToArray();
    }

    private void OnDestroy()
    {
        stopwatch.Stop();
        stopwatch = null;
        PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());
        EnableRateLock = false;
    }

    public long TimeFromFrameStartToNow()
    {
        return stopwatch.ElapsedTicks - FrameStartTime;
    }

    public long TheoreticallyTimeLeft()
    {
        return FRAME_DUR_TARGET - TimeFromFrameStartToNow();
    }

    public long FramesToTime(int frames)
    {
        return FRAME_DUR_TARGET * (long)frames;
    }

    public void SetFrameExtension(long microseconds)
    {
        FrameExtension = microseconds;
    }
}
