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

        private double? duration;
        private uint? sampleRate;

        private double? averageBitrate;
        private double? lowestBitrate;
        private double? highestBitrate;

        private float[] leftChannel;
        private float[] rightChannel;

        public AudioFile(string aFileName)
        {
            fileName = aFileName;

            leftChannel = null;
            rightChannel = null;
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

        /// <summary> Returns the duration of an mp3 file, as measured by combining the length of each frame. </summary>
        public double GetDuration()
        {
            if (this.duration != null)
                return this.duration.GetValueOrDefault();
            
            if (fileName == null)
                return -1;

            double tempDuration = 0;
            using (FileStream fs = File.OpenRead(fileName))
            {
                Mp3Frame frame = Mp3Frame.LoadFromStream(fs);

                if (frame != null)
                    sampleRate = (uint)frame.SampleRate;

                while (frame != null)
                {
                    tempDuration += frame.SampleCount / (double)frame.SampleRate;

                    try
                    { frame = Mp3Frame.LoadFromStream(fs); }
                    catch (EndOfStreamException)
                    { break; }
                }
            }
            duration = tempDuration;
            return duration.GetValueOrDefault();
        }

        /// <summary> Populates the left and right audio channels, if not already present, and then returns them.
        /// Only works for mp3 files. Returns an error if it failed. </summary>
        public string ReadMp3(out float[] aLeft, out float[] aRight)
        {
            if (leftChannel != null || rightChannel != null)
            {
                aLeft = leftChannel;
                aRight = rightChannel;
            }

            aLeft = null;
            aRight = null;

            byte[] buffer = new byte[16384 * 4];
            MemoryStream stream = new MemoryStream();

            using (FileStream fs = File.OpenRead(fileName))
            {
                try
                {
                    Mp3Frame frame = Mp3Frame.LoadFromStream(fs);

                    var mp3Format = new Mp3WaveFormat(44100, 2, 215, 32000);
                    var frameDecompressor = new DmoMp3FrameDecompressor(mp3Format);

                    while (frame != null)
                    {
                        int result = frameDecompressor.DecompressFrame(frame, buffer, 0); // some mp3s seem to crash here
                        stream.Write(buffer, 0, result);

                        try
                        { frame = Mp3Frame.LoadFromStream(fs); }
                        catch (EndOfStreamException)
                        { break; }
                    }
                }
                catch (Exception exception)
                {
                    return "returned exception \"" + exception.Message + "\"";
                }
            }

            byte[] bytes = stream.ToArray();
            Int16[] ints = new Int16[bytes.Length / sizeof(Int16)];
            Buffer.BlockCopy(bytes, 0, ints, 0, bytes.Length);

            float[] floats = ints.Select(anInt => anInt / (float)Int16.MaxValue).ToArray();

            aLeft = new float[floats.Length / 2];
            aRight = new float[floats.Length / 2];
            for (int j = 0, s = 0; j < floats.Length / 2; j++)
            {
                aLeft[j] = floats[s++];
                aRight[j] = floats[s++];
            }

            leftChannel = aLeft;
            rightChannel = aRight;

            return null;
        }

        /// <summary> Populates the left and right audio channels, if not already present, and then returns them.
        /// Only works for wav files. Returns an error if it failed. </summary>
        public string ReadWav(out float[] aLeft, out float[] aRight)
        {
            if (leftChannel != null || rightChannel != null)
            {
                aLeft = leftChannel;
                aRight = rightChannel;
            }

            aLeft = null;
            aRight = null;

            try
            {
                using (FileStream fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    BinaryReader reader = new BinaryReader(fileStream);

                    // see format at http://soundfile.sapp.org/doc/WaveFormat/

                    // riff header
                    /*int chunkID     =*/ reader.ReadInt32(); // literally "RIFF"
                    /*int chunkSize   =*/ reader.ReadInt32(); // file size in bytes
                    /*int riffFormat  =*/ reader.ReadInt32(); // literally "WAVE"

                    // fmt subchunk
                    /*int fmtID       =*/ reader.ReadInt32(); // literally "fmt ", with the space
                    /*int fmtSize     =*/ reader.ReadInt32(); // bytes for the rest of the chunk following this, 16 for PCM
                    int fmtFormat     =   reader.ReadInt16(); // PCM = 1, others indicate some compression format
                    int channels      =   reader.ReadInt16();
                    /*int sampleRate  =*/ reader.ReadInt32();
                    /*int byteRate    =*/ reader.ReadInt32(); // sampleRate * channels * bitDepth / 8     // 44100 * 2 * 4
                    /*int blockAlign  =*/ reader.ReadInt16(); // channels * bitDepth / 8
                    int bitDepth      =   reader.ReadInt16(); // bits per sample

                    // if not PCM, then this exists
                    if (fmtFormat != 1)
                    {
                        int extraSize = reader.ReadInt16();
                        reader.ReadBytes(extraSize);
                    }

                    // data subchunk
                    /*int dataID  =*/ reader.ReadInt32(); // literally "data"
                    int bytes     =   reader.ReadInt32(); // samples * channels * bitDepth / 8

                    if (bytes < 0)
                        return "could not be parsed (negative bytes)";

                    int bytesPerSample = bitDepth / 8;
                    if (bytes / (float)(channels * bytesPerSample) < 1 && bytes != 0)
                        return "could not be parsed (byte mismatch)";

                    // raw data
                    byte[] byteArray = reader.ReadBytes(bytes);
                    if (byteArray.Length < bytes)
                        return "could not be parsed (missing bytes)";

                    int samples = bytes / bytesPerSample;

                    float[] floats = null;
                    switch (bitDepth)
                    {
                        case 64:
                            double[] doubles = new double[samples];
                            Buffer.BlockCopy(byteArray, 0, doubles, 0, bytes);
                            floats = doubles.Select(aDouble => (float)aDouble).ToArray();
                            break;
                        case 32:
                            floats = new float[samples];
                            Buffer.BlockCopy(byteArray, 0, floats, 0, bytes);
                            break;
                        case 16:
                            Int16[] int16s = new Int16[samples];
                            Buffer.BlockCopy(byteArray, 0, int16s, 0, bytes);
                            floats = int16s.Select(anInt => anInt / (float)Int16.MaxValue).ToArray();
                            break;
                        default:
                            return "could not be parsed (bit depth " + bitDepth + " unsupported)";
                    }

                    switch (channels)
                    {
                        case 1:
                            aLeft = floats;
                            aRight = null;

                            leftChannel = aLeft;
                            rightChannel = aRight;

                            return null;
                        case 2:
                            aLeft = new float[samples / 2];
                            aRight = new float[samples / 2];
                            for (int i = 0, s = 0; i < samples / 2; i++)
                            {
                                aLeft[i] = floats[s++];
                                aRight[i] = floats[s++];
                            }

                            leftChannel = aLeft;
                            rightChannel = aRight;

                            return null;
                        default:
                            return "could not be parsed (unknown channel amount " + channels + ")";
                    }
                }
            }
            catch (Exception exception)
            {
                return "returned exception \"" + exception.Message + "\"";
            }
        }
    }
}
