using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Deployer.Logic
{
    class XmlUtil
    {
        public static bool ParseBoolAttribute(XmlAttribute xmlAttribute, bool defaultValue)
        {
            return xmlAttribute != null ?
                bool.Parse(xmlAttribute.InnerXml) :
                defaultValue;
        }

        internal static string ParseStringAttribute(XmlAttribute xmlAttribute, string defaultValue)
        {
            return xmlAttribute != null ?
                xmlAttribute.InnerXml :
                defaultValue;
        }

        internal static string ParseStringNode(XmlNode xmlNode, string defaultValue)
        {
            return xmlNode != null ?
                xmlNode.InnerXml :
                defaultValue;
        }

        internal static bool ParseBoolNode(XmlNode xmlNode, bool defaultValue)
        {
            return xmlNode != null ?
                bool.Parse(xmlNode.InnerXml) :
                defaultValue;
        }

        internal static int ParseIntNode(XmlNode xmlNode, int defaultValue)
        {
            return xmlNode != null ?
                int.Parse(xmlNode.InnerXml) :
                defaultValue;
        }
    }
}
