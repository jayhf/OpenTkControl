using System;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Graphics.OpenGL4;

namespace OpenTkWPFHost
{
    public class OpenGlErrorArgs : EventArgs
    {
        public DebugSource Source { get; }
        public DebugType Type { get; }
        public int Id { get; }
        public DebugSeverity Severity { get; }
        public int Length { get; }
        public IntPtr Message { get; }
        public IntPtr UserParam { get; }

        public OpenGlErrorArgs(DebugSource source, DebugType type, int id, DebugSeverity severity, int length,
            IntPtr message, IntPtr userparam)
        {
            this.Source = source;
            this.Type = type;
            this.Id = id;
            this.Severity = severity;
            this.Length = length;
            this.Message = message;
            this.UserParam = userparam;
        }

        public override string ToString()
        {
            string msg = PtrToStringUtf8(Message).Replace('\n', ' ');
            var stringBuilder = new StringBuilder();
            switch (Type)
            {
                case DebugType.DebugTypeError:
                    stringBuilder.Append($"{Severity}: {msg}\nCallStack={Environment.StackTrace}");
                    break;
                case DebugType.DebugTypePerformance:
                    stringBuilder.Append($"{Severity}: {msg}");
                    break;
                case DebugType.DebugTypePushGroup:
                    stringBuilder.Append($"{{ ({Id}) {Severity}: {msg}");
                    break;
                case DebugType.DebugTypePopGroup:
                    stringBuilder.Append($"}} ({Id}) {Severity}: {msg}");
                    break;
                default:
                    if (Source == DebugSource.DebugSourceApplication)
                    {
                        stringBuilder.Append($"{Type} {Severity}: {msg}");
                    }
                    else
                    {
                        stringBuilder.Append($"{Type} {Severity}: {msg}");
                    }

                    break;
            }

            return stringBuilder.ToString();
        }

        static string PtrToStringUtf8(IntPtr ptr)
        {
            var bytesCount = 0;
            byte b;
            do
            {
                b = Marshal.ReadByte(ptr, bytesCount);
                bytesCount++;
            } while (b != 0);

            var bytes = new byte[bytesCount - 1];
            Marshal.Copy(ptr, bytes, 0, bytesCount - 1);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}