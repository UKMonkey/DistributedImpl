using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace DistributedShared.SystemMonitor
{
    public class DllHelper
    {
        public static String GetDllExtension()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    return ".so";
                default:
                    return ".dll";
            }
        }


        public T GetNewTypeFromDll<T>(Assembly assm)
            where T : class
        {
            var type = typeof(T);
            var typesMid = assm.GetTypes().
                Where(p => p.IsClass).
                Where(p => !p.FullName.StartsWith("System")).ToList();
            var types = typesMid.Where(type.IsAssignableFrom).ToList();

            if (types.Count == 0)
                return null;
            if (types.Count > 1)
                throw new Exception("Unable to load dll as the number of valid IDllApi items = " + types.Count);

            var constructor = types[0].GetConstructor(Type.EmptyTypes);
            Debug.Assert(constructor != null, "Dll Job Worker has no default constructor");
            return (T)constructor.Invoke(null);
        }
    }
}
