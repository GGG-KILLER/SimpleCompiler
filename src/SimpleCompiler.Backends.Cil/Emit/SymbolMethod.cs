// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using CultureInfo = System.Globalization.CultureInfo;

namespace SimpleCompiler.Backend.Cil.Emit
{
    internal sealed class SymbolMethod : MethodInfo
    {
        #region Private Data Members
        private readonly ModuleBuilder m_module;
        private readonly Type m_containingType;
        private readonly string m_name;
        private readonly CallingConventions m_callingConvention;
        private readonly Type m_returnType;
        private readonly int m_token;
        private readonly Type[] m_parameterTypes;
        #endregion

        #region Constructor
        internal SymbolMethod(ModuleBuilder mod, int token, Type arrayClass, string methodName,
            CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes)
        {
            // This is a kind of MethodInfo to represent methods for array type of unbaked type

            // Another way to look at this class is as a glorified MethodToken wrapper. At the time of this comment
            // this class is only constructed inside ModuleBuilder.GetArrayMethod and the only interesting thing
            // passed into it is this MethodToken. The MethodToken was forged using a TypeSpec for an Array type and
            // the name of the method on Array.
            // As none of the methods on Array have CustomModifiers their is no need to pass those around in here.
            m_token = token;

            // The ParameterTypes are also a bit interesting in that they may be unbaked TypeBuilders.
            m_returnType = returnType ?? typeof(void);
            if (parameterTypes != null)
            {
                m_parameterTypes = new Type[parameterTypes.Length];
                Array.Copy(parameterTypes, m_parameterTypes, parameterTypes.Length);
            }
            else
            {
                m_parameterTypes = Type.EmptyTypes;
            }

            m_module = mod;
            m_containingType = arrayClass;
            m_name = methodName;
            m_callingConvention = callingConvention;

            // Validate signature
            SignatureHelper.GetMethodSigHelper(
                mod, callingConvention, returnType, null, null, parameterTypes, null, null);
        }
        #endregion

        #region MemberInfo Overrides
        public override int MetadataToken => m_token;

        public override Module Module => m_module;

        public override Type? ReflectedType => m_containingType;

        public override string Name => m_name;

        public override Type? DeclaringType => m_containingType;
        #endregion

        #region MethodBase Overrides
        public override ParameterInfo[] GetParameters() => throw new NotSupportedException("TODO: Fill in with result of SR.NotSupported_SymbolMethod");

        public override MethodImplAttributes GetMethodImplementationFlags() => throw new NotSupportedException("TODO: Fill in with result of SR.NotSupported_SymbolMethod");

        public override MethodAttributes Attributes => throw new NotSupportedException("TODO: Fill in with result of SR.NotSupported_SymbolMethod");

        public override CallingConventions CallingConvention => m_callingConvention;

        public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException("TODO: Fill in with result of SR.NotSupported_SymbolMethod");

        #endregion

        #region MethodInfo Overrides
        public override Type ReturnType => m_returnType;

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => new EmptyCAHolder();

        public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture) => throw new NotSupportedException("TODO: Fill in with result of SR.NotSupported_SymbolMethod");

        public override MethodInfo GetBaseDefinition() => this;
        #endregion

        #region ICustomAttributeProvider Implementation
        public override object[] GetCustomAttributes(bool inherit) => throw new NotSupportedException("TODO: Fill in with result of SR.NotSupported_SymbolMethod");

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotSupportedException("TODO: Fill in with result of SR.NotSupported_SymbolMethod");

        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotSupportedException("TODO: Fill in with result of SR.NotSupported_SymbolMethod");

        #endregion

        #region Public Members
        public Module GetModule() => m_module;

        #endregion
    }
}
