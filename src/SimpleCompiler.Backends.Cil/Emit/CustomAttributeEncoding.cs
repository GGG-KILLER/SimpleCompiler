// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
**
**
** CustomAttributeBuilder is a helper class to help building custom attribute.
**
**
===========================================================*/

// See CorSerializationType in corhdr.h
internal enum CustomAttributeEncoding : int
{
    Undefined = 0,
    Boolean = CorElementType.ELEMENT_TYPE_BOOLEAN,
    Char = CorElementType.ELEMENT_TYPE_CHAR,
    SByte = CorElementType.ELEMENT_TYPE_I1,
    Byte = CorElementType.ELEMENT_TYPE_U1,
    Int16 = CorElementType.ELEMENT_TYPE_I2,
    UInt16 = CorElementType.ELEMENT_TYPE_U2,
    Int32 = CorElementType.ELEMENT_TYPE_I4,
    UInt32 = CorElementType.ELEMENT_TYPE_U4,
    Int64 = CorElementType.ELEMENT_TYPE_I8,
    UInt64 = CorElementType.ELEMENT_TYPE_U8,
    Float = CorElementType.ELEMENT_TYPE_R4,
    Double = CorElementType.ELEMENT_TYPE_R8,
    String = CorElementType.ELEMENT_TYPE_STRING,
    Array = CorElementType.ELEMENT_TYPE_SZARRAY,
    Type = 0x50,
    Object = 0x51,
    Field = 0x53,
    Property = 0x54,
    Enum = 0x55
}
