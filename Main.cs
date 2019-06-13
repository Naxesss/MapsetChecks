using MapsetChecks.checks.compose;
using MapsetChecks.checks.events;
using MapsetChecks.checks.general.audio;
using MapsetChecks.checks.general.files;
using MapsetChecks.checks.general.metadata;
using MapsetChecks.checks.general.resources;
using MapsetChecks.checks.hit_sounds;
using MapsetChecks.checks.settings;
using MapsetChecks.checks.spread;
using MapsetChecks.checks.standard.compose;
using MapsetChecks.checks.standard.settings;
using MapsetChecks.checks.standard.spread;
using MapsetChecks.checks.timing;
using MapsetVerifier;
using System.Globalization;

namespace MapsetChecks
{
    public static class Main
    {
        /// <summary> Needs to be exactly "public static void Run()". This is the start point of the program,
        /// which is executed from the MapsetVerifier application when its .dll file is provided in "/checks". </summary>
        public static void Run()
        {
            // Ensures that numbers are displayed consistently across cultures, for example
            // that decimals are indicated by a period and not a comma.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

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
            CheckerRegistry.RegisterCheck(new CheckUpdateValidity());
            CheckerRegistry.RegisterCheck(new CheckZeroBytes());

            // General > Metadata
            CheckerRegistry.RegisterCheck(new CheckAdditionalMarkers());
            CheckerRegistry.RegisterCheck(new CheckInconsistentMetadata());
            CheckerRegistry.RegisterCheck(new CheckMarkerFormat());
            CheckerRegistry.RegisterCheck(new CheckMarkerSpacing());
            CheckerRegistry.RegisterCheck(new CheckUnicode());
            CheckerRegistry.RegisterCheck(new CheckVersionFormat());

            // General > Resources
            CheckerRegistry.RegisterCheck(new CheckBgPresence());
            CheckerRegistry.RegisterCheck(new CheckBgResolution());
            CheckerRegistry.RegisterCheck(new CheckMultipleVideo());
            CheckerRegistry.RegisterCheck(new CheckSpriteResolution());
            CheckerRegistry.RegisterCheck(new CheckVideoOffset());
            CheckerRegistry.RegisterCheck(new CheckVideoResolution());

            // All Modes > Compose
            CheckerRegistry.RegisterCheck(new CheckAbnormalNodes());
            CheckerRegistry.RegisterCheck(new CheckAudioUsage());
            CheckerRegistry.RegisterCheck(new CheckConcurrent());
            CheckerRegistry.RegisterCheck(new CheckDrainTime());
            CheckerRegistry.RegisterCheck(new CheckInvisibleSlider());

            // All Modes > Events
            CheckerRegistry.RegisterCheck(new CheckBreaks());
            CheckerRegistry.RegisterCheck(new CheckStoryHitSounds());

            // All Modes > Hit Sounds
            CheckerRegistry.RegisterCheck(new CheckHitSounds());
            CheckerRegistry.RegisterCheck(new CheckMuted());

            // All Modes > Settings
            CheckerRegistry.RegisterCheck(new CheckDiffSettings());
            CheckerRegistry.RegisterCheck(new CheckInconsistentSettings());
            CheckerRegistry.RegisterCheck(new CheckTickRate());

            // All Modes > Spread
            CheckerRegistry.RegisterCheck(new CheckLowestDiff());

            // All Modes > Timing
            CheckerRegistry.RegisterCheck(new CheckBehindLine());
            CheckerRegistry.RegisterCheck(new CheckConcurrentLines());
            CheckerRegistry.RegisterCheck(new CheckFirstLine());
            CheckerRegistry.RegisterCheck(new CheckInconsistentLines());
            CheckerRegistry.RegisterCheck(new CheckKiaiUnsnap());
            CheckerRegistry.RegisterCheck(new CheckPreview());
            CheckerRegistry.RegisterCheck(new CheckUnsnaps());
            CheckerRegistry.RegisterCheck(new CheckUnusedLines());
            CheckerRegistry.RegisterCheck(new CheckWrongSnapping());

            // Standard > Compose
            CheckerRegistry.RegisterCheck(new CheckAmbiguity());
            CheckerRegistry.RegisterCheck(new CheckBurai());
            CheckerRegistry.RegisterCheck(new CheckNinjaSpinner());
            CheckerRegistry.RegisterCheck(new CheckObscuredReverse());
            CheckerRegistry.RegisterCheck(new CheckOffscreen());

            // Standard > Settings
            CheckerRegistry.RegisterCheck(new CheckDefaultColours());
            CheckerRegistry.RegisterCheck(new CheckLuminosity());

            // Standard > Spread
            CheckerRegistry.RegisterCheck(new CheckCloseOverlap());
            CheckerRegistry.RegisterCheck(new CheckMultipleReverses());
            CheckerRegistry.RegisterCheck(new CheckShortSliders());
            CheckerRegistry.RegisterCheck(new CheckSpaceVariation());
            CheckerRegistry.RegisterCheck(new CheckSpinnerRecovery());
            CheckerRegistry.RegisterCheck(new CheckStackLeniency());
        }
    }
}
