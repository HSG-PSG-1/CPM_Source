using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Text;
using ProtoBuf;
using System.IO;
using CPM.DAL;

namespace CPM.Helper
{
    public class Serialization
    {
        #region Kept for future
        //1.
        // HT: Unable to use ProtoBuf due to: //http://code.google.com/p/protobuf-net/issues/detail?id=168
        // Type is not expected, and no contract can be inferred
        //Awaiting fix or another release.
        //2.
        // BinaryFormatter : Demands to mark the class as [Serializable]
        // Ref: http://www.codeproject.com/KB/cs/objserial.aspx
        //3.
        // XMLSerializer: http://geekswithblogs.net/TimH/archive/2006/02/09/68857.aspx

        //Ref urls:
        // http://stackoverflow.com/questions/4143421/fastest-way-to-serialize-and-deserialize-net-object
        // http://stackoverflow.com/questions/5173033/string-serialization-and-deserialization-problem
        // http://code.google.com/p/protobuf-net/
        #endregion

        /* CAUTION: IMP: Serialization will abort if there're self-referring child-parent relationships
             * To AVOID this 'circular-reference' make sure that the entity reference as the following
             * In CPM.dbml set the Parent property (Access) to Internal for the parent-child Association
             * This will make sure that circular entity references are NOT generated */

        public static string Serialize<T>(T serializee)
        {
            using (var ms = new MemoryStream())
            {
                #region Old - kept for future R&D
                //Otherwise ERR: Type is not expected, and no contract can be inferred
                //vw_Users_Role_Org src = (vw_Users_Role_Org)(Serializer.DeepClone<vw_Users_Role_Org>(serializee));
                //Serialize using the fastest serializer - ProtoBuf
                //Serializer.Serialize(ms, src);
                //BinaryFormatter bf = new BinaryFormatter();                bf.Serialize(ms, serializee);
                #endregion

                XmlSerializer xs = new XmlSerializer(typeof(T));
                xs.Serialize(ms, serializee);

                byte[] bSer = ms.ToArray();
                return Encoding.UTF8.GetString(bSer);
            }
        }

        public static T Deserialize<T>(string byteArrStr)
        {
            byte[] bSer = Encoding.UTF8.GetBytes(byteArrStr);
            using (MemoryStream ms = new MemoryStream(bSer))
            {
                //return ProtoBuf.Serializer.Deserialize<T>(ms);
                //return (T)new BinaryFormatter().Deserialize(ms);

                //SO: 1556874 & http://msdn.microsoft.com/en-us/library/aa302290.aspx (avoid : <FileDetail xmlns=''> was not expected.)
                //Do it here instead of  repeating attribute in every class
                XmlRootAttribute xRoot = new XmlRootAttribute();
                xRoot.ElementName = typeof(T).Name;                // xRoot.Namespace = "http://www.hsg.com";
                xRoot.IsNullable = true;
                return (T)new XmlSerializer(typeof(T),xRoot).Deserialize(ms);
            }
        }
    }
}
