using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TestGeneratorLib.Entity;
using TestGeneratorLib.OutputUtil;

namespace TestGeneratorLib
{
    public class Conveyor : ITargetBlock<string>, IDataflowBlock, ISourceBlock<OutputFile>
    {
        public readonly int MaxDegreeOfParallelism = Environment.ProcessorCount;

        private readonly TransformBlock<string, FileInfo> _gatherInfoBlock;
        private readonly TransformBlock<FileInfo, OutputFile> GenerateTestClassBlock;

        public Task Completion => GenerateTestClassBlock.Completion;

        public Conveyor()
        {
            var executionOptions = new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = MaxDegreeOfParallelism};
            var linkOptions = new DataflowLinkOptions {PropagateCompletion = true};

            _gatherInfoBlock = new TransformBlock<string, FileInfo>(
                async testableFileContent => { return await GatherInfo(testableFileContent); },
                executionOptions);

            GenerateTestClassBlock = new TransformBlock<FileInfo, OutputFile>(
                async gatheredInfo => { return await GenerateTestClassFile(gatheredInfo); },
                executionOptions);

            _gatherInfoBlock.LinkTo(GenerateTestClassBlock, linkOptions, fileInfo =>
                fileInfo != null && fileInfo.Namespaces.Count > 0);
        }

        public bool Post(string testableFilePath)
        {
            return _gatherInfoBlock.Post(testableFilePath);
        }

        private Task<OutputFile> GenerateTestClassFile(FileInfo fi)
        {
            OutputFileFormatter outputFileFormatter = new OutputFileFormatter();
            return outputFileFormatter.MakeTestClassFile(fi);
        }

        // Test internal
        internal Task<FileInfo> GatherInfo(string testableFileContent)
        {
            return Task.Run(() =>
            {
                var result = new FileInfo();
                result.Initialize(testableFileContent);

                return result;
            });
        }

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

        public IDisposable LinkTo(ITargetBlock<OutputFile> target, DataflowLinkOptions linkOptions)
        {
            return GenerateTestClassBlock.LinkTo(target, linkOptions);
        }

        public OutputFile ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<OutputFile> target,
            out bool messageConsumed)
        {
            return ((ISourceBlock<OutputFile>) GenerateTestClassBlock).ConsumeMessage(messageHeader, target,
                out messageConsumed);
        }

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<OutputFile> target)
        {
            return ((ISourceBlock<OutputFile>) GenerateTestClassBlock).ReserveMessage(messageHeader, target);
        }

        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<OutputFile> target)
        {
            ((ISourceBlock<OutputFile>) GenerateTestClassBlock).ReleaseReservation(messageHeader, target);
        }

        public void Complete()
        {
            _gatherInfoBlock.Complete();
        }
    }
}