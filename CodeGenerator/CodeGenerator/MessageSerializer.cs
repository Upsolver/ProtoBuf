using System;
using System.IO;
using SilentOrbit.Code;

namespace SilentOrbit.ProtocolBuffers
{
    internal class MessageSerializer
    {
        private readonly CodeWriter cw;
        private readonly Options options;
        private readonly FieldSerializer fieldSerializer;

        public MessageSerializer(CodeWriter cw, Options options)
        {
            this.cw = cw;
            this.options = options;
            this.fieldSerializer = new FieldSerializer(cw, options);
        }

        public void GenerateClassSerializer(ProtoMessage m)
        {
            if (m.OptionExternal || m.OptionType == "interface")
            {
                //Don't make partial class of external classes or interfaces
                //Make separate static class for them
                cw.Bracket(m.OptionAccess + " static class " + m.SerializerType);
            }
            else
            {
                if (options.SerializableAttributes)
                    cw.Attribute("System.Serializable");
                cw.Bracket(m.OptionAccess + " partial " + m.OptionType + " " + m.SerializerType);
            }

            GenerateReader(m);

            GenerateWriter(m);
            foreach (ProtoMessage sub in m.Messages.Values)
            {
                cw.WriteLine();
                GenerateClassSerializer(sub);
            }
            cw.EndBracket();
            cw.WriteLine();
            return;
        }

        public static string GetReadByte(bool stream)
        {
            return stream ? "stream.ReadByte()" : "offset == buffer.Length ? -1 : buffer[offset++]";
        }

        public static string GetPosition(bool stream)
        {
            return stream ? "stream.Position" : "offset";
        }

        public static string GetTarget(bool stream)
        {
            return stream ? "stream" : "buffer, ref offset";
        }

        public static string GetTargetDeclaration(bool stream)
        {
            return stream ? "Stream stream" : "byte[] buffer, ref int offset";
        }

        private void GenerateReader(ProtoMessage m)
        {
            string refstr = (m.OptionType == "struct") ? "ref " : "";

            if (m.OptionType != "interface")
            {
                cw.Summary(
                        "Helper: put the buffer into a MemoryStream and create a new instance to deserializing into");
                cw.Bracket(m.OptionAccess + " static " + m.CsType + " Deserialize(byte[] buffer)");
                cw.WriteLine("var instance = new " + m.CsType + "();");
                cw.WriteLine("int offset = 0;");
                cw.WriteLine("Deserialize(" + GetTarget(false) + ", " + refstr + "instance);");
                cw.WriteLine("return instance;");
                cw.EndBracketSpace();
            }

            cw.Summary("Helper: put the buffer into a MemoryStream before deserializing");
            cw.Bracket(m.OptionAccess + " static " + m.FullCsType + " Deserialize(byte[] buffer, " + refstr +
                       m.FullCsType + " instance)");
            cw.WriteLine("int offset = 0;");
            cw.WriteLine("Deserialize(" + GetTarget(false) + ", " + refstr + "instance);");
            cw.WriteLine("return instance;");
            cw.EndBracketSpace();

            foreach (bool isStream in new[] {true, false})
            {
                if (m.OptionType != "interface")
                {
                    cw.Summary("Helper: create a new instance to deserializing into");
                    cw.Bracket(m.OptionAccess + " static " + m.CsType + " Deserialize(" + GetTargetDeclaration(isStream) + ")");
                    cw.WriteLine("var instance = new " + m.CsType + "();");
                    cw.WriteLine("Deserialize(" + GetTarget(isStream) + ", " + refstr + "instance);");
                    cw.WriteLine("return instance;");
                    cw.EndBracketSpace();

                    cw.Summary("Helper: create a new instance to deserializing into");
                    cw.Bracket(m.OptionAccess + " static " + m.CsType + " DeserializeLengthDelimited(" + GetTargetDeclaration(isStream) + ")");
                    cw.WriteLine("var instance = new " + m.CsType + "();");
                    cw.WriteLine("DeserializeLengthDelimited(" + GetTarget(isStream) + ", " + refstr + "instance);");
                    cw.WriteLine("return instance;");
                    cw.EndBracketSpace();

                    cw.Summary("Helper: create a new instance to deserializing into");
                    cw.Bracket(m.OptionAccess + " static " + m.CsType + " DeserializeLength(" + GetTargetDeclaration(isStream) + ", int length)");
                    cw.WriteLine("var instance = new " + m.CsType + "();");
                    cw.WriteLine("DeserializeLength(" + GetTarget(isStream) + ", length, " + refstr + "instance);");
                    cw.WriteLine("return instance;");
                    cw.EndBracketSpace();
                }

                string[] methods = new string[]
                {
                    "Deserialize", //Default old one
                    "DeserializeLengthDelimited", //Start by reading length prefix and stay within that limit
                    "DeserializeLength", //Read at most length bytes given by argument
                };


                //Main Deserialize
                foreach (string method in methods)
                {
                    if (method == "Deserialize")
                    {
                        cw.Summary("Takes the remaining content of the stream and deserialze it into the instance.");
                        cw.Bracket(m.OptionAccess + " static " + m.FullCsType + " " + method + "(" +
                                   GetTargetDeclaration(isStream) + ", " +
                                   refstr + m.FullCsType + " instance)");
                    }
                    else if (method == "DeserializeLengthDelimited")
                    {
                        cw.Summary(
                            "Read the VarInt length prefix and the given number of bytes from the stream and deserialze it into the instance.");
                        cw.Bracket(m.OptionAccess + " static " + m.FullCsType + " " + method + "(" +
                                   GetTargetDeclaration(isStream) + ", " +
                                   refstr + m.FullCsType + " instance)");
                    }
                    else if (method == "DeserializeLength")
                    {
                        cw.Summary("Read the given number of bytes from the stream and deserialze it into the instance.");
                        cw.Bracket(m.OptionAccess + " static " + m.FullCsType + " " + method +
                                   "(" + GetTargetDeclaration(isStream) + ", int length, " + refstr + m.FullCsType +
                                   " instance)");
                    }
                    else
                        throw new NotImplementedException();


                    //Prepare List<> and default values
                    foreach (Field f in m.Fields.Values)
                    {
                        if (f.OptionDeprecated)
                            cw.WritePragma("warning disable 612");

                        if (f.Rule == FieldRule.Repeated)
                        {
                            if (f.OptionReadOnly == false)
                            {
                                //Initialize lists of the custom DateTime or TimeSpan type.
                                string csType = f.ProtoType.FullCsType;
                                if (f.OptionCodeType != null)
                                    csType = f.OptionCodeType;

                                cw.WriteLine("if (instance." + f.CsName + " == null)");
                                cw.WriteIndent("instance." + f.CsName + " = new List<" + csType + ">();");
                            }
                        }
                        else if (f.OptionDefault != null)
                        {
                            cw.WriteLine("instance." + f.CsName + " = " + f.FormatForTypeAssignment() + ";");
                        }
                        else if ((f.Rule == FieldRule.Optional) && !options.Nullable)
                        {
                            if (f.ProtoType is ProtoEnum)
                            {
                                ProtoEnum pe = f.ProtoType as ProtoEnum;
                                //the default value is the first value listed in the enum's type definition
                                foreach (var kvp in pe.Enums)
                                {
                                    cw.WriteLine("instance." + f.CsName + " = " + pe.FullCsType + "." + kvp.Name + ";");
                                    break;
                                }
                            }
                        }

                        if (f.OptionDeprecated)
                            cw.WritePragma("warning restore 612");
                    }

                    if (method == "DeserializeLengthDelimited")
                    {
                        //Important to read stream position after we have read the length field
                        cw.WriteLine("long limit = " + ProtocolParser.Base + ".ReadUInt32(" + GetTarget(isStream) + ");");
                        cw.WriteLine("limit += " + GetPosition(isStream) + ";");
                    }
                    if (method == "DeserializeLength")
                    {
                        //Important to read stream position after we have read the length field
                        cw.WriteLine("long limit = " + GetPosition(isStream) + " + length;");
                    }

                    cw.WhileBracket("true");

                    if (method == "DeserializeLengthDelimited" || method == "DeserializeLength")
                    {
                        cw.IfBracket("" + GetPosition(isStream) + " >= limit");
                        cw.WriteLine("if (" + GetPosition(isStream) + " == limit)");
                        cw.WriteIndent("break;");
                        cw.WriteLine("else");
                        cw.WriteIndent(
                            "throw new global::SilentOrbit.ProtocolBuffers.ProtocolBufferException(\"Read past max limit\");");
                        cw.EndBracket();
                    }

                    cw.WriteLine("int keyByte = " + GetReadByte(isStream) + ";");

                    //Determine if we need the lowID optimization
                    bool hasLowID = false;
                    foreach (Field f in m.Fields.Values)
                    {
                        if (f.ID < 16)
                        {
                            hasLowID = true;
                            break;
                        }
                    }

                    if (hasLowID)
                    {
                        cw.Comment("Optimized reading of known fields with field ID < 16");
                        cw.Switch("keyByte");
                        foreach (Field f in m.Fields.Values)
                        {
                            if (f.ID >= 16)
                                continue;

                            if (f.OptionDeprecated)
                                cw.WritePragma("warning disable 612");

                            cw.Dedent();
                            cw.Comment("Field " + f.ID + " " + f.WireType);
                            cw.Indent();
                            cw.Case(((f.ID << 3) | (int) f.WireType));
                            if (fieldSerializer.FieldReader(f, isStream))
                                cw.WriteLine("continue;");

                            if (f.OptionDeprecated)
                                cw.WritePragma("warning restore 612");
                        }
                        cw.SwitchEnd();
                        cw.WriteLine();
                    }
                    cw.WriteLine("if (keyByte == -1)");
                    if (method == "Deserialize")
                        cw.WriteIndent("break;");
                    else
                        cw.WriteIndent("throw new System.IO.EndOfStreamException();");

                    cw.WriteLine("var key = " + ProtocolParser.Base + ".ReadKey((byte)keyByte, " + GetTarget(isStream) +
                                 ");");

                    cw.WriteLine();

                    cw.Comment("Reading field ID > 16 and unknown field ID/wire type combinations");
                    cw.Switch("key.Field");
                    cw.Case(0);
                    cw.WriteLine(
                        "throw new global::SilentOrbit.ProtocolBuffers.ProtocolBufferException(\"Invalid field id: 0, something went wrong in the stream\");");
                    foreach (Field f in m.Fields.Values)
                    {
                        if (f.ID < 16)
                            continue;
                        cw.Case(f.ID);
                        //Makes sure we got the right wire type
                        cw.WriteLine("if(key.WireType != global::SilentOrbit.ProtocolBuffers.Wire." + f.WireType + ")");
                        cw.WriteIndent("break;"); //This can be changed to throw an exception for unknown formats.

                        if (f.OptionDeprecated)
                            cw.WritePragma("warning disable 612");

                        if (fieldSerializer.FieldReader(f, isStream))
                            cw.WriteLine("continue;");

                        if (f.OptionDeprecated)
                            cw.WritePragma("warning restore 612");
                    }
                    cw.CaseDefault();
                    if (m.OptionPreserveUnknown)
                    {
                        cw.WriteLine("if (instance.PreservedFields == null)");
                        cw.WriteIndent(
                            "instance.PreservedFields = new List<global::SilentOrbit.ProtocolBuffers.KeyValue>();");
                        cw.WriteLine(
                            "instance.PreservedFields.Add(new global::SilentOrbit.ProtocolBuffers.KeyValue(key, " +
                            ProtocolParser.Base + ".ReadValueBytes(" + GetTarget(isStream) + ", key)));");
                    }
                    else
                    {
                        cw.WriteLine(ProtocolParser.Base + ".SkipKey(" + GetTarget(isStream) + ", key);");
                    }
                    cw.WriteLine("break;");
                    cw.SwitchEnd();
                    cw.EndBracket();
                    cw.WriteLine();

                    if (m.OptionTriggers)
                        cw.WriteLine("instance.AfterDeserialize();");
                    cw.WriteLine("return instance;");
                    cw.EndBracket();
                    cw.WriteLine();
                }
            }
        }

        /// <summary>
        /// Generates code for writing a class/message
        /// </summary>
        private void GenerateWriter(ProtoMessage m)
        {
            string stack = "global::SilentOrbit.ProtocolBuffers.ProtocolParser.Stack";
            if (options.ExperimentalStack != null)
            {
                cw.WriteLine("[ThreadStatic]");
                cw.WriteLine("static global::SilentOrbit.ProtocolBuffers.MemoryStreamStack stack = new " +
                             options.ExperimentalStack + "();");
                stack = "stack";
            }

            cw.Summary("Serialize the instance into the stream");
            cw.Bracket(m.OptionAccess + " static void Serialize(Stream stream, " + m.CsType + " instance)");
            if (m.OptionTriggers)
            {
                cw.WriteLine("instance.BeforeSerialize();");
                cw.WriteLine();
            }
            if (m.IsUsingBinaryWriter)
                cw.Using("var bw = new BinaryWriter(stream, Encoding.UTF8, true)");
            //Shared memorystream for all fields
            cw.WriteLine("var msField = " + stack + ".Pop();");

            foreach (Field f in m.Fields.Values)
            {
                if (f.OptionDeprecated)
                    cw.WritePragma("warning disable 612");

                fieldSerializer.FieldWriter(m, f, cw, options);

                if (f.OptionDeprecated)
                    cw.WritePragma("warning restore 612");
            }

            cw.WriteLine(stack + ".Push(msField);");

            if (m.OptionPreserveUnknown)
            {
                cw.IfBracket("instance.PreservedFields != null");
                cw.ForeachBracket("var kv in instance.PreservedFields");
                cw.WriteLine("global::SilentOrbit.ProtocolBuffers.ProtocolParser.WriteKey(stream, kv.Key);");
                cw.WriteLine("stream.Write(kv.Value, 0, kv.Value.Length);");
                cw.EndBracket();
                cw.EndBracket();
            }
            if (m.IsUsingBinaryWriter)
                cw.EndBracket();

            cw.EndBracket();
            cw.WriteLine();

            cw.Summary("Helper: Serialize into a MemoryStream and return its byte array");
            cw.Bracket(m.OptionAccess + " static byte[] SerializeToBytes(" + m.CsType + " instance)");
            cw.Using("var ms = new MemoryStream()");
            cw.WriteLine("Serialize(ms, instance);");
            cw.WriteLine("return ms.ToArray();");
            cw.EndBracket();
            cw.EndBracket();

            cw.Summary("Helper: Serialize with a varint length prefix");
            cw.Bracket(m.OptionAccess + " static void SerializeLengthDelimited(Stream stream, " + m.CsType +
                       " instance)");
            cw.WriteLine("var data = SerializeToBytes(instance);");
            cw.WriteLine("global::SilentOrbit.ProtocolBuffers.ProtocolParser.WriteUInt32(stream, (uint)data.Length);");
            cw.WriteLine("stream.Write(data, 0, data.Length);");
            cw.EndBracket();
        }
    }
}