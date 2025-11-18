using System;
using System.Collections.Generic;
using System.Linq;

namespace Faster.EventBus.Extensions
{
    /// <summary>
    /// Provides extension methods for IEnumerable&lt;T&gt;.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Returns distinct elements from a sequence according to a specified key selector.
        /// This method mimics the behavior found in MoreLinq or modern .NET.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by the keySelector.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>
        /// An IEnumerable&lt;TSource&gt; that contains distinct elements from the source sequence.
        /// </returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            // HashSet is used to efficiently track the keys encountered so far.
            // This is O(N) complexity for the entire operation.
            HashSet<TKey> seenKeys = new HashSet<TKey>();

            foreach (TSource element in source)
            {
                TKey key = keySelector(element);

                // TryAdd is a method from modern .NET/C# 7.3+, but is often implemented 
                // in Framework 4.8 via the standard Add check pattern:
                if (seenKeys.Add(key))
                {
                    // If the key was successfully added (meaning it was not seen before),
                    // yield the current element.
                    yield return element;
                }
            }
        }
    }
}