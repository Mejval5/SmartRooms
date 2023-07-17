using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SmartRooms.Utils
{
    public static class ListUtils
    {
        public static IEnumerable<T> ShuffleList<T>(this List<T> list)
        {
            return list.OrderBy(_ => Random.Range(0f, 1f));
        }
        public static IEnumerable<T> ShuffleList<T>(this List<T> list, Unity.Mathematics.Random random)
        {
            return list.OrderBy(_ => random.NextFloat(0f, 1f));
        }
    }
}