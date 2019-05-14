using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MapsetChecks.objects
{
    public class FileAbstraction : TagLib.File.IFileAbstraction
    {
        private readonly Stream stream;
        private readonly string filePath;

        public FileAbstraction(string aFilePath)
        {
            stream = aFilePath != null ? new FileStream(aFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite) : null;
            filePath = aFilePath;
        }

        public string Name
        {
            get { return filePath; }
        }

        public Stream ReadStream
        {
            get { return stream; }
        }

        public Stream WriteStream
        {
            get { return stream; }
        }

        public void CloseStream(Stream aStream)
        {
            aStream.Position = 0;
        }

        public TagLib.File GetTagFile()
        {
            if (filePath == null || stream == null)
                return null;

            return TagLib.File.Create(this);
        }
    }
}
