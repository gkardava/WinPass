//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Windows.Data.Xml.Dom;
//using KeePassLib.Utility;

//namespace KeePass.IO.Utils
//{
//    public class KeyFile
//    {

//        private const string RootElementName = "KeyFile";
//        private const string MetaElementName = "Meta";
//        private const string VersionElementName = "Version";
//        private const string KeyElementName = "Key";
//        private const string KeyDataElementName = "Data";

//        public static byte[] LoadXmlKeyFile(byte[] pbFileData)
//        {
//            if (pbFileData == null) { Debug.Assert(false); return null; }

//            byte[] pbKeyData = null;

//            try
//            {
//                XmlDocument doc = new XmlDocument();
//                doc.LoadXml(Encoding.UTF8.GetString(pbFileData, 0, pbFileData.Length));

//                XmlElement el = doc.DocumentElement;
//                if ((el == null) || !el.NodeName.Equals(RootElementName)) return null;
//                if (el.ChildNodes.Count < 2) return null;

//                foreach (var xmlChild in el.ChildNodes)
//                {
//                    if (xmlChild.NodeName.Equals(MetaElementName)) { } // Ignore Meta
//                    else if (xmlChild.NodeName == KeyElementName)
//                    {
//                        foreach (var xmlKeyChild in xmlChild.ChildNodes)
//                        {
//                            if (xmlKeyChild.NodeName == KeyDataElementName)
//                            {
//                                if (pbKeyData == null)
//                                    pbKeyData = Convert.FromBase64String(xmlKeyChild.InnerText);
//                            }
//                        }
//                    }
//                }
//            }
//            catch (Exception) { pbKeyData = null; }
//            finally { }
//            return pbKeyData;
//        }
//    }
//}
