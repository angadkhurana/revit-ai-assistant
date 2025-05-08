using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitGpt.Functions
{
    /// <summary>
    /// Contains utility functions used across multiple Revit function classes
    /// </summary>
    public static class CommonFunctions
    {
        /// <summary>
        /// Helper method to convert ElementIds to string representation
        /// </summary>
        public static List<string> ConvertElementIdsToStrings(List<ElementId> elementIds)
        {
            List<string> idStrings = new List<string>();
            foreach (ElementId id in elementIds)
            {
                idStrings.Add(id.Value.ToString());
            }
            return idStrings;
        }

        /// <summary>
        /// Calculates Levenshtein distance between two strings for fuzzy matching
        /// </summary>
        public static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0)
                return m;

            if (m == 0)
                return n;

            for (int i = 0; i <= n; i++)
                d[i, 0] = i;

            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            for (int j = 1; j <= m; j++)
            {
                for (int i = 1; i <= n; i++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
    }
}