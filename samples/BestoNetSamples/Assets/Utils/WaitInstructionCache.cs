using System.Collections.Generic;
using UnityEngine;

namespace BestoNetSamples.Utils
{
    public static class WaitInstructionCache
    {
        private static readonly Dictionary<float, WaitForSeconds> TimeIntervalCache = new();
        private static readonly Dictionary<int, WaitForFrames> FrameCountCache = new();
        private static readonly WaitForEndOfFrame CachedWaitForEndOfFrame = new();
        private static readonly WaitForFixedUpdate CachedWaitForFixedUpdate = new();
        
        public static WaitForSeconds Seconds(float seconds)
        {
            if (!TimeIntervalCache.TryGetValue(seconds, out WaitForSeconds wait))
            {
                TimeIntervalCache.Add(seconds, wait = new WaitForSeconds(seconds));
            }
            return wait;
        }
        
        public static WaitForFrames Frames(int frameCount)
        {
            if (!FrameCountCache.TryGetValue(frameCount, out WaitForFrames wait))
            {
                FrameCountCache.Add(frameCount, wait = new WaitForFrames(frameCount));
            }
            return wait;
        }
        
        public static WaitForEndOfFrame EndOfFrame() => CachedWaitForEndOfFrame;
        
        public static WaitForFixedUpdate FixedUpdate() => CachedWaitForFixedUpdate;
    }
    
    public class WaitForFrames : CustomYieldInstruction
    {
        private readonly int _targetFrameCount;
        private readonly int _initialFrameCount;
        
        public WaitForFrames(int frameCount)
        {
            _targetFrameCount = frameCount;
            _initialFrameCount = Time.frameCount;
        }
        
        public override bool keepWaiting => 
            Time.frameCount - _initialFrameCount < _targetFrameCount;
    }
}