﻿/*
 * Copyright 2006-2011 Bastian Eicher
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Common.Streams
{
    /// <summary>
    /// A circular buffer represented as a thread-safe stream that producers can write to and consumers can read from simultaneously.
    /// </summary>
    /// <remarks>
    /// Invariants:
    /// <see cref="Length"/> smaller or equal <see cref="BufferSize"/>,
    /// <see cref="PositionWrite"/> - <see cref="Position"/> equals <see cref="Length"/>
    /// </remarks>
    public sealed class CircularBufferStream : Stream
    {
        #region Variables
        /// <summary>The byte array used as a circular buffer storage.</summary>
        private readonly byte[] _buffer;

        /// <summary>The index of the first byte currently store in the <see cref="_buffer"/>.</summary>
        private volatile int _dataStart;
        
        /// <summary>The number of bytes currently stored in the <see cref="_buffer"/>.</summary>
        private volatile int _dataLength;

        /// <summary>Indicates that the producer end has finished and no new data will be added.</summary>
        private volatile bool _doneWriting;

        private readonly EventWaitHandle _dataAvailable = new EventWaitHandle(false, EventResetMode.ManualReset);
        private readonly EventWaitHandle _spaceAvailable = new EventWaitHandle(true, EventResetMode.ManualReset);
        #endregion

        #region Properties
        /// <summary>
        /// The maximum number of bytes the buffer can hold at any time.
        /// </summary>
        public int BufferSize { get { return _buffer.Length; } }

        /// <inheritdoc/>
        public override bool CanRead { get { return true; } }

        /// <inheritdoc/>
        public override bool CanWrite { get { return true; } }

        /// <inheritdoc/>
        public override bool CanSeek { get { return false; } }

        private long _positionRead;
        /// <summary>
        /// Indicates how many bytes have been read from this buffer so far.
        /// </summary>
        public override long Position { get { return _positionRead; } set { throw new NotSupportedException(); } }

        private long _positionWrite;
        /// <summary>
        /// Indicates how many bytes have been read from this buffer so far.
        /// </summary>
        public long PositionWrite { get { return _positionWrite; } }

        /// <summary>
        /// Returns the number of bytes currently stored in the buffer.
        /// </summary>
        public override long Length { get { return _dataLength; } }

        /// <summary>
        /// Indicates that this stream has been <see cref="Stream.Dispose"/>d or <see cref="Stream.Close"/>d.
        /// </summary>
        public bool IsDisposed { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new circular buffer.
        /// </summary>
        /// <param name="bufferSize">The maximum number of bytes the buffer can hold at any time.</param>
        public CircularBufferStream(int bufferSize)
        {
            _buffer = new byte[bufferSize];
        }
        #endregion

        //--------------------//

        #region Read
        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            #region Sanity checks
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (offset < 0 || offset > buffer.Length) throw new ArgumentOutOfRangeException("offset");
            if (count + offset > buffer.Length) throw new ArgumentOutOfRangeException("count");
            #endregion

            // Bytes copied to target buffer so far
            int bytesCopied = 0;

            // Loop until the request number of bytes have been returned or until no more data is available
            while (bytesCopied != count && !(_doneWriting && _dataLength == 0))
            {
                if (IsDisposed) throw new ObjectDisposedException("CircularBufferStream");

                // Block while buffer is empty
                _dataAvailable.WaitOne();

                lock (_buffer)
                {
                    // The index of the last byte currently stored in the buffer plus one
                    int dataEnd = (_dataStart + _dataLength) % _buffer.Length;

                    // Determine how many bytes can be read in one go
                    int contigousDataBytes;
                    if (_dataLength == 0) contigousDataBytes = 0; // Empty
                    else if (_dataLength == _buffer.Length) contigousDataBytes = _buffer.Length - _dataStart; // Full, potentially wrap around, partial read
                    else if (_dataStart < dataEnd) contigousDataBytes = dataEnd - _dataStart; // No wrap around, read all
                    else contigousDataBytes = _buffer.Length - _dataStart; // Wrap around, partial read
                    Debug.Assert(contigousDataBytes <= _dataLength, "Contigous data length can not exceed total data length");

                    int bytesToCopy = Math.Min(contigousDataBytes, count - bytesCopied);
                    Buffer.BlockCopy(_buffer, _dataStart, buffer, offset + bytesCopied, bytesToCopy);

                    // Update counters
                    bytesCopied += bytesToCopy;
                    _positionRead += bytesToCopy;
                    _dataLength -= bytesToCopy;
                    _dataStart += bytesToCopy;

                    // Roll over start pointer
                    _dataStart %= _buffer.Length;

                    // Start blocking when buffer becomes empty
                    if (_dataLength == 0) _dataAvailable.Reset();

                    // Stop blocking when space becomes available
                    if (_dataLength < _buffer.Length) _spaceAvailable.Set();
                }
            }

            return bytesCopied;
        }
        #endregion

        #region Write
        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            #region Sanity checks
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (offset < 0 || offset > buffer.Length) throw new ArgumentOutOfRangeException("offset");
            if (count + offset > buffer.Length) throw new ArgumentOutOfRangeException("count");
            #endregion

            // Bytes copied to internal buffer so far
            int bytesCopied = 0;

            // Loop until the request number of bytes have been written
            while (bytesCopied != count)
            {
                if (IsDisposed) throw new ObjectDisposedException("CircularBufferStream");

                // Block while buffer is full
                _spaceAvailable.WaitOne();
                
                lock (_buffer)
                {
                    // The index of the last byte currently stored in the buffer plus one
                    int dataEnd = (_dataStart + _dataLength) % _buffer.Length;

                    // Determine how many bytes can be written in one go
                    int contigousFreeBytes;
                    if (_dataLength == _buffer.Length) contigousFreeBytes = 0; // Full
                    else if (_dataLength == 0)
                    { // Empty, realign with buffer start
                        _dataStart = 0;
                        dataEnd = 0;
                        contigousFreeBytes = _buffer.Length;
                    }
                    else if (dataEnd < _dataStart) contigousFreeBytes = _dataStart - dataEnd; // No wrap around, full write
                    else contigousFreeBytes = _buffer.Length - dataEnd; // Wrap around, partial write
                    Debug.Assert(contigousFreeBytes <= (_buffer.Length - _dataLength), "Contigous free space can not exceed total free space");

                    int bytesToCopy = Math.Min(contigousFreeBytes, count - bytesCopied);
                    Buffer.BlockCopy(buffer, offset + bytesCopied, _buffer, dataEnd, bytesToCopy);

                    // Update counters
                    bytesCopied += bytesToCopy;
                    _positionWrite += bytesToCopy;
                    _dataLength += bytesToCopy;

                    // Start blocking when buffer becomes full
                    if (_dataLength == _buffer.Length) _spaceAvailable.Reset();

                    // Stop blocking when data becomes available
                    if (_dataLength > 0) _dataAvailable.Set();
                }
            }
        }

        /// <summary>
        /// Signals that no further calls to <see cref="Write"/> are intended and any blocked <see cref="Read"/> calls should return.
        /// </summary>
        public void DoneWriting()
        {
            _doneWriting = true;

            // Stop waiting for data that will never come
            _dataAvailable.Set();
        }
        #endregion

        #region Unsupported operations
        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Flush()
        { }
        #endregion

        //--------------------//

        #region Dispose
        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
        #endregion
    }
}