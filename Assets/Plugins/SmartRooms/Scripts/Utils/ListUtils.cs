﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SmartRooms.Utils
{
    public class ListUtils
    {
        public static IEnumerable<T> ShuffleList<T>(List<T> list)
        {
            return list.OrderBy(_ => Random.Range(0f, 1f));
        }
    }
}