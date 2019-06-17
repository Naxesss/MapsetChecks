using NAudio.FileFormats.Mp3;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MapsetChecks.objects
{
    public class AudioFile
    {
        private readonly string fileName;
        
        private uint? sampleRate;

        private double? averageBitrate;
        private double? lowestBitrate;
        private double? highestBitrate;

        public AudioFile(string aFileName)
        {
            fileName = aFileName;
        }

        /// <summary> Returns the highest bitrate, populates if not already present. </summary>
        public double GetHighestBitrate()
        {
            if (highestBitrate != null)
                return highestBitrate.GetValueOrDefault();

            LoadBitrates();
            return highestBitrate.GetValueOrDefault();
        }

        /// <summary> Returns the lowest bitrate, populates if not already present. </summary>
        public double GetLowestBitrate()
        {
            if (lowestBitrate != null)
                return lowestBitrate.GetValueOrDefault();

            LoadBitrates();
            return lowestBitrate.GetValueOrDefault();
        }

        /// <summary> Returns the average bitrate, populates if not already present. </summary>
        public double GetAverageBitrate()
        {
            if (averageBitrate != null)
                return averageBitrate.GetValueOrDefault();

            LoadBitrates();
            return averageBitrate.GetValueOrDefault();
        }

        /// <summary> Reads through all frames of the mp3 and populates the lowest, highest and average bitrate values. </summary>
        private void LoadBitrates()
        {
            long frameCount = 0;
            long totalBitrate = 0;

            using (FileStream fileStream = File.OpenRead(fileName))
            {
                Mp3Frame frame = Mp3Frame.LoadFromStream(fileStream);

                if (frame != null)
                {
                    sampleRate = (uint)frame.SampleRate;

                    lowestBitrate = frame.BitRate;
                    highestBitrate = frame.BitRate;

                    while (frame != null)
                    {
                        if (frame.BitRate < lowestBitrate)
                            lowestBitrate = frame.BitRate;

                        if (frame.BitRate > highestBitrate)
                            highestBitrate = frame.BitRate;

                        totalBitrate += frame.BitRate;

                        ++frameCount;
                        try
                        { frame = Mp3Frame.LoadFromStream(fileStream); }
                        catch (EndOfStreamException)
                        { break; }
                    }
                }
            }

            averageBitrate = totalBitrate / frameCount;
        }

        /// <summary> Returns the sample rate of the first frame in the mp3. This is usually constant. </summary>
        public uint GetSampleRate()
        {
            if (sampleRate != null)
                return sampleRate.GetValueOrDefault();

            using (FileStream fs = File.OpenRead(fileName))
            {
                Mp3Frame frame = Mp3Frame.LoadFromStream(fs);

                if (frame != null)
                    sampleRate = (uint)frame.SampleRate;
            }

            return sampleRate.GetValueOrDefault();
        }
    }
}
