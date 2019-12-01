using Network.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Network.Utils
{
    public class DefaultSerializer : ISerializer
    {
        private static object _instLock = new object();
        private static DefaultSerializer _instance;

        public static ISerializer GetInstance()
        {
            if(_instance == null)
            {
                lock (_instLock)
                {
                    if (_instance == null)
                        _instance = new DefaultSerializer();
                }
            }
            return _instance;
        }

        public byte[] Serialize(object source)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, source);
                return stream.ToArray();
            }
        }

        public object Deserialize(byte[] source)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(source))
            {
                formatter.Binder = new CurrentAssemblyDeserializationBinder();
                formatter.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
                return formatter.Deserialize(stream);
            }
        }

        public T Deserialize<T>(byte[] source)
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
