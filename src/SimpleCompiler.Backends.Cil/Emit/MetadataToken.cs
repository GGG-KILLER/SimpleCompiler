// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace SimpleCompiler.Backend.Cil.Emit
{
    internal struct MetadataToken
    {
        public int Value;

        public static implicit operator int(MetadataToken token) => token.Value;
        public static implicit operator MetadataToken(int token) => new MetadataToken(token);

        public static bool IsTokenOfType(int token, params MetadataTokenType[] types)
        {
            for (int i = 0; i < types.Length; i++)
            {
                if ((int)(token & 0xFF000000) == (int)types[i])
                    return true;
            }

            return false;
        }

        public static bool IsNullToken(int token) => (token & 0x00FFFFFF) == 0;

        public MetadataToken(int token) { Value = token; }

        public bool IsGlobalTypeDefToken => Value == 0x02000001;
        public MetadataTokenType TokenType => (MetadataTokenType)(Value & 0xFF000000);
        public bool IsTypeRef => TokenType == MetadataTokenType.TypeRef;
        public bool IsTypeDef => TokenType == MetadataTokenType.TypeDef;
        public bool IsFieldDef => TokenType == MetadataTokenType.FieldDef;
        public bool IsMethodDef => TokenType == MetadataTokenType.MethodDef;
        public bool IsMemberRef => TokenType == MetadataTokenType.MemberRef;
        public bool IsEvent => TokenType == MetadataTokenType.Event;
        public bool IsProperty => TokenType == MetadataTokenType.Property;
        public bool IsParamDef => TokenType == MetadataTokenType.ParamDef;
        public bool IsTypeSpec => TokenType == MetadataTokenType.TypeSpec;
        public bool IsMethodSpec => TokenType == MetadataTokenType.MethodSpec;
        public bool IsString => TokenType == MetadataTokenType.String;
        public bool IsSignature => TokenType == MetadataTokenType.Signature;
        public bool IsModule => TokenType == MetadataTokenType.Module;
        public bool IsAssembly => TokenType == MetadataTokenType.Assembly;
        public bool IsGenericPar => TokenType == MetadataTokenType.GenericPar;

        public override string ToString() => string.Create(CultureInfo.InvariantCulture, stackalloc char[64], $"0x{Value:x8}");
    }

}
