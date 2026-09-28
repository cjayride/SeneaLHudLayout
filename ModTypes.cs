using System;
using System.Reflection;

namespace SeneaLHudLayout
{
    static class ModTypes
    {
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
    }
}
