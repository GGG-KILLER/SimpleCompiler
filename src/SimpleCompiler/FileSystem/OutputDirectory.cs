namespace SimpleCompiler.FileSystem;

public sealed class OutputDirectory(string path) : IOutputManager
{
    public Stream CreateFile(string name, FileAccess access = FileAccess.Write)
    {
        if (name.StartsWith("../") || name.Contains("/../") || name.EndsWith("/.."))
            throw new ArgumentException("Cannot navigate out of any directory.", nameof(name));

        return File.Open(Path.Combine(path, name), FileMode.Create, access);
    }
}
