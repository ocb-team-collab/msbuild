using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Build.BackEnd;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;

namespace Microsoft.Build.TaskLauncher
{
    public class Program
    {
        public static int Main(string[] args)
        {
            StaticTarget target;
            var ser = new DataContractJsonSerializer(typeof(StaticTarget));
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new StreamWriter(memoryStream, Encoding.Default, 1024*4, true))
                {
                    string s;
                    while ((s = Console.ReadLine()) != null)
                    {
                        writer.WriteLine(s);
                    }
                }

                memoryStream.Position = 0;
                target = (StaticTarget)ser.ReadObject(memoryStream);
            }

            foreach (StaticTarget.Task staticTask in target.Tasks)
            {
                LoadedType loadedType = new LoadedType(Type.GetType(staticTask.Name), AssemblyLoadInfo.Create(assemblyFile: staticTask.AssemblyFile, assemblyName: staticTask.AssemblyName), null);
                TaskPropertyInfo[] taskProperties = AssemblyTaskFactory.GetTaskParameters(loadedType);

                ITask task = TaskLoader.CreateTask(loadedType, staticTask.Name, staticTask.AssemblyName, 0, 0, null

                #if FEATURE_APPDOMAIN
            , null
#endif
            , false
#if FEATURE_APPDOMAIN
            , out var appDomain
#endif
            );
                foreach (var parameter in staticTask.Parameters)
                {
                    var taskPropertyInfo = taskProperties.First(property => property.Name == parameter.Key);
                    TaskFactoryWrapper.SetPropertyValue(task, taskPropertyInfo, parameter.Value);
                }


                task.BuildEngine = new SimpleBuildEngine();
                if (!task.Execute())
                {
                    return 1;
                }

            }

            return 0;
        }
    }

    public class SimpleHostObject : ITaskHost
    {

    }

    public class SimpleBuildEngine : IBuildEngine5
    {
        public bool IsRunningMultipleNodes => throw new NotImplementedException();

        public bool ContinueOnError => throw new NotImplementedException();

        public int LineNumberOfTaskNode => throw new NotImplementedException();

        public int ColumnNumberOfTaskNode => throw new NotImplementedException();

        public string ProjectFileOfTaskNode => throw new NotImplementedException();

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs, string toolsVersion)
        {
            throw new NotImplementedException();
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotImplementedException();
        }

        public BuildEngineResult BuildProjectFilesInParallel(string[] projectFileNames, string[] targetNames, IDictionary[] globalProperties, IList<string>[] removeGlobalProperties, string[] toolsVersion, bool returnTargetOutputs)
        {
            throw new NotImplementedException();
        }

        public bool BuildProjectFilesInParallel(string[] projectFileNames, string[] targetNames, IDictionary[] globalProperties, IDictionary[] targetOutputsPerProject, string[] toolsVersion, bool useResultsCache, bool unloadProjectsOnCompletion)
        {
            throw new NotImplementedException();
        }

        public object GetRegisteredTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
        {
            throw new NotImplementedException();
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            Console.WriteLine("Custom event: " + e.Message);
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            Console.WriteLine("Error: " + e.Message);
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            Console.WriteLine("Message: " + e.Message);
        }

        public void LogTelemetry(string eventName, IDictionary<string, string> properties)
        {
            throw new NotImplementedException();
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            Console.WriteLine("Warning: " + e.Message);
        }

        public void Reacquire()
        {
            throw new NotImplementedException();
        }

        public void RegisterTaskObject(object key, object obj, RegisteredTaskObjectLifetime lifetime, bool allowEarlyCollection)
        {
            throw new NotImplementedException();
        }

        public object UnregisterTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
        {
            throw new NotImplementedException();
        }

        public void Yield()
        {
            throw new NotImplementedException();
        }
    }
}
