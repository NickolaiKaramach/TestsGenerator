namespace TestGeneratorLib.OutputUtil
{
    public class OutputFile
    {
        public OutputFile(string name, string content)
        {
            Name = name;
            Content = content;
        }

        public string Name { get; }
        public string Content { get; }
    }
}