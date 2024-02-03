﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Modified by the Tsu (https://github.com/GGG-KILLER/Tsu) project for embedding into other projects.
// <auto-generated />

#nullable enable

namespace SimpleCompiler.MIR.Internal
{
    internal abstract class MirWalker : global::SimpleCompiler.MIR.Internal.MirVisitor
    {
        private int _recursionDepth;

        public override void Visit(global::SimpleCompiler.MIR.Internal.MirNode? node)
        {
            if (node != null)
            {
                _recursionDepth++;
                if (_recursionDepth > 30)
                {
                    global::System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
                }

                node.Accept(this);

                _recursionDepth--;
            }
        }

        protected override void DefaultVisit(global::SimpleCompiler.MIR.Internal.MirNode node)
        {
            foreach (var child in node.ChildNodes())
            {
                Visit(child);
            }
        }
    }
}