using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CtripData
{
    class IP
    {
        public int s1, s2, s3, s4;
        public override string ToString()
        {
            return s1 + "." + s2 + "." + s3 + "." + s4;
        }
        public IP(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                throw new ArgumentNullException();
            string[] i_arr = ip.Split('.');
            s1 = Convert.ToInt32(i_arr[0]);
            s2 = Convert.ToInt32(i_arr[1]);
            s3 = Convert.ToInt32(i_arr[2]);
            s4 = Convert.ToInt32(i_arr[3]);
        }
        public static bool operator <(IP ip1, IP ip2)
        {
            if (ip1.s1 != ip2.s1) return ip1.s1 < ip2.s2;
            if (ip1.s2 != ip2.s2) return ip1.s2 < ip2.s2;
            if (ip1.s3 != ip2.s3) return ip1.s3 < ip2.s3;
            return ip1.s4 < ip2.s4;
        }
        public static bool operator >(IP ip1, IP ip2)
        {
            if (ip1.s1 != ip2.s1) return ip1.s1 > ip2.s2;
            if (ip1.s2 != ip2.s2) return ip1.s2 > ip2.s2;
            if (ip1.s3 != ip2.s3) return ip1.s3 > ip2.s3;
            return ip1.s4 > ip2.s4;
        }
    }
    class IPGen
    {
        static IP start, end;
        static int idx = 0;
        static List<string> ips = null;
        public static string CurIP()
        {
            return start.ToString();
        }
        public static string NextIP()
        {
            if (start.s4 < 255) start.s4++;
            else if (start.s3 < 255) { start.s3++; start.s4 = 0; }
            else if (start.s2 < 255) { start.s2++; start.s3 = 0; start.s4 = 0; }
            else if (start.s1 < 255) { start.s1++; start.s2 = start.s3 = start.s4 = 0; }
            else
            {
                throw new Exception("cannot increase");
            }
            if (start < end) { }
            else
            {
                idx++;
                SetStartEnd();
            }
            return start.ToString();
        }
        public static void FromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException();
            ips = File.ReadLines(path).ToList();
            SetStartEnd();
        }
        static void SetStartEnd()
        {
            string[] ipsection = ips[idx].Split(' ');
            start = new IP(ipsection[0]);
            end = new IP(ipsection[1]);
        }
    }
}
