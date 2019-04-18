using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MapsetChecks.objects
{
    public class FileAbstraction : TagLib.File.IFileAbstraction
    {
        private Stream mStream;
        private string mFilePath;

        public FileAbstraction(string aFilePath)
        {
            mStream = aFilePath != null ? new FileStream(aFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite) : null;
            mFilePath = aFilePath;
        }

        public string Name
        {
            get { return mFilePath; }
        }

        public Stream ReadStream
        {
            get { return mStream; }
        }

        public Stream WriteStream
        {
            get { return mStream; }
        }

        public void CloseStream(Stream aStream)
        {
            aStream.Position = 0;
        }

        public TagLib.File GetTagFile()
        {
            if (mFilePath == null || mStream == null)
                return null;

            return TagLib.File.Create(this);
        }
    }
}
