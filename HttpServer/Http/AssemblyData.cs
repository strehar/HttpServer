using System.Reflection;

namespace Feri.MS.Http
{
    /// <summary>
    /// Internal class used to store data about registered assemblies.
    /// </summary>
    internal class AssemblyData
    {
        public string Name { get; set; }
        public string NameSpace { get; set; }
        public Assembly  Assembly { get; set; }
    }
}
