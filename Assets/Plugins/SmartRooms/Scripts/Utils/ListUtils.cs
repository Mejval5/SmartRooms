using System.Collections.Generic;
using UnityEngine;

namespace SmartRooms.Utils
{
    public class ListUtils
    {
        public static List<T> ShuffleList<T>(List<T> _list)
        {
            for (int i = 0; i < _list.Count; i++)
            {
                T temp = _list[i];
                int randomIndex = Random.Range(i, _list.Count);
                _list[i] = _list[randomIndex];
                _list[randomIndex] = temp;
            }

            return _list;
        }
    }
}