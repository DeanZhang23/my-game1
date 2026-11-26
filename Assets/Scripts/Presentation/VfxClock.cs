namespace Echoes.Presentation
{
    // Hit flash and death puff. One rate for both colorways.
    // 10 fps, 4 frames, 0.4 s one-shot. The clip does not loop.
    public static class VfxClock
    {
        public const float FramesPerSecond = 10f;
        public const int FrameCount = 4;
        public const float Seconds = 0.4f;

        public static bool TryFrame(float elapsed, out int frame)
        {
            frame = 0;
            if (elapsed < 0f)
                elapsed = 0f;
            int index = (int)(elapsed * FramesPerSecond);
            if (index >= FrameCount)
            {
                frame = FrameCount - 1;
                return false;
            }
            frame = index;
            return true;
        }
    }
}
