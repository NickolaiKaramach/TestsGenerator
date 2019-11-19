using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NUnit.Framework;
using TestGeneratorLib;
using TestGeneratorLib.OutputUtil;

namespace UnitTest
{
    public class ConveyorTest
    {
        private const string OutDir = @"../../../Files/out";
        private const string FilesDir = @"../../../Files/MyClass.cs";

        private static readonly int MaxDegreeOfParallelism = Environment.ProcessorCount;

        private static readonly ExecutionDataflowBlockOptions ExecutionOptions =
            new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = MaxDegreeOfParallelism};

        private readonly Mutex _directoryWorkMutex = new Mutex();

        private readonly DataflowLinkOptions _linkOptions =
            new DataflowLinkOptions {PropagateCompletion = true};

        private Conveyor _conv;

        private ActionBlock<OutputFile> _saveTestClassFileBlock;
        private string _testableFileContent;
        private readonly DirectoryInfo _outDir = new DirectoryInfo(OutDir);

        [SetUp]
        public void Setup()
        {
            ClearOutDir();

            using (var sr = new StreamReader(FilesDir))
            {
                _testableFileContent = sr.ReadToEnd();
            }

            _saveTestClassFileBlock = new ActionBlock<OutputFile>(
                async formatTestClassFile => { await SaveFile(formatTestClassFile, OutDir); },
                ExecutionOptions);
        }

        [Test]
        public void GatherInfoTest()
        {
            _conv = new Conveyor();

            var gatherTask = Conveyor.GatherInfo(_testableFileContent);
            gatherTask.Wait();
            var actual = gatherTask.Result;

            var actualNs = actual.Namespaces;
            Assert.AreEqual(1, actualNs.Count);
            Assert.AreEqual("UnitTest.Files", actualNs[0].Name);

            var actualClasses = actual.Namespaces[0].Classes;
            Assert.AreEqual(1, actualClasses.Count);
            Assert.AreEqual("MyClass", actualClasses[0].Name);

            var actualConstructors = actualClasses[0].Constructors;
            Assert.AreEqual(1, actualConstructors.Count);

            Assert.AreEqual("MyClass", actualConstructors[0].Name);
            Assert.AreEqual(0, actualConstructors[0].ParamTypeNamesByParamName.Count);
            Assert.AreEqual(null, actualConstructors[0].ReturnTypeName);

            var actualMethods = actualClasses[0].Methods;
            Assert.AreEqual(2, actualMethods.Count);

            Assert.AreEqual("PublicVoidMethod1", actualMethods[0].Name);
            Assert.AreEqual(0, actualMethods[0].ParamTypeNamesByParamName.Count);
            Assert.AreEqual("void", actualMethods[0].ReturnTypeName);

            Assert.AreEqual("PublicVoidMethod2", actualMethods[1].Name);
            Assert.AreEqual(2, actualMethods[1].ParamTypeNamesByParamName.Count);
            var expectedParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("d", "decimal"),
                new KeyValuePair<string, string>("os", "OperatingSystem")
            };
            Assert.AreEqual(expectedParams, actualMethods[1].ParamTypeNamesByParamName);
            Assert.AreEqual("void", actualMethods[1].ReturnTypeName);
        }

        [Test]
        public void ParallelWorkTest()
        {
            var sw = new Stopwatch();

            sw.Restart();
            _conv = new Conveyor();
            _conv.LinkTo(_saveTestClassFileBlock, _linkOptions);

            _conv.Post(_testableFileContent);
            _conv.Complete();
            _saveTestClassFileBlock.Completion.Wait();
            sw.Stop();
            var oneFileProcessingElapsed = sw.ElapsedMilliseconds;

            _saveTestClassFileBlock = new ActionBlock<OutputFile>(
                async formatTestClassFile => { await SaveFile(formatTestClassFile, OutDir); },
                ExecutionOptions);

            ClearCache();

            sw.Restart();
            _conv = new Conveyor();
            _conv.LinkTo(_saveTestClassFileBlock, _linkOptions);

            _conv.Post(_testableFileContent);
            _conv.Post(_testableFileContent);
            _conv.Complete();
            _conv.Completion.Wait();
            _saveTestClassFileBlock.Completion.Wait();
            sw.Stop();
            var twoFileProcessingElapsed = sw.ElapsedMilliseconds;

            Assert.Less(twoFileProcessingElapsed, 2 * oneFileProcessingElapsed);
            FileAssert.Exists(OutDir + @"/MyClassTests.cs");
            FileAssert.Exists(OutDir + @"/MyClassTests0.cs");
            FileAssert.Exists(OutDir + @"/MyClassTests1.cs");
            var fi1 = Array.Find(_outDir.GetFiles(), fi => fi.Name == "MyClassTests.cs");
            var fi2 = Array.Find(_outDir.GetFiles(), fi => fi.Name == "MyClassTests0.cs");
            var fi3 = Array.Find(_outDir.GetFiles(), fi => fi.Name == "MyClassTests1.cs");
            Assert.AreEqual(fi1.Length, fi2.Length);
            Assert.AreEqual(fi2.Length, fi3.Length);
        }

        private void ClearOutDir()
        {
            foreach (var file in _outDir.GetFiles()) file.Delete();
        }

        private static void ClearCache()
        {
            ObjectCache cache = MemoryCache.Default;
            var cacheKeys = cache.Select(kvp => kvp.Key).ToList();
            foreach (var cacheKey in cacheKeys)
            {
                cache.Remove(cacheKey);
            }
        }

        private Task SaveFile(OutputFile ff, string outDir)
        {
            var toSave = ff.Content;
            var fName = ff.Name + "Tests";
            var i = 0;

            _directoryWorkMutex.WaitOne();

            var savePath = outDir + "//" + fName + ".cs";
            while (File.Exists(savePath))
            {
                savePath = outDir + "//" + fName + i++ + ".cs";
            }

            var saveToFileTask = Task.Run(() =>
            {
                using (var saveFileStream = new StreamWriter(savePath))
                {
                    saveFileStream.Write(toSave.ToCharArray(), 0, toSave.Length);
                }
            });
            _directoryWorkMutex.ReleaseMutex();

            return saveToFileTask;
        }
    }
}