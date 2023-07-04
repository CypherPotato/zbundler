using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zbundler;
static class Size
{
    public const long UnitKb = 0x400;
    public const long UnitMb = 0x100000;
    public const long UnitGb = 0x40000000;
    public const long UnitTb = 0x10000000000;
    public const long UnitPb = 0x4000000000000;
    public const long UnitEb = 0x1000000000000000;

    public static string ReadableSize(long i)
    {
        long absolute_i = (i < 0 ? -i : i);
        string suffix;
        double readable;
        if (absolute_i >= UnitEb)
        {
            suffix = "EB";
            readable = (i >> 50);
        }
        else if (absolute_i >= UnitPb)
        {
            suffix = "PB";
            readable = (i >> 40);
        }
        else if (absolute_i >= UnitTb)
        {
            suffix = "TB";
            readable = (i >> 30);
        }
        else if (absolute_i >= UnitGb)
        {
            suffix = "GB";
            readable = (i >> 20);
        }
        else if (absolute_i >= UnitMb)
        {
            suffix = "MB";
            readable = (i >> 10);
        }
        else if (absolute_i >= UnitKb)
        {
            suffix = "KB";
            readable = i;
        }
        else
        {
            return i.ToString("D2") + " bytes";
        }
        readable = readable / 1024;
        return readable.ToString("0.# ") + suffix;
    }
}
