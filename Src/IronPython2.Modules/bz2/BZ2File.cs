// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// Copyright 2012 Jeff Hardy
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Ionic.BZip2;
using IronPython2.Runtime;
using Microsoft.Scripting.Runtime;
using IronPython2.Runtime.Operations;
using Microsoft.Scripting.Utils;

namespace IronPython2.Modules.Bz2 {
    public static partial class Bz2Module {
        [PythonType]
        public class BZ2File : PythonFile {
            public const string __doc__ =
@"BZ2File(name [, mode='r', buffering=0, compresslevel=9]) -> file object

Open a bz2 file. The mode can be 'r' or 'w', for reading (default) or
writing. When opened for writing, the file will be created if it doesn't
exist, and truncated otherwise. If the buffering argument is given, 0 means
unbuffered, and larger numbers specify the buffer size. If compresslevel
is given, must be a number between 1 and 9.
";
            public int buffering { get; private set; }
            public int compresslevel { get; private set; }

            private Stream bz2Stream;

            public BZ2File(CodeContext context) : base(context) { }

            public void __init__(CodeContext context,
                string filename,
                [DefaultParameterValue("r")]string mode,
                [DefaultParameterValue(0)]int buffering,
                [DefaultParameterValue(DEFAULT_COMPRESSLEVEL)]int compresslevel) {

                var pythonContext = context.LanguageContext;

                this.buffering = buffering;
                this.compresslevel = compresslevel;

                if (!mode.Contains("b") && !mode.Contains("U")) {
                    // bz2 files are always written in binary mode, unless they are in univeral newline mode
                    mode = mode + 'b';
                }

                if (mode.Contains("w")) {
                    var underlyingStream = File.Open(filename, FileMode.Create, FileAccess.Write);

                    if (mode.Contains("p")) {
                        this.bz2Stream = new ParallelBZip2OutputStream(underlyingStream);
                    } else {
                        this.bz2Stream = new BZip2OutputStream(underlyingStream);
                    }
                } else {
                    this.bz2Stream = new BZip2InputStream(File.OpenRead(filename), lazyInitialize: true);
                }

                this.__init__(bz2Stream, pythonContext.DefaultEncoding, filename, mode);
            }

            [Documentation(@"close() -> None or (perhaps) an integer

Close the file. Sets data attribute .closed to true. A closed file
cannot be used for further I/O operations. close() may be called more
than once without error.
")]
            public new void close() {
                base.close();
            }

            [Documentation(@"read([size]) -> string

Read at most size uncompressed bytes, returned as a string. If the size
argument is negative or omitted, read until EOF is reached.
")]
            public new string read() {
                ThrowIfClosed();
                return base.read();
            }

            public new string read(int size) {
                ThrowIfClosed();
                return base.read(size);
            }

            [Documentation(@"readline([size]) -> string

Return the next line from the file, as a string, retaining newline.
A non-negative size argument will limit the maximum number of bytes to
return (an incomplete line may be returned then). Return an empty
string at EOF.
")]
            public new string readline() {
                ThrowIfClosed();
                return base.readline();
            }

            public new string readline(int sizehint) {
                ThrowIfClosed();
                return base.readline(sizehint);
            }

            [Documentation(@"readlines([size]) -> list

Call readline() repeatedly and return a list of lines read.
The optional size argument, if given, is an approximate bound on the
total number of bytes in the lines returned.
")]
            public new List readlines() {
                if (this.closed) throw PythonOps.ValueError("I/O operation on closed file");
                return base.readlines();
            }

            public new List readlines(int sizehint) {
                if (this.closed) throw PythonOps.ValueError("I/O operation on closed file");
                return base.readlines(sizehint);
            }

            [Documentation(@"xreadlines() -> self

For backward compatibility. BZ2File objects now include the performance
optimizations previously implemented in the xreadlines module.
")]
            public new BZ2File xreadlines() {
                return this;
            }

            [Documentation(@"seek(offset [, whence]) -> None

Move to new file position. Argument offset is a byte count. Optional
argument whence defaults to 0 (offset from start of file, offset
should be >= 0); other values are 1 (move relative to current position,
positive or negative), and 2 (move relative to end of file, usually
negative, although many platforms allow seeking beyond the end of a file).

Note that seeking of bz2 files is emulated, and depending on the parameters
the operation may be extremely slow.
")]
            public new void seek(long offset, [DefaultParameterValue(0)]int whence) {
                throw new NotImplementedException();

                //if (this.closed) throw PythonOps.ValueError("I/O operation on closed file");
                //base.seek(offset, whence);
            }

            [Documentation(@"tell() -> int

Return the current file position, an integer (may be a long integer).
")]
            public new object tell() {
                if (base._stream is BZip2InputStream bZip2InputStream && !bZip2InputStream.Initialized) return 0; // hack to make tarfile.bz2open happy
                throw new NotImplementedException();

                //if (this.closed) throw PythonOps.ValueError("I/O operation on closed file");
                //return base.tell();
            }

            [Documentation(@"write(data) -> None

Write the 'data' string to file. Note that due to buffering, close() may
be needed before the file on disk reflects the data written.
")]
            public new void write([BytesConversion]IList<byte> data) {
                ThrowIfClosed();
                base.write(data);
            }

            public new void write(object data) {
                ThrowIfClosed();
                base.write(data);
            }

            public new void write(string data) {
                ThrowIfClosed();
                base.write(data);
            }

            public new void write(PythonBuffer data) {
                ThrowIfClosed();
                base.write(data);
            }

            [Documentation(@"writelines(sequence_of_strings) -> None

Write the sequence of strings to the file. Note that newlines are not
added. The sequence can be any iterable object producing strings. This is
equivalent to calling write() for each string.
")]
            public new void writelines(object sequence_of_strings) {
                ThrowIfClosed();
                base.writelines(sequence_of_strings);
            }

            public void __del__() {
                this.close();
            }

            [Documentation("__enter__() -> self.")]
            public new object __enter__() {
                ThrowIfClosed();
                return this;
            }

            [Documentation("__exit__(*excinfo) -> None.  Closes the file.")]
            public new void __exit__(params object[] excinfo) {
                this.close();
            }
        }
    }
}
