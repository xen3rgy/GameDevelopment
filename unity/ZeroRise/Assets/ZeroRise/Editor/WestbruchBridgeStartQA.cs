namespace ZeroRise.Editor
{
    // Compatibility entry points for the original focused verification commands.
    public static class WestbruchBridgeStartQA
    {
        public static void Run()=>WestbruchOverpassQA.RebuildAndValidate();
        public static void CheckSaved()=>WestbruchOverpassQA.CaptureSaved();
    }
}
