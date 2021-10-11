using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Build.BackEnd;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using Microsoft.Build.Utilities;

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

            if (Environment.GetEnvironmentVariable("MICROSOFT_BUILD_TASKLAUNCHER_DEBUG") == "1")
            {
                while(!Debugger.IsAttached)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }

            if (args[0].Equals("run", StringComparison.OrdinalIgnoreCase))
            {
                return RunTarget();
            }
            else if (args[0].Equals("print", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 4)
                {
                    Usage();
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("pkgdomino")))
                {
                    Console.WriteLine("%PKGDOMINO% must be set to root of Domino folder");
                    return 1;
                }

                StaticGraphToDScript(args[1], args[2], args[3]);
                return 0;
            }
            else if (args[0].Equals("meta", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 5)
                {
                    Usage();
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("pkgdomino")))
                {
                    Console.WriteLine("%PKGDOMINO% must be set to root of Domino folder");
                    return 1;
                }

                MetagraphCreator(args[1], args[2], args[3], args[4]);
                return 0;
            }
            else
            {
                Usage();
                return 1;
            }
        }

        private static void MetagraphCreator(string sourceDirectory, string msBuild, string graphDirectory, string outputDirectory)
        {
            Mount srcMount = new Mount
            {
                Name = "Src",
                Path = new DirectoryInfo(sourceDirectory).Parent.Parent.FullName,
                IsReadable = true,
                IsWritable = false,
                TrackSourceFileChanges = true
            };

            Mount outputMount = new Mount
            {
                Name = "Output",
                Path = outputDirectory,
                IsReadable = true,
                IsWritable = true,
                TrackSourceFileChanges = true
            };

            Mount msbuildMount = new Mount
            {
                Name = "MsbuildSrc",
                Path = Path.GetDirectoryName(msBuild),
                IsReadable = true,
                IsWritable = false,
                TrackSourceFileChanges = true
            };

            string projectFile = Directory.EnumerateFiles(sourceDirectory).Single(t => t.EndsWith("proj"));

            List<Mount> mounts = new List<Mount>() { TaskLauncherMount, ProgramDataMount, BreadcrumbStoreMount, ProgramFilesx86Mount, msbuildMount, srcMount, outputMount };
            WriteConfigDsc(graphDirectory, mounts);
            WriteModuleConfigDsc(graphDirectory);
            PrintMetabuildSpec(projectFile, msBuild, graphDirectory, outputDirectory);

            Console.WriteLine(@"%PKGDOMINO%\bxl.exe /c:" + graphDirectory + @"\config.dsc");
            Console.WriteLine(Assembly.GetExecutingAssembly().Location + @" print " + outputDirectory + "\\graph.json " + sourceDirectory);
        }

        private static void StaticGraphToDScript(string graphFile, string graphDirectory, string outputDirectory)
        {
            StaticGraph graph = ReadStaticGraph(graphFile);
            PrintPips(graph, graphDirectory, outputDirectory);

            Mount everythingMount = new Mount
            {
                Name = "Everything",
                Path = Path.GetPathRoot(Path.GetFullPath(graph.ProjectPath)),
                IsReadable = true,
                IsWritable = true,
                TrackSourceFileChanges = true
            };

            Mount srcMount = new Mount
            {
                Name = "Src",
                Path = Path.GetDirectoryName(graph.ProjectPath),
                IsReadable = true,
                IsWritable = false,
                TrackSourceFileChanges = true
            };

            Mount objMount = new Mount
            {
                Name = "Obj",
                Path = Path.Combine(Path.GetDirectoryName(graph.ProjectPath), "obj"),
                IsReadable = true,
                IsWritable = true,
                TrackSourceFileChanges = true
            };

            Mount outputMount = new Mount
            {
                Name = "Output",
                Path = outputDirectory,
                IsReadable = true,
                IsWritable = true,
                TrackSourceFileChanges = true
            };

            Mount binMount = new Mount
            {
                Name = "Bin",
                Path = Path.Combine(Path.GetDirectoryName(graph.ProjectPath), "bin"),
                IsReadable = true,
                IsWritable = true,
                TrackSourceFileChanges = true
            };

            List<Mount> mounts = new List<Mount>() { everythingMount, TaskLauncherMount, ProgramDataMount, BreadcrumbStoreMount, srcMount, objMount, binMount, outputMount };

            WriteConfigDsc(graphDirectory, mounts);
            WriteModuleConfigDsc(graphDirectory);
            Console.WriteLine(@"%PKGDOMINO%\bxl.exe /c:" + graphDirectory + @"\config.dsc");
        }

        private static Mount TaskLauncherMount = new Mount
        {
            Name = "TaskLauncherSrc",
            Path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            IsReadable = true,
            IsWritable = false,
            TrackSourceFileChanges = true
        };

        private static Mount ProgramDataMount = new Mount
        {
            Name = "ProgramData",
            Path = Environment.GetEnvironmentVariable("ProgramData"),
            IsReadable = true,
            IsWritable = false,
            TrackSourceFileChanges = true
        };

        private static Mount BreadcrumbStoreMount = new Mount
        {
            Name = "BreadcrumbStore",
            Path = Path.Combine(Environment.GetEnvironmentVariable("ProgramData"), "Microsoft", "NetFramework", "BreadcrumbStore"),
            IsReadable = true,
            IsWritable = false,
            TrackSourceFileChanges = false
        };

        private static Mount ProgramFilesx86Mount = new Mount
        {
            Name = "ProgramFilesx86",
            Path = Environment.GetEnvironmentVariable("ProgramFiles(x86)"),
            IsReadable = true,
            IsWritable = false,
            TrackSourceFileChanges = true
        };

        private static void Usage()
        {
            List<string> lines = new List<string>()
            {
                "Usage:",
                "\trun < targetJson\t\t\tRuns a target given its json description.",
                "\tprint graphJsonFile outputFolder\tPrints DominoScript into the output folder for a given graph json.",
                "\tmeta projFile outputFolder msbuildPath\t\tPrints DominoScript into the output folder to run metabuild for a particular proj file"
            };

            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }
        }

        private static StaticGraph ReadStaticGraph(string file)
        {
            StaticGraph graph;
            var ser = new DataContractJsonSerializer(typeof(StaticGraph));
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                graph = (StaticGraph)ser.ReadObject(stream);
            }

            return graph;
        }

        private static int PrintPips(StaticGraph graph, string outputFolder, string outputDirectory)
        {
            StringBuilder specContents = new StringBuilder();
            specContents.AppendLine("import {Cmd, Transformer} from \"Sdk.Transformers\";\n");
            specContents.AppendLine(
                string.Format("const tool: Transformer.ToolDefinition = {{ exe: f`{0}`, dependsOnWindowsDirectories: true, prepareTempDirectory: true, runtimeDirectoryDependencies: [ Transformer.sealSourceDirectory(d`{1}`, Transformer.SealSourceDirectoryOption.allDirectories) ] }};\n",
                    System.Reflection.Assembly.GetExecutingAssembly().Location,
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)));

            var ser = new DataContractJsonSerializer(typeof(StaticTarget));

            int i = 0;
            foreach (var target in graph.StaticTargets)
            {
                StringBuilder stdIn = new StringBuilder();
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

                List<string> inputs = new List<string>()
                {
                    $"Transformer.sealSourceDirectory(d`{Environment.GetEnvironmentVariable("ProgramData")}`, Transformer.SealSourceDirectoryOption.allDirectories)"
                };

                foreach (var inputId in target.InputFileIds ?? Enumerable.Empty<long>())
                {
                    var staticFile = graph.Files.First(file => file.Id == inputId);
                    if (!staticFile.ProducingTargetId.HasValue)
                    {
                        inputs.Add($"f`{staticFile.Path}`");
                    }
                    else
                    {
                        inputs.Add($"target{staticFile.ProducingTargetId}.getOutputFile(p`{staticFile.Path}`)");    
                    }
                }

                foreach (var task in target.Tasks)
                {
                    if (!string.IsNullOrWhiteSpace(task.AssemblyFile))
                    {
                        inputs.Add($"f`{task.AssemblyFile}`");
                    }
                }

                List<string> outputs = new List<string>();
                foreach (var outputId in target.OutputFileIds ?? Enumerable.Empty<long>())
                {
                    outputs.Add($"p`{graph.Files[(int)outputId].Path}`");
                }

                var envVars = new List<(string, string)>()
                {
                    ("ProgramData", Environment.GetEnvironmentVariable("ProgramData"))
                };

                specContents.AppendLine(
                    $@"const {"target" + target.Id} = Transformer.execute(
{{
	tool: tool,
	arguments: [ Cmd.rawArgument(""run"") ],
	consoleInput: ""{NormalizeRawString(stdIn.ToString(), stdIn)}"",
	description: ""{target.Name + "_" + NormalizeRawString(target.LocationString, new StringBuilder())}"",
	workingDirectory: d`{Path.GetDirectoryName(graph.ProjectPath)}`,
    environmentVariables: [{string.Join(",\n", envVars.Select(envVar => $"\t{{ name: \"{envVar.Item1}\", value: \"{envVar.Item2}\" }}"))}],
	consoleOutput: p`{Path.Combine(outputDirectory, "target" + i + ".out")}`,
	dependencies: [
{string.Join(",\n\t\t", inputs)}
	],
	implicitOutputs: [{string.Join(",\n\t\t", outputs)}]
}});
");
                i++;

            }

            string specFile = Path.Combine(outputFolder, "spec.dsc");
            File.Delete(specFile);
            File.WriteAllText(specFile, specContents.ToString());

            return 0;
        }

        private static void PrintMetabuildSpec(string projectFile, string msBuild, string graphDirectory, string outputDirectory)
        {
            projectFile = Path.GetFullPath(projectFile);
            DirectoryInfo projFolder = Directory.GetParent(projectFile);

            List<string> inputs = new List<string>()
            {
                $"f`{Path.Combine(Path.GetDirectoryName(projectFile), "app.config")}`",
                // $"Transformer.sealSourceDirectory(d`{Path.GetDirectoryName(projFile)}`, Transformer.SealSourceDirectoryOption.allDirectories)",
                $"Transformer.sealSourceDirectory(d`{ProgramFilesx86Mount.Path}`, Transformer.SealSourceDirectoryOption.allDirectories)",
                $"Transformer.sealSourceDirectory(d`{Path.GetDirectoryName(msBuild)}`, Transformer.SealSourceDirectoryOption.allDirectories)",
                $"Transformer.sealSourceDirectory(d`{Environment.GetEnvironmentVariable("ProgramData")}`, Transformer.SealSourceDirectoryOption.allDirectories)",
                $"Transformer.sealSourceDirectory(d`{Path.GetDirectoryName(projectFile)}`, Transformer.SealSourceDirectoryOption.allDirectories)",
            };

            for (int i = 0; i < 3; ++i)
            {
                inputs.Add($"f`{Path.Combine(projFolder.FullName, "Directory.Build.rsp")}`");
                inputs.Add($"f`{Path.Combine(projFolder.FullName, "Directory.Build.props")}`");
                inputs.Add($"f`{Path.Combine(projFolder.FullName, ".editorconfig")}`");
                inputs.Add($"f`{Path.Combine(projFolder.FullName, "Directory.Build.targets")}`");

                projFolder = projFolder.Parent;
            }

            var outputGraph = $"{outputDirectory}\\graph.json";

            List<string> outputs = new List<string>()
            {
                $"f`{outputGraph}`"
            };

            List<(string, string)> envVars = new List<(string, string)>()
            {
                ("MSBUILDSTATIC", "1"),
                ("MSBUILDSTATIC_OUTPUT", NormalizeRawString(outputGraph, new StringBuilder())),
                ("ProgramData", Environment.GetEnvironmentVariable("ProgramData"))
            };

            StringBuilder specContents = new StringBuilder();
            specContents.AppendLine("import {Artifact, Cmd, Transformer} from \"Sdk.Transformers\";\n");
            specContents.AppendLine(
                string.Format("const tool: Transformer.ToolDefinition = {{ exe: f`{0}`, dependsOnWindowsDirectories: true, prepareTempDirectory: true, runtimeDirectoryDependencies: [ Transformer.sealSourceDirectory(d`{1}`, Transformer.SealSourceDirectoryOption.allDirectories) ] }};\n",
                    msBuild,
                    Path.GetDirectoryName(msBuild)));
            specContents.AppendLine(
                string.Format("const {0} = Transformer.execute(\n{{\n\ttool: tool,\n\targuments: [ Cmd.rawArgument(\"{1}\") ],\n\tenvironmentVariables: [{2}],\n\tdescription: \"{3}\",\n\tworkingDirectory: d`{4}`,\n\tconsoleOutput: p`{5}`,\n\tdependencies: [{6}],\n\timplicitOutputs: [{7}]\n}});\n",
                    "msbuild0",
                    NormalizeRawString(projectFile, new StringBuilder()),
                    string.Join(",\n", envVars.Select(envVar => $"\t{{ name: \"{envVar.Item1}\", value: \"{envVar.Item2}\" }}")),
                    "Running static msbuild for: " + projectFile,
                    Path.GetDirectoryName(projectFile),
                    Path.Combine(outputDirectory, "msbuild0.out"),
                    string.Join(",\n\t\t", inputs),
                    string.Join(",\n\t\t", outputs)));
            string specFile = Path.Combine(graphDirectory, "spec.dsc");
            File.Delete(specFile);
            File.WriteAllText(specFile, specContents.ToString());
        }

        private class Mount
        {
            public string Name { get; set; }

            public string Path { get; set; }

            public bool TrackSourceFileChanges { get; set; }

            public bool IsWritable { get; set; }

            public bool IsReadable { get; set; }

        }

        private static void WriteConfigDsc(string outputFolder, List<Mount> mounts)
        {
            StringBuilder configBuilder = new StringBuilder();
            configBuilder.AppendLine("config({");
            configBuilder.AppendLine("\tmodules: [ f`module.config.dsc` ],");
            configBuilder.AppendLine("\tresolvers: [{");
            configBuilder.AppendLine("\t\tkind: \"SourceResolver\",");
            configBuilder.AppendLine($"\t\tpackages: [ f`{Environment.GetEnvironmentVariable("pkgdomino")}/sdk/sdk.transformers/package.config.dsc` ]");
            configBuilder.AppendLine("\t}],");
            configBuilder.AppendLine("\tmounts: [");
            foreach (var mount in mounts)
            {
                string mountLine = string.Format("\t\t{{ name: a`{0}`, path: p`{1}`, trackSourceFileChanges: {2}, isWritable: {3}, isReadable: {4} }},",
                    mount.Name,
                    mount.Path,
                    mount.TrackSourceFileChanges.ToString().ToLower(),
                    mount.IsWritable.ToString().ToLower(),
                    mount.IsReadable.ToString().ToLower());
                configBuilder.AppendLine(mountLine);
            }
            configBuilder.AppendLine("\t]\n});");
            string configFile = Path.Combine(outputFolder, "config.dsc");
            File.Delete(configFile);
            File.WriteAllText(configFile, configBuilder.ToString());

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
                Type type;
                if (staticTask.AssemblyFile != null)
                {
                    AssemblyName an = AssemblyName.GetAssemblyName(staticTask.AssemblyFile);
                    if (an == null)
                    {
                        Console.WriteLine("Caouldn't get assembly name for assembly file: " + staticTask.AssemblyFile);
                        return 1;
                    }
                    Assembly a = Assembly.Load(an);
                    if (a == null)
                    {
                        Console.WriteLine("Couldn't loaded assembly for assembly: " + an.FullName + " from file: " + staticTask.AssemblyFile);
                        return 1;
                    }
                    type = a.GetType(staticTask.Name.Split(',')[0]);
                    if (type == null)
                    {
                        Console.WriteLine("Couldn't create type for string: " + staticTask.Name.Split(',')[0] + " assembly file: " + (staticTask.AssemblyFile ?? "null") + " and assembly name: " + (staticTask.AssemblyName ?? "null"));
                        Console.WriteLine("Types in the assembly:\n" + string.Join(",\n", a.GetTypes().Select(availableType => availableType.FullName)));
                        return 1;
                    }
                }
                else
                {
                    type = Type.GetType(staticTask.Name);
                    if (type == null)
                    {
                        Console.WriteLine("Couldn't create type for string: " + staticTask.Name + " assembly file: " + (staticTask.AssemblyFile ?? "null") + " and assembly name: " + (staticTask.AssemblyName ?? "null"));
                        return 1;
                    }
                }

                var assemblyLoadInfo = AssemblyLoadInfo.Create(assemblyFile: staticTask.AssemblyFile, assemblyName: staticTask.AssemblyName);
                if (assemblyLoadInfo == null)
                {
                    Console.WriteLine("Couldn't create type for assembly file: " + (staticTask.AssemblyFile ?? "null") + " and name: " + (staticTask.AssemblyName ?? "null"));
                    return 1;
                }

                LoadedType loadedType = new LoadedType(type, assemblyLoadInfo, null);
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
                    var taskPropertyInfo = taskProperties.FirstOrDefault(property => property.Name == parameter.Key);
                    if (taskPropertyInfo == null)
                    {
                        Console.Error.WriteLine("Could not find property: \"" + parameter.Key + "\" for task + \"" + staticTask.Name + "\"");
                        return 1;
                    }

                    StaticTarget.Task.ParameterType parameterType = parameter.Value.ParameterType;
                    switch (parameterType)
                    {
                        case StaticTarget.Task.ParameterType.Primitive:
                            object value = GetTypedValue(parameter.Value.Primitive.Type, parameter.Value.Primitive.Value);
                            TaskFactoryWrapper.SetPropertyValue(task, taskPropertyInfo, value);
                            break;
                        case StaticTarget.Task.ParameterType.Primitives:
                            var values = GetTypedArrayValue(parameter.Value.Primitives.Type, parameter.Value.Primitives.Values);
                            TaskFactoryWrapper.SetPropertyValue(task, taskPropertyInfo, values);
                            break;
                        case StaticTarget.Task.ParameterType.TaskItem:
                            TaskFactoryWrapper.SetPropertyValue(task, taskPropertyInfo, new TaskItem(parameter.Value.TaskItem.ItemSpec, parameter.Value.TaskItem.Metadata));
                            break;
                        case StaticTarget.Task.ParameterType.TaskItems:
                            ITaskItem[] taskItems = parameter.Value.TaskItems.Select(taskItem => new TaskItem(taskItem.ItemSpec, taskItem.Metadata)).ToArray();
                            TaskFactoryWrapper.SetPropertyValue(task, taskPropertyInfo, taskItems);
                            break;
                    }

                }


                task.BuildEngine = new SimpleBuildEngine();
                try
                {
                    // MSBuild ignores this return value :(
                    task.Execute();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("TASK ERROR: " + e);
                    return 1;
                }
            }

            return 0;
        }

        private static object GetTypedArrayValue(string typeString, IEnumerable<string> stringValues)
        {
            Type primitiveType = Type.GetType(typeString);
            if (primitiveType == typeof(bool[]))
            {
                return stringValues.Select(stringValue => ConversionUtilities.ConvertStringToBool(stringValue)).ToArray();
            }
            else if (primitiveType == typeof(string) || primitiveType == typeof(string[]))
            {
                return stringValues.ToArray();
            }
            else
            {
                return stringValues.Select(stringValue => Convert.ChangeType(stringValue, primitiveType, CultureInfo.InvariantCulture)).ToArray();
            }
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
