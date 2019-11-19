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
using TestGeneratorLib.Entity;
using FileInfo = System.IO.FileInfo;

namespace UnitTest
{
    public class ConveyorTest
    {
        private Conveyor _conv;
        private string _testableFileContent;

        private const string OutDir = @"../../../Files/out";
        private const string FilesDir = @"../../../Files/MyClass.cs";
        private DirectoryInfo outDi = new DirectoryInfo(OutDir);

        public static readonly int MaxDegreeOfParallelism = Environment.ProcessorCount;

        private static readonly ExecutionDataflowBlockOptions ExecutionOptions =
            new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = MaxDegreeOfParallelism};

        private readonly DataflowLinkOptions _linkOptions =
            new DataflowLinkOptions {PropagateCompletion = true};

        private ActionBlock<OutputFile> _saveTestClassFileBlock;

        private readonly Mutex _directoryWorkMutex = new Mutex();

        [SetUp]
        public void Setup()
        {
            ClearOutDir();

            using (var sr = new System.IO.StreamReader(FilesDir))
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

            Task<TestGeneratorLib.Entity.FileInfo> gatherTask = _conv.GatherInfo(_testableFileContent);
            gatherTask.Wait();
            TestGeneratorLib.Entity.FileInfo actual = gatherTask.Result;

            FileInfo expected;

            List<NamespaceInfo> actualNs = actual.Namespaces;
            Assert.AreEqual(1, actualNs.Count);
            Assert.AreEqual("UnitTest.Files", actualNs[0].Name);

            List<TestableClassInfo> actualClasses = actual.Namespaces[0].Classes;
            Assert.AreEqual(1, actualClasses.Count);
            Assert.AreEqual("MyClass", actualClasses[0].Name);

            List<BaseMethodInfo> actualConstructors = actualClasses[0].Constructors;
            Assert.AreEqual(1, actualConstructors.Count);

            Assert.AreEqual("MyClass", actualConstructors[0].Name);
            Assert.AreEqual(0, actualConstructors[0].ParamTypeNamesByParamName.Count);
            Assert.AreEqual(null, actualConstructors[0].ReturnTypeName);

            List<BaseMethodInfo> actualMethods = actualClasses[0].Methods;
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
            Stopwatch sw = new Stopwatch();

            sw.Restart();
            _conv = new Conveyor();
            _conv.LinkTo(_saveTestClassFileBlock, _linkOptions);

            _conv.Post(_testableFileContent);
            _conv.Complete();
            _saveTestClassFileBlock.Completion.Wait();
            sw.Stop();
            long oneFileProcessingElapsed = sw.ElapsedMilliseconds;

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
            long twoFileProcessingElapsed = sw.ElapsedMilliseconds;

            Assert.Less(twoFileProcessingElapsed, 2 * oneFileProcessingElapsed);
            FileAssert.Exists(OutDir + @"/MyClassTests.cs");
            FileAssert.Exists(OutDir + @"/MyClassTests0.cs");
            FileAssert.Exists(OutDir + @"/MyClassTests1.cs");
            FileInfo fi1 = Array.Find(outDi.GetFiles(), fi => fi.Name == "MyClassTests.cs");
            FileInfo fi2 = Array.Find(outDi.GetFiles(), fi => fi.Name == "MyClassTests0.cs");
            FileInfo fi3 = Array.Find(outDi.GetFiles(), fi => fi.Name == "MyClassTests1.cs");
            Assert.AreEqual(fi1.Length, fi2.Length);
            Assert.AreEqual(fi2.Length, fi3.Length);
        }

        private void ClearOutDir()
        {
            foreach (FileInfo file in outDi.GetFiles())
            {
                file.Delete();
            }
        }

        private void ClearCache()
        {
            ObjectCache cache = MemoryCache.Default;
            List<string> cacheKeys = cache.Select(kvp => kvp.Key).ToList();
            foreach (string cacheKey in cacheKeys)
            {
                cache.Remove(cacheKey);
            }
        }

        private Task SaveFile(OutputFile ff, string outDir)
        {
            string toSave = ff.Content;
            string fName = ff.Name + "Tests";
            int i = 0;
            string savePath = null;

            _directoryWorkMutex.WaitOne();

            savePath = outDir + "//" + fName + ".cs";
            if (File.Exists(savePath))
            {
                do
                {
                    savePath = outDir + "//" + fName + i++ + ".cs";
                } while (File.Exists(savePath));
            }

            Task saveToFileTask = Task.Run(() =>
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