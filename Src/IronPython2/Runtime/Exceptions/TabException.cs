﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Dynamic;
using Microsoft.Scripting;

namespace IronPython2.Runtime.Exceptions {
    /// <summary>
    /// .NET Exception thrown when a Python syntax error is related to incorrect tabs.
    /// </summary>
    [Serializable]
    sealed class TabException : IndentationException {
        public TabException(string message) : base(message) { }

        public TabException(string message, SourceUnit sourceUnit, SourceSpan span, int errorCode, Severity severity)
            : base(message, sourceUnit, span, errorCode, severity) { }
    }
}
