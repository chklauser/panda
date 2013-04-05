using System;
using JetBrains.Annotations;
using Panda.Core;

namespace Panda
{
    public static class VirtualFileSystem
    {
        public const uint DefaultBlockSize = 4096;

        /// <summary>
        /// The path separator used by the Panda virtual file system.
        /// </summary>
        [PublicAPI]
        public const char SeparatorChar = '/';

        /// <summary>
        /// Checks file-/directory name, throws exception if not legal.
        /// </summary>
        /// <param name="nodeName"></param>
        public static void CheckNodeName(string nodeName)
        {
            if (!IsLegalNodeName(nodeName))
            {
                throw new IllegalNodeNameException();
            }
        }

        /// <summary>
        /// Checks for separator chars in the file-/directory name. If there is any, the file name is invalid.
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        public static Boolean IsLegalNodeName(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName) || nodeName.IndexOf(VirtualFileSystem.SeparatorChar) != -1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}