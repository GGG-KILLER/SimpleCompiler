namespace SimpleCompiler.FileSystem;

public interface IOutputManager
{
    Stream CreateFile(string name, FileAccess access = FileAccess.Write);
}
