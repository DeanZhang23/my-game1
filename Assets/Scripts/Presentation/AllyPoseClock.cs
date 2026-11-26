namespace Echoes.Presentation
{
    public enum AllyPose
    {
        Idle = 0,
        Walk = 1,
        Attack = 2
    }

    public struct AllyPoseSample
    {
        public AllyPose Pose;
        public int Frame;
        public float Time;
    }

    // One hardcoded rate for all eight official allies. Not per character.
    // Walk loops at 8 fps: each of the 4 frames holds 125 ms (500 ms per cycle)
    // so the stride reads as walking instead of a flicker.
    // Attack is a 4-frame one-shot at that same 8 fps, so the clip lasts 0.5 s.
    // 0.5 s finishes inside the existing attack intervals (melee 1.2 s, ranged 1.5 s).
    // MoveHoldSeconds bridges the sim's 50 ms movement lattice so the walk loop
    // does not blink to idle between steps. It is not a fourth pose.
    public static class AllyPoseClock
    {
        public const float FramesPerSecond = 8f;
        public const int FrameCount = 4;
        public const float MoveHoldSeconds = 0.08f;

        public static float AttackSeconds
        {
            get { return FrameCount / FramesPerSecond; }
        }

        public static int WalkFrame(float elapsed)
        {
            if (elapsed < 0f)
                elapsed = 0f;
            int index = (int)(elapsed * FramesPerSecond);
            int frame = index % FrameCount;
            if (frame < 0)
                frame += FrameCount;
            return frame;
        }

        // True while the one-shot is still showing a frame. False once it has finished
        // (the clip does not loop).
        public static bool TryAttackFrame(float elapsed, out int frame)
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

        // sinceLastStep is presentation time since the sim position last changed.
        // A dead unit is never walking. Before the first step, pass a value above the hold.
        public static bool IsMoving(float sinceLastStep, bool alive)
        {
            if (!alive)
                return false;
            if (sinceLastStep < 0f)
                sinceLastStep = 0f;
            return sinceLastStep <= MoveHoldSeconds;
        }

        // swing restarts the attack one-shot at frame 0 and does not consume dt.
        // A playing attack runs to the end even if the unit is also moving, then the
        // next pose starts at frame 0. Attack never loops.
        public static AllyPoseSample Advance(AllyPose pose, float time, bool moving, bool swing, float dt)
        {
            if (dt < 0f)
                dt = 0f;

            if (swing)
                return Sample(AllyPose.Attack, 0, 0f);

            if (pose == AllyPose.Attack)
            {
                time += dt;
                int frame;
                if (TryAttackFrame(time, out frame))
                    return Sample(AllyPose.Attack, frame, time);
                if (moving)
                    return Sample(AllyPose.Walk, 0, 0f);
                return Sample(AllyPose.Idle, 0, 0f);
            }

            if (moving)
            {
                if (pose != AllyPose.Walk)
                    return Sample(AllyPose.Walk, 0, 0f);
                time += dt;
                return Sample(AllyPose.Walk, WalkFrame(time), time);
            }

            return Sample(AllyPose.Idle, 0, 0f);
        }

        private static AllyPoseSample Sample(AllyPose pose, int frame, float time)
        {
            AllyPoseSample sample = new AllyPoseSample();
            sample.Pose = pose;
            sample.Frame = frame;
            sample.Time = time;
            return sample;
        }
    }
}
