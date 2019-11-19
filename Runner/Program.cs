using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TestGeneratorLib;
using TestGeneratorLib.Entity;

namespace Runner
{
    static class Program
    {
        private static readonly int MaxDegreeOfParallelism = Environment.ProcessorCount;

        private static readonly List<string> SavedPathes = new List<string>();
        private static readonly Mutex DirectoryWorkMutex = new Mutex();

        private static readonly ExecutionDataflowBlockOptions ExecutionOptions =
            new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = MaxDegreeOfParallelism};

        private static readonly TransformBlock<string, string> LoadTestableFileBlock =
            new TransformBlock<string, string>(
                async path => await ReadFileContent(path),
                ExecutionOptions);

        private static readonly ActionBlock<OutputFile> SaveTestClassFileBlock = new ActionBlock<OutputFile>(
            async formatTestClassFile => { await SaveFile(formatTestClassFile, SaveDir); },
            ExecutionOptions);

        private const string SaveDir = @"../../../../UnitTest/Files/out";

        private static void Main()
        {
            //Creating conv
            var conveyor = new Conveyor();
            var linkOptions = new DataflowLinkOptions {PropagateCompletion = true};

            LoadTestableFileBlock.LinkTo(conveyor, linkOptions);
            conveyor.LinkTo(SaveTestClassFileBlock, linkOptions);

            Console.WriteLine(LoadTestableFileBlock.Post(@"../../../../UnitTest/Files/DependentClass.cs"));
            Console.WriteLine(LoadTestableFileBlock.Post(@"../../../../UnitTest/Files/MyClass.cs"));

            LoadTestableFileBlock.Complete();
            SaveTestClassFileBlock.Completion.Wait();

            foreach (var path in SavedPathes)
            {
                Console.WriteLine(path);
            }
        }

        private static Task SaveFile(OutputFile outputFile, string outDir)
        {
            var toSave = outputFile.Content;
            var fileName = outputFile.Name + "Tests";
            var i = 0;

            DirectoryWorkMutex.WaitOne();

            var savePath = outDir + "//" + fileName + ".cs";
            while (System.IO.File.Exists(savePath))
            {
                savePath = outDir + "//" + fileName + i++ + ".cs";
            }

            SavedPathes.Add(savePath);
            var saveToFileTask = Task.Run(() =>
            {
                using (var saveFileStream = new System.IO.StreamWriter(savePath))
                {
                    saveFileStream.Write(toSave.ToCharArray(), 0, toSave.Length);
                }
            });
            
            DirectoryWorkMutex.ReleaseMutex();
            return saveToFileTask;
        }

        private static async Task<string> ReadFileContent(string path)
        {
            using (var reader = new System.IO.StreamReader(path))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}