//
//  This file contain references on how to write and read
//  fixed integers and float/double.
//  
using System;
using System.IO;

namespace SilentOrbit.ProtocolBuffers
{
    public static partial class ProtocolParser
    {
        #region Fixed Int, Only for reference
        /// <summary>
        /// Only for reference
        /// </summary>
        [Obsolete("Only for reference")]
        public static ulong ReadFixed64(BinaryReader reader)
        {
            return reader.ReadUInt64();
        }

        /// <summary>
        /// Only for reference
        /// </summary>
        [Obsolete("Only for reference")]
        public static long ReadSFixed64(BinaryReader reader)
        {
            return reader.ReadInt64();
        }

        /// <summary>
        /// Only for reference
        /// </summary>
        [Obsolete("Only for reference")]
        public static uint ReadFixed32(BinaryReader reader)
        {
            return reader.ReadUInt32();
        }

        /// <summary>
        /// Only for reference
        /// </summary>
        [Obsolete("Only for reference")]
        public static int ReadSFixed32(BinaryReader reader)
        {
            return reader.ReadInt32();
        }

        /// <summary>
        /// Only for reference
        /// </summary>
        [Obsolete("Only for reference")]
        public static void WriteFixed64(BinaryWriter writer, ulong val)
        {
            writer.Write(val);
        }

        /// <summary>
        /// Only for reference
        /// </summary>
        [Obsolete("Only for reference")]
        public static void WriteSFixed64(BinaryWriter writer, long val)
        {
            writer.Write(val);
        }

        /// <summary>
        /// Only for reference
        /// </summary>
        [Obsolete("Only for reference")]
        public static void WriteFixed32(BinaryWriter writer, uint val)
        {
            writer.Write(val);
        }

        /// <summary>
        /// Only for reference
        /// </summary>
        [Obsolete("Only for reference")]
        public static void WriteSFixed32(BinaryWriter writer, int val)
        {
            writer.Write(val);
        }

        #endregion

        #region Fixed: float, double. Only for reference

        public static float ReadSingle(Stream stream)
        {
            byte[] buffer = new byte[4];
            int read = 0;
            while (read < 4)
            {
                int r = stream.Read(buffer, read, 4 - read);
                if (r == 0)
                    throw new ProtocolBufferException("Expected " + (4 - read) + " got " + read);
                read += r;
            }
            read = 0;
            return ReadSingle(buffer, ref read);
        }

        public static double ReadDouble(Stream stream)
        {
            byte[] buffer = new byte[8];
            int read = 0;
            while (read < 8)
            {
                int r = stream.Read(buffer, read, 8 - read);
                if (r == 0)
                    throw new ProtocolBufferException("Expected " + (8 - read) + " got " + read);
                read += r;
            }
            read = 0;
            return ReadDouble(buffer, ref read);
        }

        [System.Security.SecuritySafeCritical]
        public static unsafe float ReadSingle(byte[] buffer, ref int offset)
        {
            uint tmpBuffer = (uint)(buffer[offset++] | buffer[offset++] << 8 | buffer[offset++] << 16 | buffer[offset++] << 24);
            return *((float*)&tmpBuffer);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static unsafe double ReadDouble(byte[] buffer, ref int offset)
        {
            uint lo = (uint)(buffer[offset++] | buffer[offset++] << 8 |
                buffer[offset++] << 16 | buffer[offset++] << 24);
            uint hi = (uint)(buffer[offset++] | buffer[offset++] << 8 |
                buffer[offset++] << 16 | buffer[offset++] << 24);

            ulong tmpBuffer = ((ulong)hi) << 32 | lo;
            return *((double*)&tmpBuffer);
        }

        /// <summary>
        /// Only for reference
        /// </summary>
        [Obsolete("Only for reference")]
        public static float ReadFloat(BinaryReader reader)
        {
            return reader.ReadSingle();
        }

        /// <summary>
        /// Only for reference
        /// </summary>
        [Obsolete("Only for reference")]
        public static double ReadDouble(BinaryReader reader)
        {
            return reader.ReadDouble();
        }

        /// <summary>
        /// Only for reference
        /// </summary>
        [Obsolete("Only for reference")]
        public static void WriteFloat(BinaryWriter writer, float val)
        {
            writer.Write(val);
        }

        /// <summary>
        /// Only for reference
        /// </summary>
        [Obsolete("Only for reference")]
        public static void WriteDouble(BinaryWriter writer, double val)
        {
            writer.Write(val);
        }

        #endregion
    }
}

