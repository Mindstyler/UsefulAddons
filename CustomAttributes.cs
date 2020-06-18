#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using System.Linq;
using UnityEditor;

namespace Com.Mindstyler.Additional
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    internal sealed class ListAllAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    internal sealed class SetInEditorAttribute : Attribute
    {

    }

    /// <summary>
    /// A helperclass to get all specific variables of an declared class. Can be used to work with many variables at once.
    /// </summary>
    public static class AttributeHelper
    {
        /// <summary>
        /// Returns a System.Reflection.FieldInfo[] from the variables, where xxx.GetValue() and xxx.SetValue() can be used with.
        /// </summary>
        /// <typeparam name="T">'T' is the class, where variables should be read from.</typeparam>
        /// <typeparam name="U">'U' is the type of which the variables you want should be. It ignores all variables not of type 'U'</typeparam>
        /// <returns></returns>
        public static FieldInfo[] ListVariables<T, U>()
        {
            return typeof(T).GetFields(/*BindingFlags.Public | BindingFlags.NonPublic*/).Where(field => field.GetCustomAttributes<ListAllAttribute>(true).Any()).Where(field => field.FieldType.Name.Equals(typeof(U).Name)).ToArray();
        }

        public static bool CheckEditorFields()
        {
            bool passed = true;

            //get all classes of assembly where there are 'SetInEditor' attributes
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(type => type.GetFields().Where(field => field.GetCustomAttributes<SetInEditorAttribute>(true).Any()).Any()))
            {
                //find all scene references of those classes
                UnityEngine.Object[] instances = UnityEngine.Object.FindObjectsOfType(type);

                if (instances != null) //Unityobject null check
                {
                    //check every field with the attribute for every instance if there are null values
                    foreach (FieldInfo info in type.GetFields().Where(field => field.GetCustomAttributes<SetInEditorAttribute>(true).Any()).ToArray())
                    {
                        foreach (UnityEngine.Object instance in instances)
                        {
                            object obj = info.GetValue(instance);

                            if (obj is null || (UnityEngine.Object)obj == null)
                            {
                                passed = false;
                                Debug.LogError($"The field <color=#3293a8>'{info.Name}'</color> of gameobject <color=#32a873>'{((MonoBehaviour)instance).gameObject}'</color> has to be set in the editor!");
                            }
                        }
                    }
                }
            }

            if (passed)
            {
                Debug.Log("<color=green>Success.</color> All editor fields are set.");
            }
            else
            {
                //EditorApplication.isPlaying = false;
            }

            return passed;
        }


        /// <summary>
        /// Checks if all fields with 'SetInEditor' attribute on the isntancees are really set in the editor.
        /// </summary>
        /// <returns>True if all fields are set, false if any field is not set.</returns>
        public static bool CheckEditorFieldsLinq()
        {
            bool passed = true;

            foreach ((FieldInfo info, UnityEngine.Object instance) in
                from Type type in Assembly.GetExecutingAssembly().GetTypes().Where(type => type.GetFields().Where(field => field.GetCustomAttributes<SetInEditorAttribute>(true).Any()).Any())
                let instances = UnityEngine.Object.FindObjectsOfType(type)
                where instances != null
                from FieldInfo info in type.GetFields().Where(field => field.GetCustomAttributes<SetInEditorAttribute>(true).Any()).ToArray()
                from UnityEngine.Object instance in instances
                let obj = info.GetValue(instance)
                where obj is null || (UnityEngine.Object)obj == null
                select (info, instance))
            {
                passed = false;
                Debug.LogError($"The field <color=#3293a8>'{info.Name}'</color> of gameobject <color=#32a873>'{((MonoBehaviour)instance).gameObject}'</color> has to be set in the editor!");
            }

            if (passed)
            {
                Debug.Log("<color=green>Success.</color> All editor fields are set.");
            }
            else
            {
                //EditorApplication.isPlaying = false;
            }

            return passed;
        }
    }
}