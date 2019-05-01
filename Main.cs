using MapsetChecks.checks.general.audio;
using MapsetChecks.checks.general.files;
using MapsetChecks.checks.general.metadata;
using MapsetChecks.checks.general.resources;
using MapsetChecks.checks.hit_sounds;
using MapsetChecks.checks.settings;
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

            // General > Metadata
            CheckerRegistry.RegisterCheck(new CheckAdditionalMarkers());
            CheckerRegistry.RegisterCheck(new CheckInconsistentMetadata());
            CheckerRegistry.RegisterCheck(new CheckMarkerFormat());
            CheckerRegistry.RegisterCheck(new CheckMarkerSpacing());
            CheckerRegistry.RegisterCheck(new CheckTVSize());
            CheckerRegistry.RegisterCheck(new CheckUnicode());

            // General > Resources
            CheckerRegistry.RegisterCheck(new CheckBgPresence());
            CheckerRegistry.RegisterCheck(new CheckMultipleVideo());
            CheckerRegistry.RegisterCheck(new CheckVideoOffset());
            CheckerRegistry.RegisterCheck(new CheckBgResolution());
            CheckerRegistry.RegisterCheck(new CheckSpriteResolution());
            CheckerRegistry.RegisterCheck(new CheckVideoResolution());

            // Hit Sounds
            CheckerRegistry.RegisterCheck(new CheckHitSounds());
            CheckerRegistry.RegisterCheck(new CheckMuted());

            // Settings
            CheckerRegistry.RegisterCheck(new CheckDiffSettings());
            CheckerRegistry.RegisterCheck(new CheckInconsistentSettings());
            CheckerRegistry.RegisterCheck(new CheckTickRate());

            // Timing
            CheckerRegistry.RegisterCheck(new CheckUnsnaps());
            CheckerRegistry.RegisterCheck(new CheckWrongSnapping());
            CheckerRegistry.RegisterCheck(new CheckBehindLine());
            CheckerRegistry.RegisterCheck(new CheckFirstLine());
            CheckerRegistry.RegisterCheck(new CheckConcurrentLines());
            CheckerRegistry.RegisterCheck(new CheckPreview());
        }
    }
}
