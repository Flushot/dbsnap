using System;
using System.Text;

namespace DbSnap.Util
{
    /// <summary>
    /// String utilities
    /// </summary>
    public static class StringUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static String NormalizeFilename(String fileName)
        {
            return NormalizeFilename(fileName, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static String NormalizeFilename(String fileName, bool ignoreDot)
        {
            if (fileName == null)
                return null;

            const String reservedChars = "<>:\"/\\|?*";
            StringBuilder normalized = new StringBuilder();
            foreach (char c in fileName)
            {
                if (c == '[' || c == ']')
                    continue;

                bool hasReserved = false;
                foreach (char rc in reservedChars)
                    if (c == rc || (!ignoreDot && c == '.'))
                        hasReserved = true;

                normalized.Append(hasReserved ? '_' : c);
            }

            return normalized.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="needle"></param>
        /// <param name="haystack"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static bool IsInArray(String needle, String[] haystack, bool ignoreCase)
        {
            if (needle == null || haystack == null)
                return false;

            foreach (String inHaystack in haystack)
                if (String.Compare(needle, inHaystack, ignoreCase) == 0)
                    return true;

            return false;
        }
    }
}
