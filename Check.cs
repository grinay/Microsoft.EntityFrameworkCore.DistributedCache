using System;

namespace EFCore.AsCaching
{
    /// <summary>
    /// Helper class to check parameter.
    /// </summary>
    public static class Check
    {
        /// <summary>
        /// Check if parameter is <see cref="null"/>.
        /// </summary>
        /// <param name="obj">Parameter value</param>
        /// <param name="name">Parameter nam</param>
        public static void NotNull(object obj, string name)
        {
            if (obj == null)
                throw new ArgumentNullException("name");
        }

        /// <summary>
        /// Check if <see cref="String"/> parameter is empty.
        /// </summary>
        /// <param name="obj">Parameter value</param>
        /// <param name="name">Parameter nam</param>
        public static void NotEmpty(string obj, string name)
        {
            if (String.IsNullOrEmpty(obj))
                throw new ArgumentNullException("name");
        }
    }
}
