using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.Build.BackEnd.Shared;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;

namespace Microsoft.Build.BackEnd
{
    [DataContract]
    public class StaticTarget
    {
        [DataContract]
        public class Task
        {
            [DataContract]
            public class TaskItem
            {
                [DataMember]
                public string ItemSpec;

                [DataMember]
                public Dictionary<string, string> Metadata;

                public TaskItem(ITaskItem taskItem)
                {
                    ItemSpec = taskItem.ItemSpec;
                    Metadata = taskItem.MetadataNames.OfType<string>()
                        .Where(t => !FileUtilities.ItemSpecModifiers.IsItemSpecModifier(t))
                        .ToDictionary(t => t, t => taskItem.GetMetadata(t));
                }
            }

            public enum ParameterType
            {
                Primitive,
                Primitives,
                TaskItem,
                TaskItems
            }

            [DataContract]
            public class Primitive
            {
                [DataMember]
                public string Value;

                [DataMember]
                public string Type;

                public Primitive(string value, Type type)
                {
                    Value = value;
                    Type = type.FullName;
                }
            }

            [DataContract]
            public class PrimitiveList
            {
                [DataMember]
                public List<string> Values;

                [DataMember]
                public string Type;

                public PrimitiveList(List<string> values, Type type)
                {
                    Values = values;
                    Type = type.FullName;
                }
            }

            [DataContract]
            public class Parameter
            {
                [DataMember]
                public ParameterType ParameterType;

                [DataMember]
                public Primitive Primitive;

                [DataMember]
                public TaskItem TaskItem;

                [DataMember]
                public PrimitiveList Primitives;

                [DataMember]
                public List<TaskItem> TaskItems;

                public Parameter(Primitive primitive)
                {
                    ParameterType = ParameterType.Primitive;
                    Primitive = primitive;
                }

                public Parameter(TaskItem value)
                {
                    ParameterType = ParameterType.TaskItem;
                    TaskItem = value;
                }

                public Parameter(List<TaskItem> value)
                {
                    ParameterType = ParameterType.TaskItems;
                    TaskItems = value;
                }

                public Parameter(PrimitiveList value)
                {
                    ParameterType = ParameterType.Primitives;
                    Primitives = value;
                }
            }

            [DataMember]
            public string Name;

            [DataMember]
            public string AssemblyFile;

            [DataMember]
            public string AssemblyName;

            [DataMember]
            public Dictionary<string, Parameter> Parameters = new Dictionary<string, Parameter>();
        }

        [DataMember]
        public List<Task> Tasks = new List<Task>();

        [DataMember]
        public long Id;

        [DataMember]
        public string Name;

        [DataMember]
        public List<long> InputFileIds { get; set; }

        [DataMember]
        public List<long> OutputFileIds { get; set; }

        private ElementLocation _location;

        public ElementLocation Location
        {
            get { return _location; }
            set { _location = value; LocationString = _location.LocationString; }
        }

        [DataMember]
        public string LocationString { get; set; }

        public void RecordInput(long fileId)
        {
            if (InputFileIds == null) InputFileIds = new List<long>();
            InputFileIds.Add(fileId);
        }

        public void RecordOutput(long fileId)
        {
            if (OutputFileIds == null) OutputFileIds = new List<long>();
            OutputFileIds.Add(fileId);
        }

        private static long _nextTargetId;
        public StaticTarget()
        {
            Id = Interlocked.Increment(ref _nextTargetId);
        }
    }


    [DataContract]
    public class StaticGraph
    {
        [DataMember]
        public List<StaticTarget> StaticTargets = new List<StaticTarget>();

        [DataMember]
        public List<StaticFile> Files;

        [DataMember]
        public string ProjectPath;
    }
}
