// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Microsoft.Build.Framework
{
    /// <summary>
    /// This interface defines a static "task" in the build system that can execute while generating a static graph of build actions.  
    /// </summary>
    public interface ITaskHybrid : ITask
    {
        /// <summary>
        /// This method is called by the build engine to begin static task execution for tasks that run both during graph creation and during the build.
        /// A task uses the return value to indicate whether it was successful. If a task throws an exception out of this method, the engine will
        /// automatically assume that the task has failed.
        /// </summary>
        /// <returns>true, if successful</returns>
        bool ExecuteStatic();
    }
}
