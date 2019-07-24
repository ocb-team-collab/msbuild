using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

namespace Microsoft.Build.BackEnd.Shared
{
    public class SimulatedFileSystem
    {
        private readonly IDictionary<string, StaticFile> _pathToFileObject;
        private readonly List<StaticFile> _fileByIndex;

        public static readonly SimulatedFileSystem Instance = new SimulatedFileSystem();

        private SimulatedFileSystem()
        {
            _pathToFileObject = new Dictionary<string, StaticFile>(StringComparer.OrdinalIgnoreCase);
            _fileByIndex = new List<StaticFile>();
        }

        public IEnumerable<StaticFile> KnownFiles => _fileByIndex;

        public long RecordOutput(long producingTargetId, ITaskItem outputItem)
        {
            return RecordOutput(producingTargetId, outputItem.ItemSpec);
        }

        public long RecordOutput(long producingTargetId, string filePath)
        {
            var fileObject = GetFileObject(filePath);

            if (fileObject.ProducingTargetId.HasValue && producingTargetId != fileObject.ProducingTargetId)
            {
                throw new ApplicationException($"Duplicate producer for {filePath}");
            }

            fileObject.ProducingTargetId = producingTargetId;

            return fileObject.Id;
        }

        public long GetFileId(string filePath)
        {
            return GetFileObject(filePath).Id;
        }

        private StaticFile GetFileObject(string filePath)
        {
            if (!_pathToFileObject.TryGetValue(filePath, out var fileObject))
            {
                fileObject = new StaticFile
                {
                    Path = filePath,
                };

                _pathToFileObject.Add(filePath, fileObject);
                _fileByIndex.Add(fileObject);
            }

            return fileObject;
        }
    }

    public class StaticFile
    {
        private static long _nextId = -1;

        public string Path { get; set; }

        public long? ProducingTargetId { get; set; }

        public long Id { get; set; }

        public StaticFile()
        {
            Id = Interlocked.Increment(ref _nextId);
        }
    }

}
