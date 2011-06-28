using System;
using System.Text;
using System.IO;

namespace DbSnap.Util
{
    /// <summary>
    /// Path utilities
    /// </summary>
    public static class PathUtils
    {
        /// <summary>
        /// Converts an absolute path to a relative path.
        /// </summary>
        /// <param name="destinationPath"></param>
        /// <param name="startPath"></param>
        /// <returns>Relative path</returns>
        public static String GetRelativePath(String destinationPath, String startPath)
        {
            String[] startPathParts = Path.GetFullPath(startPath).Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
            String[] destinationPathParts = destinationPath.Split(Path.DirectorySeparatorChar);

            int sameCounter = 0;
            while ((sameCounter < startPathParts.Length) && 
                   (sameCounter < destinationPathParts.Length) && 
                    startPathParts[sameCounter].Equals(
                        destinationPathParts[sameCounter], 
                        StringComparison.InvariantCultureIgnoreCase))
                sameCounter++;

            if (sameCounter == 0)
                return destinationPath; // There is no relative link.

            StringBuilder builder = new StringBuilder();
            for (int i = sameCounter; i < startPathParts.Length; ++i)
                builder.Append(".." + Path.DirectorySeparatorChar);

            for (int i = sameCounter; i < destinationPathParts.Length; ++i)
                builder.Append(destinationPathParts[i] + Path.DirectorySeparatorChar);

            --builder.Length;
            return builder.ToString();
        }
    }
}
