using System;
using System.Collections.Generic;
using System.Reflection;
using Cet.IO.Protocols;
using Cet.IO;
using System.ComponentModel;

namespace ModbusHelpers
{
    public interface ITagsAdapter
    {
        bool IsTagUpdated(int Address);
        bool IsTagUpdated(int Address, int index);
        bool IsTagsUpdated();
    }

    public class ModbusAdapter<T> : ITagsAdapter where T : new()
    {
        private delegate void ParserAction(string TagName, int offset, int index, ModbusCommand command, Object Current, out Object Result);
        public Dictionary<string, int> Addresses { get; private set; } = new Dictionary<string, int>();
        
        Dictionary<string, Object> Precisions = new Dictionary<string, Object>();
        Dictionary<string, Object> StoredTagsValues = new Dictionary<string, Object>();
        public Dictionary<string, bool> TagsChanged { get; private set; }
        public Dictionary<string, bool> TagsSending { get; private set; }

        Dictionary<string, int> TagsSize = new Dictionary<string, int>();

        private static ParserAction ActionRead;
        private static ParserAction ActionWrite;

        public List<string> TagUsedLastRW = new List<string>();
        public ModbusAdapter()
        {
            TagsChanged = new Dictionary<string, bool>();
            TagsSending = new Dictionary<string, bool>();
            T tags = new T();
            ActionRead = this.ParserRead;
            ActionWrite = this.ParserWrite;
            Type typ = typeof(T).GetNestedType("Registers") ?? typeof(T).BaseType.GetNestedType("Registers");
            foreach (FieldInfo Register in typ.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                try
                {
                    int Address = int.Parse(Register.GetValue(null).ToString());
                    Addresses.Add(Register.Name, Address);
                    FieldInfo tag = tags.GetType().GetField(Register.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (tag != null)
                    {
                        int len = tag.GetValue(tags) is Array ? (tag.GetValue(tags) as Array).Length : 1;
                        TagsSize.Add(tag.Name, len);
                        for (int i = 0; i < len; i++)
                        {
                            TagsSending.Add(Address + ":" + i, false);
                            TagsChanged.Add(Address + ":" + i, false);
                        }
                    }
                }
                catch (Exception e)
                {
                    System.Windows.MessageBox.Show(e.Message + ", " + Register.Name + ":" + int.Parse(Register.GetValue(null).ToString()));
                    return;
                }

            }


            if (typeof(T).GetNestedType("Precisions") != null)
            {
                foreach (FieldInfo Precision in typeof(T).GetNestedType("Precisions").GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    Object Value = Precision.GetValue(null);
                    if (Value != null)
                    {
                        if (Value is Array)
                        {
                            Array arr = Value as Array;
                            for (int i = 0; i < arr.Length; i++)
                            {
                                Precisions.Add(Precision.Name + ":" + i, arr.GetValue(i));
                            }
                        }
                        else
                            Precisions.Add(Precision.Name + ":0", Value);
                    }

                }
            }
        }


        public void Read(ModbusCommand command, ref T tags)
        {
            Parse(command, ParserRead, ref tags);
        }

        public void Write(ModbusCommand command, ref T tags)
        {
            Parse(command, ParserWrite, ref tags);
        }

        private void Parse(ModbusCommand command, ParserAction ParserAction, ref T tags)
        {
            TagUsedLastRW.Clear();
            foreach (KeyValuePair<string, int> p in Addresses)
            {
                FieldInfo tag = tags.GetType().GetField(p.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                int Address = p.Value;
                try
                {
                    if (tag != null)
                    {
                     
                        int len = TagsSize[tag.Name];

                        int offset = (((Address) >= command.Offset) && ((Address) < (command.Offset + command.Count))) ? offset = (Address) - command.Offset : -1;
                        for (int i = 0; i < len; i++)
                        {
                            int step;
                            string tp = tag.FieldType.Name.Replace("[]", "");
                            if ( (tp == "DateTime") || (tp == "UInt32") || (tp == "Single"))
                                step = 2;
                            else
                                step = 1;

                            bool offset_i = (((Address + i*step) >= command.Offset) && ((Address + i*step + step-1) < (command.Offset + command.Count))) ;
                            if (offset_i)
                            {
                                Object value = tag.GetValue(tags);
                                if (len != 1)
                                {
                                    Array arr = value as Array;
                                    value = arr.GetValue(i);
                                }
                                ParserAction(tag.Name, offset, i, command, value, out Object Result);
                                if (Result != null)
                                {
                                    if (len != 1)
                                    {
                                        Array arr = tag.GetValue(tags) as Array;
                                        arr.SetValue(Result, i);
                                    }
                                    else
                                        tag.SetValue(tags, Result);
                                }
                            }
                        }
                        if (offset != -1)
                        {
                            try
                            {
                                string nn = tag.Name.IndexOf('_') == 0 ? tag.Name.Substring(1) : tag.Name;
                                TagUsedLastRW.Add(nn);
                            }
                            catch (Exception e)
                            { }
                        }

                    }
                }
                finally { }
            }
        }

        void ParserRead(string TagName, int offset, int index, ModbusCommand command, Object Current, out Object Result)
        {
            string k = Addresses[TagName] + ":" + index;
            string k_str = TagName + ":" + index;
            TagsChanged[k] = true;

            Result = null;
            try
            {
                int step = 2 * index;
                if (Current.GetType().Name == typeof(DateTime).Name)
                {
                    double ts = ((ulong)(command.Data[offset + 1 + step]) << 16) + (ulong)command.Data[offset + step];
                    DateTime NewVal = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local).AddSeconds(ts);
                    if (StoredTagsValues.ContainsKey(k))
                    {
                        TagsChanged[k] = IsEq<DateTime>(NewVal, (DateTime)StoredTagsValues[k]) ? false : true;
                        StoredTagsValues[k] = NewVal;
                    }
                    else
                        StoredTagsValues.Add(k, NewVal);
                    Result = NewVal;
                }

                if (Current.GetType().Name == typeof(Int16).Name)
                {
                    Int16 NewVal = command.Data[offset + index] > 0xffff ? (Int16)((long)command.Data[offset + index] - 0x10000) : (Int16)command.Data[offset + index];
                    if (StoredTagsValues.ContainsKey(k))
                    {
                        TagsChanged[k] = IsEq<Int16>(NewVal, (Int16)StoredTagsValues[k]) ? false : true;
                        StoredTagsValues[k] = NewVal;
                    }
                    else
                        StoredTagsValues.Add(k, NewVal);
                    Result = NewVal;
                }

                if (Current.GetType().Name == typeof(UInt16).Name)
                {
                    UInt16 NewVal = command.Data[offset + index];
                    if (StoredTagsValues.ContainsKey(k))
                    {
                        TagsChanged[k] = IsEq<UInt16>(NewVal, (UInt16)StoredTagsValues[k]) ? false : true;
                        StoredTagsValues[k] = NewVal;
                    }
                    else
                        StoredTagsValues.Add(k, NewVal);
                    Result = NewVal;
                }


                if (Current.GetType().Name == typeof(UInt32).Name)
                {
                    UInt32 NewVal = (UInt32) (command.Data[offset + step] << 16) + command.Data[offset + step + 1];
                    if (StoredTagsValues.ContainsKey(k))
                    {
                        TagsChanged[k] = IsEq<UInt32>(NewVal, (UInt32)StoredTagsValues[k]) ? false : true;
                        StoredTagsValues[k] = NewVal;
                    }
                    else
                        StoredTagsValues.Add(k, NewVal);
                    Result = NewVal;
                }


                if (Current.GetType().Name == typeof(Single).Name)
                {
                    Single NewVal = ByteArrayHelpers.ReadIEEE(command.Data, offset + step);
                    if (StoredTagsValues.ContainsKey(k))
                    {
                        if (Precisions.ContainsKey(k_str))
                        {
                            TagsChanged[k] = Math.Abs(NewVal - (Single)StoredTagsValues[k]) > (Single)Precisions[k_str] ? true : false;
                        }
                        else
                            TagsChanged[k] = IsEq<Single>(NewVal, (Single)StoredTagsValues[k]) ? false : true;

                        StoredTagsValues[k] = NewVal;
                    }
                    else
                        StoredTagsValues.Add(k, NewVal);
                    Result = NewVal;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(TagName +"["+index+"] "+ e.Message);
                Result = null;
            }
            finally
            {
            }
        }

        public bool IsEq<V>(V t1, V t2) where V : IComparable
        {
            return t1.CompareTo(t2) == 0;
        }

        public bool IsTagUpdated(int Address)
        {
            return IsTagUpdated(Address, 0);
        }

        public bool IsTagUpdated(int Address, int index)
        {
            return TagsChanged[Address + ":" + index];
        }

        public bool IsTagsUpdated()
        {
            foreach (KeyValuePair<string, bool> p in TagsChanged)
                if (p.Value) return true;
            return false;
        }


        void ParserWrite(string TagName, int offset, int index, ModbusCommand command, Object Current, out Object Result)
        {

            Result = null;
            try
            {
                int step = 2 * index;
                if (Current.GetType().Name == typeof(DateTime).Name)
                {
                    uint epoch = (uint)((DateTime)Current - new DateTime(1970, 1, 1)).TotalSeconds;
                    command.Data[offset + step] = (ushort)(epoch & ushort.MaxValue);
                    command.Data[offset + 1 + step] = (ushort)(epoch >> 16);
                }

                if (Current.GetType().Name == typeof(Int16).Name)
                {
                    Int16 tt = (Int16)Current;
                    command.Data[offset + index] = tt < 0 ? (ushort)(0xffff + tt + 1) : (ushort)tt;
                }

                if (Current.GetType().Name == typeof(UInt16).Name)
                {
                    command.Data[offset + index] = (ushort)Current;
                }

                if (Current.GetType().Name == typeof(UInt32).Name)
                {
                    uint i = BitConverter.ToUInt32(BitConverter.GetBytes((uint)Current), 0);
                    command.Data[offset + step + 1] = (ushort)i;
                    command.Data[offset + step] = (ushort)(i >> 16);
                }

                if (Current.GetType().Name == typeof(Single).Name)
                {
                    uint i = BitConverter.ToUInt32(BitConverter.GetBytes((float)Current), 0);
                    command.Data[offset + step] = (ushort)i;
                    command.Data[offset + 1 + step] = (ushort)(i >> 16);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Result = null;
            }

        }

    }


    [Serializable]
    public class IOTags
    {
        public void SetFrom(IOTags tags)
        {
            try
            {
                foreach (FieldInfo info in tags.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    FieldInfo myInfo = this.GetType().GetField(info.Name);
                    if (myInfo != null)
                    {
                        if (info.GetValue(tags).GetType().IsArray)
                        {
                            Array v0 = info.GetValue(tags) as Array;
                            Array v1 = myInfo.GetValue(this) as Array;
                            for (int i = 0; i < v0.Length; i++)
                            {
                                v1.SetValue(v0.GetValue(i), i);
                            }
                        }
                        else
                        {
                            myInfo.SetValue(this, info.GetValue(tags));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(" IOTags.SetFrom:" + e.Message);
            }
        }


    }
}
