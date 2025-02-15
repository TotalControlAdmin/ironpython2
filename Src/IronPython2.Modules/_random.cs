// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Numerics;

using IronPython2.Runtime;
using IronPython2.Runtime.Operations;
using Microsoft.Scripting.Utils;

[assembly: PythonModule("_random", typeof(IronPython2.Modules.PythonRandom))]
namespace IronPython2.Modules {
    public static partial class PythonRandom {
        public const string __doc__ = "implements a random number generator";

        [PythonType]
        public class Random {
            private RandomGen _rnd;

            public Random(object seed = null) {
                this.seed(seed);
            }

            #region Public API surface

            public object getrandbits(int bits) {
                if (bits <= 0) {
                    throw PythonOps.ValueError("number of bits must be greater than zero");
                }

                lock (this) {
                    return MathUtils.GetRandBits(_rnd.NextBytes, bits);
                }
            }

            public PythonTuple getstate() {
                object[] state;
                lock (this) {
                    state = _rnd.GetState();
                }
                return PythonTuple.MakeTuple(state);
            }

            public void jumpahead(int count) {
                lock (this) {
                    _rnd.NextBytes(new byte[4096]);
                }
            }

            public void jumpahead(BigInteger count) {
                lock (this) {
                    _rnd.NextBytes(new byte[4096]);
                }
            }

            public void jumpahead(object count) {
                throw PythonOps.TypeError("jumpahead requires an integer, not '{0}'", PythonOps.GetPythonTypeName(count));
            }

            public object random() {
                lock (this) {
                    byte[] randA = new byte[sizeof(uint)];
                    byte[] randB = new byte[sizeof(uint)];
                    _rnd.NextBytes(randA);
                    _rnd.NextBytes(randB);

                    // this is pulled from _randommodule.c from CPython
                    uint a = BitConverter.ToUInt32(randA, 0) >> 5;
                    uint b = BitConverter.ToUInt32(randB, 0) >> 6;
                    return (a * 67108864.0 + b) * (1.0 / 9007199254740992.0);
                }
            }

            public void seed(object s = null) {
                int newSeed;
                switch (s) {
                    case null:
                        newSeed = DateTime.Now.GetHashCode();
                        break;
                    case int i:
                        newSeed = i;
                        break;
                    default:
                        if (!PythonContext.IsHashable(s)) {
                            throw PythonOps.TypeError("unhashable type: '{0}'", PythonOps.GetPythonTypeName(s));
                        }
                        newSeed = s.GetHashCode();
                        break;
                }

                lock (this) {
                    _rnd = new RandomGen(newSeed);
                }
            }

            public void setstate(PythonTuple state) {
                if (state.Count != 58) throw PythonOps.ValueError("state vector is the wrong size");
                var stateArray = state._data.Cast<int>().ToArray();
                lock (this) {
                    _rnd.SetState(stateArray);
                }
            }

            #endregion
        }
    }
}
