using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Windows.Storage;

namespace System.IO
{
  class FileStream : Stream
  {
    private Stream stream;

    public FileStream(string path, FileMode mode, FileAccess access)
    {
      OpenFile(path, access);
    }

    private async void OpenFile(string path, FileAccess access)
    {
      StorageFile file = await StorageFile.GetFileFromPathAsync(path);
      if (access == FileAccess.Read)
        stream = await file.OpenStreamForReadAsync();
      else
        stream = await file.OpenStreamForWriteAsync();
    }

    public override bool CanRead
    {
      get { return stream.CanRead; }
    }

    public override bool CanSeek
    {
      get { return stream.CanSeek; }
    }

    public override bool CanWrite
    {
      get { return stream.CanWrite; }
    }

    public override void Flush()
    {
      stream.Flush();
    }

    public override long Length
    {
      get { return stream.Length; }
    }

    public override long Position
    {
      get { return stream.Position; }
      set { stream.Position = value; }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      return stream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      return stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
      stream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      stream.Write(buffer, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
      stream.Dispose();
      stream = null;
    }
  }
}
