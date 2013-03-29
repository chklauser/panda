using System;
using System.Collections.Generic;
namespace Panda.Core
{
    public class PathUtil
    {
        // TODO implement path parsing in this class
 
        // one example would be to take a relative path as a String
        // and turn it into an array of "path segments".

        // The Navigate implementation can then "react" to each of these segments
        //  i.e. for a "..", move up, for a  "." do nothing, otherwise lookup the entry

        /// <summary>
        /// Returns true if path is absolute.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool isAbsolutePath(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            
            return path.Substring(0, 1) == VirtualFileSystem.SeparatorChar.ToString();
        }

        /// <summary>
        /// Takes a path string and parses (splits) it into an array. Additionally it checks if the path as path is valid (not if all things exist).
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string[] parsePath(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            
            // split the path on SeparatorChar
            string[] pathArray = path.Split(VirtualFileSystem.SeparatorChar);

            // check for illegal names (e.g. "//asdf" is not a valid path, "/asdf/" also not)
            foreach (string element in pathArray) {
                if (string.IsNullOrEmpty(element)) {
                    throw new IllegalPathException("Found " + VirtualFileSystem.SeparatorChar.ToString() + VirtualFileSystem.SeparatorChar.ToString() + " in path.");
                }
            }

            return pathArray;
        }
    }
}