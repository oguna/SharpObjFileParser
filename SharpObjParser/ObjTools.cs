using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SharpObjParser
{
    public class ObjTools
    {
        public static bool IsEndOfBuffer(StreamReader stream)
        {
            return stream.EndOfStream;
        }

        public static bool IsSeparator(char token)
        {
            return (token == ' ' || token == '\n' || token == '\f' || token == '\r' || token == '\t');
        }

        public static bool IsNewLine(char token)
        {
            return token == '\n' || token == '\f' || token == '\r';
        }

        public static void GetNextWord(StreamReader stream)
        {
            char c;
            while(!IsEndOfBuffer(stream))
            {
                if (!IsSeparator((char)stream.Peek()) || IsNewLine((char)stream.Peek()))
                {
                    break;
                }
                stream.Read();
            }
        }

        public static void SkipLine(StreamReader sr, ref uint line)
        {
            while(!IsEndOfBuffer(sr) && !IsNewLine((char)sr.Peek()))
            {
                sr.Read();
            }
            if (sr.BaseStream.Position != sr.BaseStream.Length)
            {
                sr.Read();
                line++;
            }
            while ((sr.BaseStream.Position != sr.BaseStream.Length) && (sr.Peek() == '\t' || sr.Peek() == ' '))
            {
                sr.Read();
            }
        }

        public static string CopyNextWord(StreamReader sr)
        {
            StringBuilder sb = new StringBuilder();
            GetNextWord(sr);
            while(!IsSeparator((char)sr.Peek()) && !IsEndOfBuffer(sr))
            {
                sb.Append((char)sr.Read());
                if (sr.EndOfStream)
                {
                    break;
                }
            }
            return sb.ToString();
        }

        public static void GetFloat(StreamReader sr, out float value)
        {
            string buffer = CopyNextWord(sr);
            value = float.Parse(buffer);
        }
    }
}
