using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DeploymentManager
{
    public static class Utils
    {
        public static int MaxRetries = 3;

        public static string GetFileContent(string namespaze, string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream($"{namespaze}.{fileName}"))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static byte[] GetByteContent(string namespaze, string fileName)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream($"{namespaze}.{fileName}"))
            {
                if (stream == null)
                {
                    return null;
                }
                
                byte[] ba = new byte[stream.Length];
                stream.Read(ba, 0, ba.Length);
                return ba;
            }
        }
    }
}
