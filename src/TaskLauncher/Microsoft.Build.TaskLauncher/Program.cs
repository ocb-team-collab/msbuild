using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
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
            if (args.Length == 0 || args[0] == "/?" || args[0] == "-?" || args[0] == "-help" || args[0] == "--help" || args[0] == "/help")
            {
                Usage();
                return 0;
            }
            if (args[0].Equals("run", StringComparison.OrdinalIgnoreCase))
            {
                return RunTarget();
            }
            else if (args[0].Equals("print", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 3)
                {
                    Usage();
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("pkgdomino")))
                {
                    Console.WriteLine("%PKGDOMINO% must be set to root of Domino folder");
                    return 1;
                }

                StaticGraph graph = ReadStaticGraph(args[1]);
                PrintPips(graph, args[2]);
                WriteConfigDsc(args[2]);
                WriteModuleConfigDsc(args[2]);
                Console.WriteLine(@"%PKGDOMINO%\bxl.exe /c:" + args[2] + @"\config.dsc");
                return 0;
            }
            else
            {
                Usage();
                return 1;
            }
        }

        private static void Usage()
        {
            Console.WriteLine("Usage:\n\trun < targetJson\t\t\t\tRuns a target given its json description.\n\tprint graphJsonFile outputFolder\t\tPrints DominoScript into the output folder for a given graph json.");
        }

        private static StaticGraph ReadStaticGraph(string file)
        {
            StaticGraph graph;
            var ser = new DataContractJsonSerializer(typeof(StaticGraph));
            using (var stream = new FileStream(file, FileMode.Open))
            {
                graph = (StaticGraph)ser.ReadObject(stream);
            }

            return graph;
        }

        private static int PrintPips(StaticGraph graph, string outputFolder)
        {
            StringBuilder specContents = new StringBuilder();
            specContents.AppendLine("import {Cmd, Transformer} from \"Sdk.Transformers\";\n");
            specContents.AppendLine(
                string.Format("const tool: Transformer.ToolDefinition = {{ exe: f`{0}`, dependsOnWindowsDirectories: true, prepareTempDirectory: true, runtimeDirectoryDependencies: [ Transformer.sealSourceDirectory(d`{1}`) ] }};\n",
                    System.Reflection.Assembly.GetExecutingAssembly().Location,
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)));

            var ser = new DataContractJsonSerializer(typeof(StaticTarget));

            int i = 0;
            StringBuilder stdIn = new StringBuilder();
            foreach (var target in graph.StaticTargets)
            {
                using (var stream = new MemoryStream())
                {
                    ser.WriteObject(stream, target);
                    stream.Position = 0;
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            stdIn.AppendLine(line);
                        }
                    }
                }

                specContents.AppendLine(
                    string.Format("const {0} = Transformer.execute(\n{{\n\ttool: tool,\n\targuments: [ Cmd.rawArgument(\"run\") ],\n\tconsoleInput: \"{1}\",\n\tdescription: \"{2}\",\n\tworkingDirectory: d`{3}`,\n\tconsoleOutput: p`{4}`,\n\tdependencies: [{5}]\n}});\n",
                        "target" + i,
                        NormalizeRawString(stdIn.ToString(), stdIn),
                        "target" + i,
                        Directory.GetCurrentDirectory(),
                        Path.Combine(Directory.GetCurrentDirectory(), "target" + i + ".out"),
                        $@"f`{Environment.GetEnvironmentVariable("ProgramData")}\Microsoft\VisualStudio\Setup\x86\Microsoft.VisualStudio.Setup.Configuration.Native.dll`"));
                i++;

            }

            string specFile = Path.Combine(outputFolder, "spec.dsc");
            File.Delete(specFile);

            File.WriteAllText(specFile, specContents.ToString());

            return 0;
        }

        private static void WriteConfigDsc(string outputFolder)
        {
            string configDsc = string.Format("config({{\n\tmodules: [f`module.config.dsc` ],\n\tresolvers: [{{\n\t\tkind: \"SourceResolver\",\n\t\tpackages: [ f`{0}` ]\n}}],\n\tmounts: [ {{ name: a`src`, path: p`{1}`, trackSourceFileChanges: true, isWritable: false, isReadable: true }}, {{ name: a`ProgramData`, path: p`{2}`, trackSourceFileChanges: true, isWritable: false, isReadable: true }} ]\n}});",
                $"{Environment.GetEnvironmentVariable("pkgdomino")}/sdk/sdk.transformers/package.config.dsc",
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                Environment.GetEnvironmentVariable("ProgramData"));
            string configFile = Path.Combine(outputFolder, "config.dsc");
            File.Delete(configFile);
            File.WriteAllText(configFile, configDsc);

        }

        private static void WriteModuleConfigDsc(string outputFolder)
        {
            string modulePath = Path.Combine(outputFolder, "module.config.dsc");
            File.Delete(modulePath);
            File.WriteAllText(modulePath, "module({ name: 'test', nameResolutionSemantics: NameResolutionSemantics.implicitProjectReferences, projects: [ f`spec.dsc` ]});");
        }

        private static string NormalizeRawString(string raw, StringBuilder builder)
        {
            builder.Clear();

            foreach (char t in raw)
            {
                switch (t)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        builder.Append(t);
                        break;
                }
            }

            return builder.ToString();
        }

        private static int RunTarget()
        {
            StaticTarget target;
            var ser = new DataContractJsonSerializer(typeof(StaticTarget));
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new StreamWriter(memoryStream, Encoding.Default, 1024 * 4, true))
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
                    StaticTarget.Task.ParameterType parameterType = parameter.Value.ParameterType;
                    switch (parameterType)
                    {
                        case StaticTarget.Task.ParameterType.Primitive:
                            object value = GetTypedValue(parameter.Value.Primitive.Type, parameter.Value.Primitive.Value);
                            TaskFactoryWrapper.SetPropertyValue(task, taskPropertyInfo, value);

                            break;
                        case StaticTarget.Task.ParameterType.Primitives:
                            var values = parameter.Value.Primitives.Values.Select(stringValue => GetTypedValue(parameter.Value.Primitives.Type, stringValue));
                            TaskFactoryWrapper.SetPropertyValue(task, taskPropertyInfo, values);
                            break;
                        case StaticTarget.Task.ParameterType.TaskItem:
                            break;
                        case StaticTarget.Task.ParameterType.TaskItems:
                            break;
                    }

                }


                task.BuildEngine = new SimpleBuildEngine();
                if (!task.Execute())
                {
                    return 1;
                }

            }

            return 0;
        }

        private static object GetTypedValue(string typeString, string stringValue)
        {
            Type primitiveType = Type.GetType(typeString);
            if (primitiveType == typeof(bool) || primitiveType == typeof(bool[]))
            {
                return ConversionUtilities.ConvertStringToBool(stringValue);
            }
            else if (primitiveType == typeof(string) || primitiveType == typeof(string[]))
            {
                return stringValue;
            }
            else
            {
                return Convert.ChangeType(stringValue, primitiveType, CultureInfo.InvariantCulture);
            }
        }
    }

    public class SimpleHostObject : ITaskHost
    {

    }

    public class SimpleBuildEngine : IBuildEngine5
    {
        public bool IsRunningMultipleNodes => throw new NotImplementedException();

        public bool ContinueOnError => throw new NotImplementedException();

        public int LineNumberOfTaskNode => 0;

        public int ColumnNumberOfTaskNode => 0;

        public string ProjectFileOfTaskNode => "a project";

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
