namespace Project.Gameplay.Knife
{
    public static class KnifeSlicerRuntime
    {
        public static bool IsJudging { get; private set; }
        public static float MinKnifeSpeed { get; private set; }
        public static float SliceCooldownSeconds { get; private set; }
        public static bool UseContactTime { get; private set; }
        public static int MinContactMs { get; private set; }

        public static void SetRuntimeParams(bool isJudging, float minKnifeSpeed, float sliceCooldownSeconds, bool useContactTime, int minContactMs)
        {
            IsJudging = isJudging;
            MinKnifeSpeed = minKnifeSpeed;
            SliceCooldownSeconds = sliceCooldownSeconds;
            UseContactTime = useContactTime;
            MinContactMs = minContactMs;
        }
    }
}
