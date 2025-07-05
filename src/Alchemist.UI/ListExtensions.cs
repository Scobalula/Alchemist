using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alchemist.UI
{
    public static class ListExtensions
    {
        public static void MoveToNext<T>(this IList<T> list, T element)
        {
            int index = list.IndexOf(element);

            if (index == -1 || index == list.Count - 1)
                return;

            int newPosition = index + 1;

            (list[index], list[newPosition]) = (list[newPosition], list[index]);
        }

        public static void MoveToPrevious<T>(this IList<T> list, T element)
        {
            int index = list.IndexOf(element);

            if (index == -1 || index == 0)
                return;

            int newPosition = index - 1;

            (list[index], list[newPosition]) = (list[newPosition], list[index]);
        }
    }
}
