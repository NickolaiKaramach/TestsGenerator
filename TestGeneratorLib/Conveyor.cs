using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TestGeneratorLib.Entity;
using TestGeneratorLib.OutputUtil;

namespace TestGeneratorLib
{
    public class Conveyor : ITargetBlock<string>, ISourceBlock<OutputFile>
    {
        private readonly TransformBlock<string, FileInfo> _gatherInfoBlock;
        private readonly TransformBlock<FileInfo, OutputFile> _generateTestClassBlock;
        private readonly int _maxDegreeOfParallelism = Environment.ProcessorCount;

        public Conveyor()
        {
            var executionOptions = new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = _maxDegreeOfParallelism};
            var linkOptions = new DataflowLinkOptions {PropagateCompletion = true};

            _gatherInfoBlock = new TransformBlock<string, FileInfo>(
                async testableFileContent => await GatherInfo(testableFileContent),
                executionOptions);

            _generateTestClassBlock = new TransformBlock<FileInfo, OutputFile>(
                async gatheredInfo => await GenerateTestClassFile(gatheredInfo),
                executionOptions);

            _gatherInfoBlock.LinkTo(_generateTestClassBlock, linkOptions, fileInfo =>
                fileInfo != null && fileInfo.Namespaces.Count > 0);
        }

        public IDisposable LinkTo(ITargetBlock<OutputFile> target, DataflowLinkOptions linkOptions)
        {
            return _generateTestClassBlock.LinkTo(target, linkOptions);
        }

        public OutputFile ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<OutputFile> target,
            out bool messageConsumed)
        {
            return ((ISourceBlock<OutputFile>) _generateTestClassBlock).ConsumeMessage(messageHeader, target,
                out messageConsumed);
        }

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<OutputFile> target)
        {
            return ((ISourceBlock<OutputFile>) _generateTestClassBlock).ReserveMessage(messageHeader, target);
        }

        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<OutputFile> target)
        {
            ((ISourceBlock<OutputFile>) _generateTestClassBlock).ReleaseReservation(messageHeader, target);
        }

        public Task Completion => _generateTestClassBlock.Completion;

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, string messageValue,
            ISourceBlock<string> source, bool consumeToAccept)
        {
            return ((ITargetBlock<string>) _gatherInfoBlock).OfferMessage(messageHeader, messageValue, source,
                consumeToAccept);
        }

        public void Fault(Exception exception)
        {
            ((ITargetBlock<string>) _gatherInfoBlock).Fault(exception);
        }

        public void Complete()
        {
            _gatherInfoBlock.Complete();
        }

        public bool Post(string testableFilePath)
        {
            return _gatherInfoBlock.Post(testableFilePath);
        }

        private static Task<OutputFile> GenerateTestClassFile(FileInfo fi)
        {
            var outputFileFormatter = new OutputFileFormatter();
            return outputFileFormatter.MakeTestClassFile(fi);
        }

        public static Task<FileInfo> GatherInfo(string testableFileContent)
        {
            return Task.Run(() =>
            {
                var result = new FileInfo();
                result.Initialize(testableFileContent);

                return result;
            });
        }
    }
}