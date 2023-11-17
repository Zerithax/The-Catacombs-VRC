using System;

namespace Argus
{
    public static class ArrayMethodExtensions
    {
        public static T[] Prepend<T>(this T[] array, T item)
        {
            T[] newArray = new T[array.Length + 1];
            newArray[0] = item;
            Array.Copy(array, 0, newArray, 1, array.Length);
            return newArray;
        }

        public static T[] Add<T>(this T[] array, T item)
        {
            T[] newArray = new T[array.Length + 1];
            newArray[array.Length] = item;
            Array.Copy(array, newArray, array.Length);
            return newArray;
        }

        public static T[] Remove<T>(this T[] array, T item)
        {
            int index = Array.IndexOf(array, item);
            if (index == -1)
                return array;
		
            T[] newArray = new T[array.Length - 1];
            Array.Copy(array, newArray, index);
            Array.Copy(array, index + 1, newArray, index, array.Length - index - 1);
            return newArray;
        }
        
        public static T[] RemoveAt<T>(this T[] array, int index)
        {
            if (index == -1)
                return array;
		
            T[] newArray = new T[array.Length - 1];
            Array.Copy(array, newArray, index);
            Array.Copy(array, index + 1, newArray, index, array.Length - index - 1);
            return newArray;
        }

        public static bool Contains<T>(this T[] array, T item) => Array.IndexOf(array, item) != -1;
        public static int IndexOf<T>(this T[] array, T item) => Array.IndexOf(array, item);
    }
}