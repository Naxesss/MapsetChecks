using MapsetChecks.checks.general.audio;
using MapsetChecks.checks.general.files;
using MapsetChecks.checks.timing;
using MapsetVerifier;

namespace MapsetChecks
{
    public class Main
    {
        /// <summary> Needs to be exactly "public static void Run()". This is the start point of the program,
        /// which is executed from the MapsetVerifier application when its .dll file is provided in "/checks". </summary>
        public static void Run()
        {
            // CheckerRegistry is what MapsetVerifier uses to determine which checks to run, so by adding an
            // instance of a check to it, it will be executed and loaded exactly like any other check.

            // Examples
            // CheckerRegistry.RegisterCheck(new CheckTest());
            // CheckerRegistry.RegisterCheck(new GeneralCheckTest());

            // General > Audio
            CheckerRegistry.RegisterCheck(new CheckAudioInVideo());
            CheckerRegistry.RegisterCheck(new CheckBitrate());
            CheckerRegistry.RegisterCheck(new CheckHitSoundDelay());
            CheckerRegistry.RegisterCheck(new CheckHitSoundFormat());
            CheckerRegistry.RegisterCheck(new CheckHitSoundImbalance());
            CheckerRegistry.RegisterCheck(new CheckHitSoundLength());
            CheckerRegistry.RegisterCheck(new CheckMultipleAudio());

            // General > Files
            CheckerRegistry.RegisterCheck(new CheckUnusedFiles());
            CheckerRegistry.RegisterCheck(new CheckUpdateVailidity());

            // Timing
            CheckerRegistry.RegisterCheck(new CheckUnsnaps());
            CheckerRegistry.RegisterCheck(new CheckWrongSnapping());
        }
    }
}
