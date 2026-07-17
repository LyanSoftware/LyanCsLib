using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Common
{
    public class EnumerableWrapperStream : Stream
    {
        public IEnumerable<byte> Enumerable { get; }
        private IEnumerator<byte> Enumerator { get; }

        public EnumerableWrapperStream(IEnumerable<byte> enumerable)
        {
            Enumerable = enumerable;
            Enumerator = enumerable.GetEnumerator();
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        private long position = 0;
        public override long Position { get => position; set => throw new NotSupportedException(); }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var c = 0;
            for (; c < count && Enumerator.MoveNext(); c++)
            {
                buffer[offset++] = Enumerator.Current;
                Position++;
            }
            return c;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin != SeekOrigin.Current || offset < 0)
                throw new NotSupportedException();
            for (; offset > 0 && Enumerator.MoveNext(); offset--)
                Position++;
            return Position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
