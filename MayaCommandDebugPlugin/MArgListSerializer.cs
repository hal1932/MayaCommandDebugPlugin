using Autodesk.Maya.OpenMaya;

namespace MayaCommandDebugPlugin
{
    public class MArgListSerializer
    {
        public static byte[] Serialize(MArgList argl)
        {
            // がんばってシリアライズする
            return new byte[] { 0 };
        }

        public static MArgList Deserialize(byte[] data)
        {
            // がんばってデシリアライズする
            return new MArgList();
        }
    }
}
