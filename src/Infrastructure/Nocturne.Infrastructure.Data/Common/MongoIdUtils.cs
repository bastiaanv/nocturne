namespace Nocturne.Infrastructure.Data.Common
{
    /// <summary>
    /// Utility methods for working with MongoDB ObjectIds
    /// </summary>
    public static class MongoIdUtils
    {
        /// <summary>
        /// Validates if a string is a valid MongoDB ObjectId (24-character hex string)
        /// </summary>
        /// <param name="id">The string to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidMongoId(string? id)
        {
            // MongoDB ObjectId is a 24-character hex string
            return !string.IsNullOrEmpty(id)
                && id.Length == 24
                && System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-fA-F0-9]{24}$");
        }

        /// <summary>
        /// Unwraps an ObjectId(...) wrapper format commonly found in older Nightscout data.
        /// Returns the bare 24-character hex string if wrapped, or the original value if not.
        /// </summary>
        /// <param name="id">The ID string, possibly wrapped as "ObjectId(hex24)"</param>
        /// <returns>The unwrapped ID, or the original value if not wrapped</returns>
        public static string? UnwrapObjectId(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return id;

            if (id.StartsWith("ObjectId(", StringComparison.Ordinal) && id.EndsWith(')'))
            {
                return id.Substring(9, id.Length - 10);
            }

            return id;
        }
    }
}
