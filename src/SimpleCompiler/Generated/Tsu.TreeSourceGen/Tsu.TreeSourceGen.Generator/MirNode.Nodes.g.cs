﻿// <auto-generated/>

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SimpleCompiler.MIR
{
    partial class MirNode 
    {
        public abstract void Accept(SimpleCompiler.MIR.MirVisitor visitor);
        [return: MaybeNull]
        public abstract TReturn Accept<TReturn>(SimpleCompiler.MIR.MirVisitor<TReturn> visitor);
    }
}

namespace SimpleCompiler.MIR
{
    partial class Assignment 
    {
        public override void Accept(SimpleCompiler.MIR.MirVisitor visitor) => visitor.VisitAssignment(this);
        [return: MaybeNull]
        public override TReturn Accept<TReturn>(SimpleCompiler.MIR.MirVisitor<TReturn> visitor) => visitor.VisitAssignment(this);
    }
}

namespace SimpleCompiler.MIR
{
    partial class BinaryOperation 
    {
        public override void Accept(SimpleCompiler.MIR.MirVisitor visitor) => visitor.VisitBinaryOperation(this);
        [return: MaybeNull]
        public override TReturn Accept<TReturn>(SimpleCompiler.MIR.MirVisitor<TReturn> visitor) => visitor.VisitBinaryOperation(this);
    }
}

namespace SimpleCompiler
{
    partial class Constant 
    {
        public override void Accept(SimpleCompiler.MIR.MirVisitor visitor) => visitor.VisitConstant(this);
        [return: MaybeNull]
        public override TReturn Accept<TReturn>(SimpleCompiler.MIR.MirVisitor<TReturn> visitor) => visitor.VisitConstant(this);
    }
}

namespace SimpleCompiler.MIR
{
    partial class ExpressionStatement 
    {
        public override void Accept(SimpleCompiler.MIR.MirVisitor visitor) => visitor.VisitExpressionStatement(this);
        [return: MaybeNull]
        public override TReturn Accept<TReturn>(SimpleCompiler.MIR.MirVisitor<TReturn> visitor) => visitor.VisitExpressionStatement(this);
    }
}

namespace SimpleCompiler.MIR
{
    partial class FunctionCall 
    {
        public override void Accept(SimpleCompiler.MIR.MirVisitor visitor) => visitor.VisitFunctionCall(this);
        [return: MaybeNull]
        public override TReturn Accept<TReturn>(SimpleCompiler.MIR.MirVisitor<TReturn> visitor) => visitor.VisitFunctionCall(this);
    }
}

namespace SimpleCompiler.MIR
{
    partial class EmptyStatement 
    {
        public override void Accept(SimpleCompiler.MIR.MirVisitor visitor) => visitor.VisitEmptyStatement(this);
        [return: MaybeNull]
        public override TReturn Accept<TReturn>(SimpleCompiler.MIR.MirVisitor<TReturn> visitor) => visitor.VisitEmptyStatement(this);
    }
}

namespace SimpleCompiler.MIR
{
    partial class StatementList 
    {
        public override void Accept(SimpleCompiler.MIR.MirVisitor visitor) => visitor.VisitStatementList(this);
        [return: MaybeNull]
        public override TReturn Accept<TReturn>(SimpleCompiler.MIR.MirVisitor<TReturn> visitor) => visitor.VisitStatementList(this);
    }
}

namespace SimpleCompiler.MIR
{
    partial class UnaryOperation 
    {
        public override void Accept(SimpleCompiler.MIR.MirVisitor visitor) => visitor.VisitUnaryOperation(this);
        [return: MaybeNull]
        public override TReturn Accept<TReturn>(SimpleCompiler.MIR.MirVisitor<TReturn> visitor) => visitor.VisitUnaryOperation(this);
    }
}

namespace SimpleCompiler
{
    partial class Variable 
    {
        public override void Accept(SimpleCompiler.MIR.MirVisitor visitor) => visitor.VisitVariable(this);
        [return: MaybeNull]
        public override TReturn Accept<TReturn>(SimpleCompiler.MIR.MirVisitor<TReturn> visitor) => visitor.VisitVariable(this);
    }
}