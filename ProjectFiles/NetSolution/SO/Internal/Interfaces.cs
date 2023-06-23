using System;
using System.Collections.Generic;
using UAManagedCore;

namespace NetZero.Internal
{
    internal struct ControllerCOA
    {
        public COAModel[] items { get; set; }
    }

    internal struct COAModel
    {
        public string name { get; set; }
        public string mimeType { get; set; }

        public string backingTag { get; set; }

        public bool isUDT { get; set; }

        public COAinfoAttributes[] infoAttributes { get; set; }
        public COAModel[] items { get; set; }
    }

    internal struct COAinfoAttributes
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    internal struct AOIInformantion
    {
        public string tag;
        public int type;
    }

    internal class UDTInfo
    {
        public string name;
        public int count;
        public int index;
        public List<UDTTag> tags;
    }

    internal class UDTTag
    {
        public string name;
        public int type;
        public int chars;
        public int array;
    }

    internal class RegisterElement
    {
        private readonly string _fqn = "";
        public string name = "";
        public int ID;
        public int parentID;
        public int type;
        public string fullPath;
        public List<COAinfoAttributes> infoAttributes;
    }

    internal struct SOInformation
    {
        public int type;
        public string fqn;
        public string fullpath;
        public IUAVariable Variable;
    }

    internal struct MyVQT
    {
        public object v;
        public int q;
        public long t;
    }

    internal struct WriteDINT
    {
        public int v;
        public string mimeType;
    }

    internal struct WriteReal
    {
        public float v;
        public string mimeType;
    }

    internal struct WriteBool
    {
        public bool v;
        public string mimeType;
    }

    [Serializable]
    internal struct CIPOptionFormatL1
    {
        public CIPOptionFormatL2 cipRequestData { get; set; }
        public bool cipUnconnectedMessaging { get; set; }
    }

    [Serializable]
    internal struct CIPOptionFormatL2
    {
        public CIPOptionFormatL3 data { get; set; }
    }

    [Serializable]
    internal struct CIPOptionFormatL3
    {
        public List<byte> v { get; set; }
        public string mimeType { get; set; }
    }

    internal struct SOConcertoPayload
    {
        public ModelList[] modelList { get; set; }
        public Datatypes[] datatypes { get; set; }
        public int version { get; set; }
    }

    internal struct ModelList
    {
        public string dsId { get; set; }
        public string fqn { get; set; }
        public int ns { get; set; }
        public string guids { get; set; }
        public uint typeid { get; set; }
    }

    internal struct Datatypes
    {
        public string name { get; set; }
        public string mimeType { get; set; }
        public Datatypes[] items { get; set; }
    }

    internal struct ControllerDataType
    {
        public Datatypes[] collections { get; set; }
    }
}