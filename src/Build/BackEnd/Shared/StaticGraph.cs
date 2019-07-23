using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Collections;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using Microsoft.Build.Shared.FileSystem;
using ElementLocation = Microsoft.Build.Construction.ElementLocation;
using ProjectItemInstanceFactory = Microsoft.Build.Execution.ProjectItemInstance.TaskItem.ProjectItemInstanceFactory;
using ReservedPropertyNames = Microsoft.Build.Internal.ReservedPropertyNames;
using TargetLoggingContext = Microsoft.Build.BackEnd.Logging.TargetLoggingContext;
using TaskLoggingContext = Microsoft.Build.BackEnd.Logging.TaskLoggingContext;

namespace Microsoft.Build.BackEnd
{
    public class StaticTarget
    {
        public class Task
        {
            [DataMember]
            public string Name;

            [DataMember]
            public string AssemblyFile;

            [DataMember]
            public string AssemblyName;

            [DataMember]
            public Dictionary<string, object> Parameters = new Dictionary<string, object>();
        }

        [DataMember]
        public List<Task> Tasks = new List<Task>();
    }


    public class StaticGraph
    {
        [DataMember]
        public List<StaticTarget> StaticTargets = new List<StaticTarget>();
    }
}
