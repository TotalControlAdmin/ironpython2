// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

using Microsoft.Scripting;

using IronPython2.Runtime;

namespace IronPython2.Compiler {
    [Serializable]
    public sealed class PythonCompilerOptions : CompilerOptions {
        private ModuleOptions _module;
        private bool _skipFirstLine, _dontImplyIndent;
        private string _moduleName;
        private int[] _initialIndentation;
        private CompilationMode _compilationMode;

        /// <summary>
        /// Creates a new PythonCompilerOptions with the default language features enabled.
        /// </summary>
        public PythonCompilerOptions()
            : this(ModuleOptions.None) {
        }

        /// <summary>
        /// Creates a new PythonCompilerOptions with the specified language features enabled.
        /// </summary>
        public PythonCompilerOptions(ModuleOptions features) {
            _module = features;
        }

        /// <summary>
        /// Creates a new PythonCompilerOptions and enables or disables true division.
        /// 
        /// This overload is obsolete, instead you should use the overload which takes a
        /// ModuleOptions.
        /// </summary>
        [Obsolete("Use the overload that takes ModuleOptions instead")]
        public PythonCompilerOptions(bool trueDivision) {
            TrueDivision = trueDivision;
        }

        public bool DontImplyDedent {
            get { return _dontImplyIndent; }
            set { _dontImplyIndent = value; }
        }

        /// <summary>
        /// Gets or sets the initial indentation.  This can be set to allow parsing
        /// partial blocks of code that are already indented.
        /// 
        /// For each element of the array there is an additional level of indentation.
        /// Each integer value represents the number of spaces used for the indentation.
        /// 
        /// If this value is null then no indentation level is specified.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public int[] InitialIndent {
            get {
                return _initialIndentation;
            }
            set {
                _initialIndentation = value;
            }
        }

        public bool TrueDivision {
            get {
                return (_module & ModuleOptions.TrueDivision) != 0;
            }
            set {
                if (value) _module |= ModuleOptions.TrueDivision;
                else _module &= ~ModuleOptions.TrueDivision;
            }
        }

        public bool AllowWithStatement {
            get {
                return (_module & ModuleOptions.WithStatement) != 0;
            }
            set {
                if (value) _module |= ModuleOptions.WithStatement;
                else _module &= ~ModuleOptions.WithStatement;
            }
        }

        public bool AbsoluteImports {
            get {
                return (_module & ModuleOptions.AbsoluteImports) != 0;
            }
            set {
                if (value) _module |= ModuleOptions.AbsoluteImports;
                else _module &= ~ModuleOptions.AbsoluteImports;
            }
        }

        public bool Verbatim {
            get {
                return (_module & ModuleOptions.Verbatim) != 0;
            }
            set {
                if (value) _module |= ModuleOptions.Verbatim;
                else _module &= ~ModuleOptions.Verbatim;
            }
        }

        public bool PrintFunction {
            get {
                return (_module & ModuleOptions.PrintFunction) != 0;
            }
            set {
                if (value) _module |= ModuleOptions.PrintFunction;
                else _module &= ~ModuleOptions.PrintFunction;
            }
        }

        public bool UnicodeLiterals {
            get {
                return (_module & ModuleOptions.UnicodeLiterals) != 0;
            }
            set {
                if (value) _module |= ModuleOptions.UnicodeLiterals;
                else _module &= ~ModuleOptions.UnicodeLiterals;
            }
        }

        public bool Interpreted {
            get {
                return (_module & ModuleOptions.Interpret) != 0;
            }
            set {
                if (value) _module |= ModuleOptions.Interpret;
                else _module &= ~ModuleOptions.Interpret;
            }
        }

        public bool Optimized {
            get {
                return (_module & ModuleOptions.Optimized) != 0;
            }
            set {
                if (value) _module |= ModuleOptions.Optimized;
                else _module &= ~ModuleOptions.Optimized;
            }
        }

        public ModuleOptions Module {
            get {
                return _module;
            }
            set {
                _module = value;
            }
        }

        public string ModuleName {
            get {
                return _moduleName;
            }
            set {
                _moduleName = value;
            }
        }

        public bool SkipFirstLine {
            get { return _skipFirstLine; }
            set { _skipFirstLine = value; }
        }

        internal CompilationMode CompilationMode {
            get {
                return _compilationMode;
            }
            set {
                _compilationMode = value;
            }
        }
    }
}
