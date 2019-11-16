namespace TestGeneratorLib.Entity
{
    public class OutputFile
    {
        public string Name { get; private set; }
        public string Content { get; private set; }

        public OutputFile(string name, string content)
        {
            Name = name;
            Content = content;
        }
    }
}