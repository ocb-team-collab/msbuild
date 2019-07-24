using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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

        public long RecordOutput(StaticTarget producingTarget, ITaskItem outputItem)
        {
            return RecordOutput(producingTarget, outputItem.ItemSpec);
        }

        public long RecordOutput(StaticTarget producingTarget, string filePath)
        {
            var fileObject = GetFileObject(filePath);

            if (fileObject.ProducingTarget != null && fileObject.ProducingTarget.Id != producingTarget.Id)
            {

                throw new ApplicationException($"Duplicate producer for {filePath}.  Produced by: " + fileObject.ProducingTarget.Location.LocationString + " and by " + producingTarget.Location.LocationString);
            }

            fileObject.ProducingTarget = producingTarget;

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

    [DataContract]
    public class StaticFile
    {
        private static long _nextId = -1;
        private StaticTarget _producingTarget;

        [DataMember]
        public string Path { get; set; }

        [DataMember]
        public long Id { get; set; }

        public StaticTarget ProducingTarget
        {
            get => _producingTarget;
            set
            {
                _producingTarget = value;
                ProducingTargetId = value?.Id;
            }
        }

        [DataMember]
        public long? ProducingTargetId { get; set; }

        public StaticFile()
        {
            Id = Interlocked.Increment(ref _nextId);
        }
    }

}
