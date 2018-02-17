using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace splitfile
{
    class Program
    {
        static readonly string SplitFileTemplate = "{0}.split{1}";
        static readonly int MaxCachedSize = 8 * 1024 * 1024;
        static readonly long MaxSize = (3 * 1024 * 1024 * 1024L - 500 * 1024 * 1024L) / 1;    // 3G - 500M
        static void Main(string[] args)
        {
            //string s = Console.ReadLine();
            //if (s != "1314")
            //{
            //    return;
            //}

            SplitAllVideo(@"./");
            // CombineAllVideo(@"./");
        }
        static void SplitAllVideo(string path)
        {
            List<string> Allfiles = new List<string>();
            Allfiles.AddRange(Directory.GetFiles(path, "*.mp4", SearchOption.AllDirectories));
            Allfiles.AddRange(Directory.GetFiles(path, "*.avi", SearchOption.AllDirectories));
            Allfiles.AddRange(Directory.GetFiles(path, "*.wmv", SearchOption.AllDirectories));
            foreach (var a in Allfiles)
            {
                Split(a);
            }
        }
        static void CombineAllVideo(string path)
        {
            List<string> Allfiles = new List<string>();
            Allfiles.AddRange(Directory.GetFiles(path, "*.split1", SearchOption.AllDirectories));
            foreach (var a in Allfiles)
            {
                Combile(a.Substring(0, a.Length - 7));
            }
        }
        // 将一个大文件分割成几分。
        static void Split(string fullname)
        {
            if (!File.Exists(fullname))
            {
                return;
            }

            var file = File.Open(fullname, FileMode.Open, FileAccess.Read);
            long maxsize = MaxSize;
            if (file.Length >= maxsize)
            {
                long cnt = (file.Length + maxsize - 1) / maxsize;
                long lastpos = file.Seek(0, SeekOrigin.End);
                while(cnt > 0)
                {
                    long filemaxsize = maxsize;
                    if (lastpos < maxsize)
                    {
                        filemaxsize = lastpos;
                        lastpos = 0;
                    }
                    else
                    {
                        lastpos -= maxsize;
                    }
                    file.Seek(lastpos, SeekOrigin.Begin);
                    string newfilename = string.Format(SplitFileTemplate, fullname, cnt);
                    if (File.Exists(newfilename))
                    {
                        File.Delete(newfilename);
                    }
                    using (var newfile = File.Create(newfilename))
                    {
                        byte[] buffer = new byte[MaxCachedSize];
                        int r = 0;
                        long total = 0;
                        do
                        {
                            if (total + MaxCachedSize > filemaxsize)
                            {
                                r = file.Read(buffer, 0, (int)(filemaxsize - total));
                            }
                            else
                            {
                                r = file.Read(buffer, 0, MaxCachedSize);
                            }
                            newfile.Write(buffer, 0, r);
                            total += r;
                        } while (r == MaxCachedSize && total < filemaxsize);

                        newfile.Flush();
                        newfile.Close();
                    }
                    cnt--;
                }
                file.Close();
                file.Dispose();

                File.Delete(fullname);
            }
        }
        // 将分割的文件合并成一个
        static void Combile(string fullname)
        {
            string newfilename = fullname + "";
            if (File.Exists(newfilename))
            {
                File.Delete(newfilename);
            }

            byte[] buffer = new byte[MaxCachedSize];

            var file = File.Open(newfilename, FileMode.Create, FileAccess.Write);
            int cnt = 1;
            string splitfile = string.Format(SplitFileTemplate, fullname, cnt);
            while (File.Exists(splitfile))
            {
                using (var partfile = File.OpenRead(splitfile))
                {
                    int r = 0;
                    do
                    {
                        r = partfile.Read(buffer, 0, MaxCachedSize);
                        file.Write(buffer, 0, r);
                    } while (r == MaxCachedSize);
                    partfile.Close();
                }
                File.Delete(splitfile);

                cnt++;
                splitfile = string.Format(SplitFileTemplate, fullname, cnt);
            }
        }
    }
}
