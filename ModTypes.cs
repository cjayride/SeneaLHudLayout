using System;
using System.Reflection;
using UnityEngine;

namespace SeneaLHudLayout
{
    /// <summary>
    /// Silent type/member lookup. AccessTools.Method/Field/TypeByName log HarmonyX
    /// "Could not find" warnings for optional third-party APIs; these do not.
    /// </summary>
    static class ModTypes
    {
        const BindingFlags Declared =
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic;

        public static Type Find(string assemblyName, string fullName)
        {
            Assembly[] loaded;
            try
            {
                loaded = AppDomain.CurrentDomain.GetAssemblies();
            }
            catch (Exception)
            {
                return null;
            }

            for (int i = 0; i < loaded.Length; i++)
            {
                Assembly assembly = loaded[i];
                string name;
                try
                {
                    name = assembly.GetName().Name;
                }
                catch (Exception)
                {
                    continue;
                }

                if (name != assemblyName)
                {
                    continue;
                }

                try
                {
                    return assembly.GetType(fullName, false);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }

        public static Type FindAny(params string[] fullNames)
        {
            if (fullNames == null)
            {
                return null;
            }

            Assembly[] loaded;
            try
            {
                loaded = AppDomain.CurrentDomain.GetAssemblies();
            }
            catch (Exception)
            {
                return null;
            }

            for (int n = 0; n < fullNames.Length; n++)
            {
                string fullName = fullNames[n];
                if (string.IsNullOrEmpty(fullName))
                {
                    continue;
                }

                for (int i = 0; i < loaded.Length; i++)
                {
                    try
                    {
                        Type type = loaded[i].GetType(fullName, false);
                        if (type != null)
                        {
                            return type;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return null;
        }

        public static MethodInfo DeclaredMethod(Type type, string name, Type[] parameters = null)
        {
            if (type == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            try
            {
                MethodInfo[] methods = type.GetMethods(Declared);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != name)
                    {
                        continue;
                    }

                    if (parameters == null)
                    {
                        return method;
                    }

                    ParameterInfo[] args = method.GetParameters();
                    if (args.Length != parameters.Length)
                    {
                        continue;
                    }

                    bool match = true;
                    for (int j = 0; j < args.Length; j++)
                    {
                        if (parameters[j] != null && args[j].ParameterType != parameters[j])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        return method;
                    }
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        public static MethodInfo Method(Type type, string name, Type[] parameters = null)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                MethodInfo method = DeclaredMethod(t, name, parameters);
                if (method != null)
                {
                    return method;
                }
            }

            return null;
        }

        public static MethodInfo FirstMethod(Type type, params string[] names)
        {
            if (names == null)
            {
                return null;
            }

            for (int i = 0; i < names.Length; i++)
            {
                MethodInfo method = Method(type, names[i]);
                if (method != null)
                {
                    return method;
                }
            }

            return null;
        }

        public static FieldInfo DeclaredField(Type type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            try
            {
                return type.GetField(name, Declared);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static FieldInfo Field(Type type, string name)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo field = DeclaredField(t, name);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        public static FieldInfo FirstField(Type type, params string[] names)
        {
            if (names == null)
            {
                return null;
            }

            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo field = Field(type, names[i]);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        public static void LogMissing(string feature, string target)
        {
            Debug.Log("[SeneaL HUD Layout] " + feature + ": " + target + " not found; skipping.");
        }
    }
}
