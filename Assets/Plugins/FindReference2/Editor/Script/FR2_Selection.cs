using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace vietlabs.fr2
{
    public class FR2_Selection
    {
        public static HashSet<string> h = new HashSet<string>();

        public static void Commit()
        {
            var list = new List<Object>();
            foreach (var guid in h)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                // var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                var obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
                list.Add(obj);                                
            }

            Selection.objects = list.ToArray();
        }
                      
    }
}

