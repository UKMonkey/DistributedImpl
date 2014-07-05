using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

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


        public static Assembly LoadDll(String path)
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            return Assembly.LoadFrom(path);
        }


        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string s = @"C:\lib\" + args.Name.Remove(args.Name.IndexOf(',')) + ".dll";
            return Assembly.LoadFile(s);
        }


        public static T GetNewTypeFromDll<T>(Assembly assm)
            where T : class
        {
            List<Type> types;
            try
            {
                var type = typeof(T);
                var typesMid = assm.GetTypes().
                    Where(p => p.IsClass).
                    Where(p => p.FullName != null && !p.FullName.StartsWith("System")).ToList();
                types = typesMid.Where(type.IsAssignableFrom).ToList();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Unable to get the target type - at a guess it's using an old version of the interface dll");
                return null;
            }


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
