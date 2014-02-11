
using System.IO;
using System.Net;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
namespace CtripData
{
    class GzipConverter
    {
        //解密
        public static Stream Gzip(HttpWebResponse HWResp)
        {
            Stream stream1 = null;
            if (HWResp.ContentEncoding == "gzip")
            {
                stream1 = new GZipInputStream(HWResp.GetResponseStream());
            }
            else
            {
                if (HWResp.ContentEncoding == "deflate")
                {
                    stream1 = new InflaterInputStream(HWResp.GetResponseStream());
                }
            }
            if (stream1 == null)
            {
                return HWResp.GetResponseStream();
            }
            MemoryStream stream2 = new MemoryStream();
            int count = 0x800;
            byte[] buffer = new byte[0x800];
            goto A;
        A:
            count = stream1.Read(buffer, 0, count);
            if (count > 0)
            {
                stream2.Write(buffer, 0, count);
                goto A;
            }
            stream2.Seek((long)0, SeekOrigin.Begin);
            return stream2;
        }
    }
}
