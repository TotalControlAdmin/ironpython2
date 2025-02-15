// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Dynamic;
using Microsoft.Scripting;

namespace IronPython2.Compiler {
    internal struct TokenWithSpan {
        private readonly Token _token;
        private readonly IndexSpan _span;

        public TokenWithSpan(Token token, IndexSpan span) {
            _token = token;
            _span = span;
        }

        public IndexSpan Span {
            get { return _span; }
        }

        public Token Token {
            get { return _token; }
        }

    }

    /// <summary>
    /// Summary description for Token.
    /// </summary>
    public abstract class Token {
        private readonly TokenKind _kind;

        protected Token(TokenKind kind) {
            _kind = kind;
        }

        public TokenKind Kind {
            get { return _kind; }
        }

        public virtual object Value {
            get {
                throw new NotSupportedException(IronPython2.Resources.TokenHasNoValue);
            }
        }

        public override string ToString() {
            return base.ToString() + "(" + _kind + ")";
        }

        public abstract String Image {
            get;
        }
    }

    public class ErrorToken : Token {
        private readonly String _message;

        public ErrorToken(String message)
            : base(TokenKind.Error) {
            _message = message;
        }

        public String Message {
            get { return _message; }
        }

        public override String Image {
            get { return _message; }
        }

        public override object Value {
            get { return _message; }
        }
    }

    public class IncompleteStringErrorToken : ErrorToken {
        private readonly string _value;

        public IncompleteStringErrorToken(string message, string value)
            : base(message) {
            _value = value;
        }

        public override string Image {
            get {
                return _value;
            }
        }

        public override object Value {
            get {
                return _value;
            }
        }
    }

    public class ConstantValueToken : Token {
        private readonly object _value;

        public ConstantValueToken(object value)
            : base(TokenKind.Constant) {
            _value = value;
        }

        public object Constant {
            get { return this._value; }
        }

        public override object Value {
            get { return _value; }
        }

        public override String Image {
            get {
                return _value == null ? "None" : _value.ToString();
            }
        }
    }

    sealed class UnicodeStringToken : ConstantValueToken {
        public UnicodeStringToken(object value)
            : base(value) {
        }
    }

    public sealed class CommentToken : Token {
        private readonly string _comment;

        public CommentToken(string comment)
            : base(TokenKind.Comment) {
            _comment = comment;
        }

        public string Comment {
            get { return _comment; }
        }

        public override string Image {
            get { return _comment; }
        }

        public override object Value {
            get { return _comment; }
        }
    }

    public class NameToken : Token {
        private readonly string _name;

        public NameToken(string name)
            : base(TokenKind.Name) {
            _name = name;
        }

        public string Name {
            get { return this._name; }
        }

        public override object Value {
            get { return _name; }
        }

        public override String Image {
            get {
                return _name;
            }
        }
    }

    public class OperatorToken : Token {
        private readonly int _precedence;
        private readonly string _image;

        public OperatorToken(TokenKind kind, string image, int precedence)
            : base(kind) {
            _image = image;
            _precedence = precedence;
        }

        public int Precedence {
            get { return _precedence; }
        }

        public override object Value {
            get { return _image; }
        }

        public override String Image {
            get { return _image; }
        }
    }

    public class SymbolToken : Token {
        private readonly string _image;

        public SymbolToken(TokenKind kind, String image)
            : base(kind) {
            _image = image;
        }

        public String Symbol {
            get { return _image; }
        }

        public override object Value {
            get { return _image; }
        }

        public override String Image {
            get { return _image; }
        }
    }
}
