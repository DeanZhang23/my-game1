namespace NiaIsland.Pier
{
    public static class PierMovement
    {
        public static float Step(float x, float axis, float speed, float deltaTime, float minX, float maxX)
        {
            if (deltaTime < 0f)
                deltaTime = 0f;
            if (minX > maxX)
            {
                var swap = minX;
                minX = maxX;
                maxX = swap;
            }

            var next = x + axis * speed * deltaTime;
            if (next < minX)
                return minX;
            if (next > maxX)
                return maxX;
            return next;
        }
    }
}
