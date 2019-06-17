using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using ManagedBass;

namespace MapsetChecks.objects
{
    public static class Audio
    {
        private static readonly ConcurrentDictionary<string, object> locks = new ConcurrentDictionary<string, object>();
        private static bool isInitialized = false;
        
        private static void Initialize()
        {
            if (!isInitialized)
            {
                // 0 = No Output Device
                if (!Bass.Init(0))
                    throw new BadImageFormatException(
                        $"Could not initialize ManagedBass, error \"{Bass.LastError}\".");

                isInitialized = true;
            }
        }

        private static int CreateStream(string aFileName)
        {
            Initialize();

            int stream = Bass.CreateStream(aFileName, 0, 0, BassFlags.Decode);
            if (stream == 0)
                throw new BadImageFormatException(
                    $"Could not get audio duration of \"{aFileName}\", error \"{Bass.LastError}\".");

            return stream;
        }

        private static void FreeStream(int aStream)
        {
            Bass.StreamFree(aStream);
        }

        /// <summary> Returns the format of the audio file (e.g. mp3, wav, etc). </summary>
        public static ChannelType GetFormat(string aFileName)
        {
            // Implements a queue to prevent race conditions since Bass is a static library.
            // Also prevents deadlocks through using new object() rather than the file name itself.
            lock (locks.GetOrAdd(aFileName, new object()))
            {
                int stream = CreateStream(aFileName);
                Bass.ChannelGetInfo(stream, out ChannelInfo channelInfo);

                FreeStream(stream);
                return channelInfo.ChannelType;
            }
        }

        /// <summary> Returns the channel amount (1 = mono, 2 = stereo, etc). </summary>
        public static int GetChannels(string aFileName)
        {
            lock (locks.GetOrAdd(aFileName, new object()))
            {
                int stream = CreateStream(aFileName);
                Bass.ChannelGetInfo(stream, out ChannelInfo channelInfo);

                FreeStream(stream);
                return channelInfo.Channels;
            }
        }

        /// <summary> Returns the audio duration in ms. </summary>
        public static double GetDuration(string aFileName)
        {
            lock (locks.GetOrAdd(aFileName, new object()))
            {
                int    stream  = CreateStream(aFileName);
                long   length  = Bass.ChannelGetLength(stream);
                double seconds = Bass.ChannelBytes2Seconds(stream, length);

                FreeStream(stream);
                return seconds * 1000;
            }
        }

        /// <summary> Returns the normalized audio peaks (split by channel) for each ms (List = time, array = channel). </summary>
        public static List<float[]> GetPeaks(string aFileName)
        {
            lock (locks.GetOrAdd(aFileName, new object()))
            {
                int    stream  = CreateStream(aFileName);
                long   length  = Bass.ChannelGetLength(stream);
                double seconds = Bass.ChannelBytes2Seconds(stream, length);

                Bass.ChannelGetInfo(stream, out ChannelInfo channelInfo);

                List<float[]> peaks = new List<float[]>();
                for (int i = 0; i < (int)(seconds * 1000); ++i)
                {
                    float[] levels = new float[channelInfo.Channels];

                    bool success = Bass.ChannelGetLevel(stream, levels, 0.001f, 0);
                    if (!success)
                        throw new BadImageFormatException(
                            "Could not parse audio peak of \"" + aFileName + "\" at " + i * 1000 + " ms.");

                    peaks.Add(levels);
                }

                FreeStream(stream);
                return peaks;
            }
        }
    }
}
