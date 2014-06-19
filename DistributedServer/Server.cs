using System;
using System.Linq;
using System.Reflection;
using DistributedServerInterfaces.Interfaces;
using System.Threading;
using System.Diagnostics;


namespace DistributedServerDll
{
    public class Server
    {
        static private T GetNewTypeFromDll<T>(AppDomain domain) where T : class
        {
            lock (domain)
            {
                var type = typeof(T);
                var types = domain.GetAssemblies().
                    SelectMany(s => s.GetTypes()).
                    Where(p => p.IsClass).
                    Where(type.IsAssignableFrom).ToList();

                if (types.Count == 0)
                    return null;
                if (types.Count > 1)
                    throw new Exception("Unable to load dll as the number of valid IDllApi items = " + types.Count);

                var constructor = types[0].GetConstructor(Type.EmptyTypes);
                Debug.Assert(constructor != null, "Dll Job Worker has no default constructor");
                return (T)constructor.Invoke(null);
            }
        }

        //http://support.microsoft.com/kb/837908/en-us
        //private Assembly ResolveHandler(object sender, ResolveEventArgs args)
        static private Assembly ResolveHandler(string dllName)
        {
            return Assembly.LoadFrom(dllName);
        }


        static private AppDomain LoadDll(string directory, string fileName)
        {
            AppDomainSetup domaininfo = new AppDomainSetup();
            domaininfo.ApplicationBase = directory;

            var domain = AppDomain.CreateDomain(fileName);//, AppDomain.CurrentDomain.Evidence, domaininfo);
            var assemblyName = new AssemblyName { CodeBase = fileName };

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler((x, y) => ResolveHandler(fileName));

            domain.Load(assemblyName);
            return domain;
        }


        static void Main(string[] args)
        {
            var dll = LoadDll(".", "DistributedServerDll.dll");
            var server = GetNewTypeFromDll<IDistributedServer>(dll);

            server.StartListening(
                   @"c:\stuff\Server\Serverdlls\new", @"c:\stuff\Server\Serverdlls\working",
                   @"c:\stuff\Server\Clientdlls\new", @"c:\stuff\Server\Clientdlls\working",
                   12345);
        }
    }
}
