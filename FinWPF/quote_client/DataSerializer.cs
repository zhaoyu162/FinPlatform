using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace FinWPF.quote_client
{
    public class DataSerializer
    {
        public static T DeserializeToObject<T>(string srcPath) where T : class
        {
            try
            {
                if (File.Exists($"{DateTime.Today:yyyyMMdd}\\{srcPath}.bin"))
                {
                    var formatter = new BinaryFormatter();
                    using (var stream = File.OpenRead($"{DateTime.Today:yyyyMMdd}\\{srcPath}.bin"))
                        return (T)formatter.Deserialize(stream);

                    //var ser = new XmlSerializer(typeof(T));
                    //using (var sr = new StreamReader($"{DateTime.Today:yyyyMMdd}\\{srcPath}.xml"))
                    //{
                    //    return (T)ser.Deserialize(sr);
                    //}
                }
            }
            catch
            {

            }
            return default(T);
        }

        public static string SerializeToFile<T>(string destPath, T obj) where T :class
        {
            try
            {
                Directory.CreateDirectory($"{DateTime.Today:yyyyMMdd}");
                var realPath = $"{DateTime.Today:yyyyMMdd}\\{destPath}.bin";
                var serializer = new BinaryFormatter();
                using (var stream = File.Open(realPath, FileMode.OpenOrCreate))
                {
                    serializer.Serialize(stream, obj);
                    //serializer.Serialize(writer, obj);
                }
                return realPath;
            }
            catch
            {

            }
            return null;
        }
    }
}
