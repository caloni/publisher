namespace Publisher.Core.Parsers
{
    public interface IParser
    {
        void ParseFile(string filePath, bool isPrivate);
        void ParseFiles(params string[] filePaths);
    }
}