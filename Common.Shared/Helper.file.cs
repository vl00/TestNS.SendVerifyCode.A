using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public static partial class Program_ext_file
{
    public static void File_Del_NoError(string file)
    {
        if (File.Exists(file))
            File.Delete(file);
    }

    public static void Dir_MakeSureExists(string dir)
    {
        if (!Directory.Exists(dir)) 
		    Directory.CreateDirectory(dir);
    }

    public static void Dir_Del_NoError(string dir)
    {
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
    }

    public static void Dir_Move_Overwrite(string src, string dist)
    {
        Dir_Del_NoError(dist);
        Directory.Move(src, dist);
    }

    public static void Dir_Clear(string dir)
    {
        if (!Directory.Exists(dir)) return;
        var ls = new List<string>();
        ls.AddRange(Directory.EnumerateDirectories(dir));
        foreach (var ff in ls)
        {
            Directory.Delete(ff, true);
        }
        ls.Clear();
        ls.AddRange(Directory.EnumerateFiles(dir));
        foreach (var ff in ls)
        {
            File.Delete(ff);
        }
        ls.Clear();
    }

    public static IEnumerable<string> Dir_Match_Files(string dir, string regexStr)
    {
        foreach (var f in Directory.EnumerateFiles(dir))
        {
            if (Regex.IsMatch(f, regexStr))
                yield return f;
        }
    }
}
