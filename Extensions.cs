using System.Collections.Generic;

namespace RigidViewer
{
    public static class Extensions
    {
        // Method to quickly remove an element from making use of the fact that the order of the elements in the list is irrelevant
        // The key idea is that it is faster to remove elements at the end of an array-based list than removing them towards the first element
        public static bool FastRemove<T>(this List<T> list, T item) where T : class
        {
            int l = list.Count;
            for (int i = 0; i < l; ++i)
            {
                T el = list[i];
                //if(item.Equals(el))
                if (item == el)
                {
                    list[i] = list[l - 1]; //Overwrites the current element to delete with the last element in the list
                    list.RemoveAt(l - 1); //Since we already copied the last element, we can now remove i at the end
                    return true;
                }
            }
            return false;
        }
    }
}
