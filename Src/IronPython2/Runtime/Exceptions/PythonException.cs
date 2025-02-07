﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.Scripting.Runtime;

namespace IronPython2.Runtime.Exceptions {
    [Serializable]
    class PythonException : Exception, IPythonAwareException {
        private object _pyExceptionObject;
        private List<DynamicStackFrame> _frames;
        private TraceBack _traceback;

        public PythonException() : base() { }
        public PythonException(string msg) : base(msg) { }
        public PythonException(string message, Exception innerException)
            : base(message, innerException) {
        }
#if FEATURE_SERIALIZATION
        protected PythonException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif

        object IPythonAwareException.PythonException {
            get {
                return _pyExceptionObject;
            }
            set {
                _pyExceptionObject = value;
            }
        }

        List<DynamicStackFrame> IPythonAwareException.Frames {
            get { return _frames; }
            set { _frames = value; }
        }

        TraceBack IPythonAwareException.TraceBack {
            get { return _traceback; }
            set { _traceback = value; }
        }
    }

    interface IPythonAwareException {
        object PythonException {
            get;
            set;
        }
        List<DynamicStackFrame> Frames {
            get;
            set;
        }

        TraceBack TraceBack {
            get;
            set;
        }
    }
}
