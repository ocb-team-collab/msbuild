// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Microsoft.Build.Framework
{
    /// <summary>
    /// This interface defines a "task" in the build system that is completely skipped for Static builds. 
    /// </summary>
    public interface ITaskStaticSkip : ITask
    {
    }
}
