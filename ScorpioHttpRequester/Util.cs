using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Net;
public class Util {
    public delegate void Action();
    private static Control control;
    public static string Version { get; private set; }
    public static void Init(Control control) {
        Util.control = control;
    }
    public static void Exec(Action action) {
        control.Invoke(action);
    }
    public static string toString(Stream stream, Encoding encode) {
        if (stream == null) return "";
        return encode.GetString(toByteArray(stream));
    }
    /// <summary> 字节流转成byte[] </summary>
    public static byte[] toByteArray(Stream stream) {
        if (stream == null) return null;
        MemoryStream result = new MemoryStream();
        int length = 0;
        byte[] bytes = new byte[8192];
        while ((length = stream.Read(bytes, 0, 8192)) > 0) {
            result.Write(bytes, 0, length);
        }
        return result.ToArray();
    }
}