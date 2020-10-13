using System;
using System.Collections.Generic;
using System.Text;

namespace LunaServer.Utilities
{
    public static class ArrayExtensions
    {
        public static IEnumerable<T[]> AsChunks<T>(this T[] source, int chunkMaxSize)
        {
            var chunks = source.Length / chunkMaxSize;
            var leftOver = source.Length % chunkMaxSize;
            var result = new List<T[]>(chunks + 1);
            var offset = 0;

            for (var i = 0; i < chunks; i++)
            {
                result.Add(new ArraySegment<T>(source, offset, chunkMaxSize).ToArray());
                offset += chunkMaxSize;
            }

            if (leftOver > 0)
            {
                result.Add(new ArraySegment<T>(source, offset, leftOver).ToArray());
            }

            return result;
        }
    }
}
