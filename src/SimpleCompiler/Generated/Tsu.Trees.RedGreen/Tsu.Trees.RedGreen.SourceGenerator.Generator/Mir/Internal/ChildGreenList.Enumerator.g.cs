﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Modified by the Tsu (https://github.com/GGG-KILLER/Tsu) project for embedding into other projects.
// <auto-generated />

#nullable enable

namespace SimpleCompiler.MIR.Internal
{
    internal partial struct ChildMirList
    {
        internal struct Enumerator
        {
            private readonly global::SimpleCompiler.MIR.Internal.MirNode? _node;
            private int _childIndex;
            private global::SimpleCompiler.MIR.Internal.MirNode? _list;
            private int _listIndex;
            private global::SimpleCompiler.MIR.Internal.MirNode? _currentChild;

            internal Enumerator(global::SimpleCompiler.MIR.Internal.MirNode? node)
            {
                _node = node;
                _childIndex = -1;
                _listIndex = -1;
                _list = null;
                _currentChild = null;
            }

            public bool MoveNext()
            {
                if (_node != null)
                {
                    if (_list != null)
                    {
                        _listIndex++;

                        if (_listIndex < _list.SlotCount)
                        {
                            _currentChild = _list.GetSlot(_listIndex);
                            return true;
                        }

                        _list = null;
                        _listIndex = -1;
                    }

                    while (true)
                    {
                        _childIndex++;

                        if (_childIndex == _node.SlotCount)
                        {
                            break;
                        }

                        var child = _node.GetSlot(_childIndex);
                        if (child == null)
                        {
                            continue;
                        }

                        if (child.Kind == global::SimpleCompiler.MIR.Internal.MirNode.ListKind)
                        {
                            _list = child;
                            _listIndex++;

                            if (_listIndex < _list.SlotCount)
                            {
                                _currentChild = _list.GetSlot(_listIndex);
                                return true;
                            }
                            else
                            {
                                _list = null;
                                _listIndex = -1;
                                continue;
                            }
                        }
                        else
                        {
                            _currentChild = child;
                        }

                        return true;
                    }
                }

                _currentChild = null;
                return false;
            }

            public global::SimpleCompiler.MIR.Internal.MirNode Current => _currentChild!;
        }
    }
}