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

        [MenuItem("Custom/CheckEditor")]
        [InitializeOnEnterPlayMode]
        public static bool CheckEditorFields()
        {
            bool passed = true;

            //get all classes of assembly where there are 'SetInEditor' attributes
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(type => type.GetFields().Where(field => field.GetCustomAttributes<SetInEditorAttribute>(true).Any()).Any()))
            {
                //find all scene references of those classes
                UnityEngine.Object[] instances = UnityEngine.Object.FindObjectsOfType(type);

                if (instances.Length == 0)
                {
                    continue;
                }
                
                //check every field with the attribute for every instance if there are null values
                foreach (FieldInfo info in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Where(field => field.GetCustomAttributes<SetInEditorAttribute>(true).Any()).ToArray())
                {
                    //TODO write applicable types check as a seperate roslyn analyzer
                    
                    //Get all types that are not displayed in editor and throw error because attribute is not needed and should not be used
                    if ((!info.IsPublic && !info.GetCustomAttributes<SerializableAttribute>().Any()) || info.IsStatic)
                    {
                        Debug.LogError($"You can only set 'SetInEditor' Attribute on public or serialized fields and properties. Error on {info.Name}");
                        continue;
                    }
                    
                    //filter all fields that are not unity objects and throw error for wrong attribute use
                    if (!info.FieldType.IsSubclassOf(typeof(UnityEngine.Object)))
                    {
                        //make exception for standard serialized list for unity editor
                        if (info.FieldType.IsGenericType && (info.FieldType.GetGenericTypeDefinition() == typeof(List<>)))
                        {
                            //but only if the generic type of the list is a Unity.Object
                            if (!info.FieldType.GetGenericArguments()[0].IsSubclassOf(typeof(UnityEngine.Object)))
                            {
                                Debug.LogError("You cannot add this attribute to a list with items not deriving from UnityEngine.Object");
                                continue;
                            }

                            foreach (UnityEngine.Object instance in instances)
                            {
                                IEnumerable obj = (IEnumerable)info.GetValue(instance);

                                foreach (object item in obj)
                                {
                                    if (item is null || (UnityEngine.Object)item == null)
                                    {
                                        passed = false;
                                        Debug.LogError($"The field <color=#3293a8>'{info.Name}'</color> of gameobject <color=#32a873>'{((MonoBehaviour)instance).gameObject}'</color> has to be set in the editor!");
                                    }
                                }
                            }

                            continue;
                        }
                        //make exception for standard array for unity editor
                        else if (info.FieldType.IsSubclassOf(typeof(Array)))
                        {
                            //but only if the type stored in the array derives from Unity.Object
                            if (!info.FieldType.GetElementType().IsSubclassOf(typeof(UnityEngine.Object)))
                            {
                                Debug.LogError($"Cannot add 'SetInEditor' Attribute on list or array with elements not deriving from 'UnityEngine.Object'");
                                continue;
                            }

                            foreach (UnityEngine.Object instance in instances)
                            {
                                object[] obj = (object[])info.GetValue(instance);

                                foreach (object item in obj)
                                {
                                    if (item is null || (UnityEngine.Object)item == null)
                                    {
                                        passed = false;
                                        Debug.LogError($"The field <color=#3293a8>'{info.Name}'</color> of gameobject <color=#32a873>'{((MonoBehaviour)instance).gameObject}'</color> has to be set in the editor!");
                                    }
                                }
                            }

                            continue;
                        }
                        //throw error for wrong attribute use
                        else
                        {
                            Debug.LogError($"You can only set 'SetInEditor' Attribute on types derived from 'UnityEngine.Object.' Error on: {info.Name}");
                            continue;
                        }
                    }

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
        [MenuItem("Custom/CheckEditorFields")]
        //[InitializeOnEnterPlayMode]
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
                EditorApplication.isPlaying = false;
            }

            return passed;
        }
    }
}