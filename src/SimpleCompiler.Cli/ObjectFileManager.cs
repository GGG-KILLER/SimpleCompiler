namespace SimpleCompiler.Cli;

internal sealed class ObjectFileManager(string path)
{
    public StreamWriter CreateText(string name) =>
        File.CreateText(Path.Combine(path, Path.GetFileName(name)));

    public static ObjectFileManager Create(string path)
    {
        Directory.CreateDirectory(path);
        return new ObjectFileManager(path);
    }
}
