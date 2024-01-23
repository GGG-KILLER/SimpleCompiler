namespace SimpleCompiler.Helpers;

public static class TypeHelper
{
    public static Type GetFuncType(Type[] args, Type ret)
    {
        return (args.Length + 1) switch
        {
            1 => typeof(Func<>).MakeGenericType(ret),
            2 => typeof(Func<,>).MakeGenericType(args[0], ret),
            3 => typeof(Func<,,>).MakeGenericType(args[0], args[1], ret),
            4 => typeof(Func<,,,>).MakeGenericType(args[0], args[1], args[2], ret),
            5 => typeof(Func<,,,,>).MakeGenericType(args[0], args[1], args[2], args[3], ret),
            6 => typeof(Func<,,,,,>).MakeGenericType(args[0], args[1], args[2], args[3], args[4], ret),
            7 => typeof(Func<,,,,,,>).MakeGenericType(args[0], args[1], args[2], args[3], args[4], args[5], ret),
            8 => typeof(Func<,,,,,,,>).MakeGenericType(args[0], args[1], args[2], args[3], args[4], args[5], args[6], ret),
            9 => typeof(Func<,,,,,,,,>).MakeGenericType(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], ret),
            10 => typeof(Func<,,,,,,,,,>).MakeGenericType(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], ret),
            11 => typeof(Func<,,,,,,,,,,>).MakeGenericType(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], ret),
            12 => typeof(Func<,,,,,,,,,,,>).MakeGenericType(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], ret),
            13 => typeof(Func<,,,,,,,,,,,,>).MakeGenericType(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11], ret),
            14 => typeof(Func<,,,,,,,,,,,,,>).MakeGenericType(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11], args[12], ret),
            15 => typeof(Func<,,,,,,,,,,,,,,>).MakeGenericType(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11], args[12], args[13], ret),
            16 => typeof(Func<,,,,,,,,,,,,,,,>).MakeGenericType(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11], args[12], args[13], args[14], ret),
            17 => typeof(Func<,,,,,,,,,,,,,,,,>).MakeGenericType(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11], args[12], args[13], args[14], args[15], ret),
            _ => throw new NotSupportedException()
        };
    }

    public static byte[] GenerateNullabeAttributeBytes(Type[] types)
    {
        var vals = new List<byte>(types.Length);
        foreach (var type in types)
            processType(type, vals);
        return [.. vals];

        static void processType(Type type, List<byte> bytes)
        {
            bytes.Add(0); // Oblivious
            if (type.IsGenericType)
            {
                foreach (var innerType in type.GetGenericArguments())
                    processType(innerType, bytes);
            }
        }
    }
}
