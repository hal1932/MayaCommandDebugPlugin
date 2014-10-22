using Autodesk.Maya.OpenMaya;
using System.Diagnostics;
using System.Linq;

namespace MayaCommandDebugPlugin
{
    class DebugHelper
    {
        [Conditional("DEBUG")]
        public static void DumpMethod()
        {
            var trace = new StackTrace();
            var method = trace.GetFrame(1).GetMethod();

            MGlobal.displayInfo(string.Format("{0}.{1}({2})",
                method.ReflectedType.Name,
                method.Name,
                string.Join(", ",
                    method.GetParameters()
                        .Select(arg => arg.ParameterType.Name))));
        }
    }
}
