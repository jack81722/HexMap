using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;


namespace Network
{
    public class Serialization
    {
        public static byte[] ToByteArray(object source)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, source);
                return stream.ToArray();
            }
        }

        public static object ToObject(byte[] source)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(source))
            {
                formatter.Binder = new CurrentAssemblyDeserializationBinder();
                formatter.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
                return formatter.Deserialize(stream);
            }
        }

        public static T ToObject<T>(byte[] source)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(source))
            {
                formatter.Binder = new CurrentAssemblyDeserializationBinder();
                formatter.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
                return (T)formatter.Deserialize(stream);
            }
        }
    }

    public sealed class CurrentAssemblyDeserializationBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            return Type.GetType(String.Format("{0}, {1}", typeName, Assembly.GetExecutingAssembly().FullName));
        }
    }
}