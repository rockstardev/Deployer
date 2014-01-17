using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace UsefulHeap.Zip
{
    class ZipDirectory
    {
        public static void CompressDirectory(string toZipFile, string directoryPath)
        {
            ZipArchive newArchive = new ZipArchive(toZipFile, FileAccess.Write);

            CompressDirectory(newArchive, directoryPath);

            newArchive.Close();
        }


        public static void CompressDirectory(ZipArchive archive, string directoryPath)
        {
            CompressDirectory(archive, directoryPath, "");
        }

        private static void CompressDirectory(ZipArchive archive, string directoryPath, string relativePrefix)
        {
            DirectoryInfo di = new DirectoryInfo(directoryPath);

            foreach (DirectoryInfo childDir in di.GetDirectories())
            {
                string newRelativePrefix = relativePrefix + childDir.Name + "/";
                //                archive[newRelativePrefix].Write(new byte[] { }, 0, 0);
                CompressDirectory(archive, childDir.FullName, newRelativePrefix);
            }

            foreach (FileInfo fi in di.GetFiles())
            {
                byte[] content = File.ReadAllBytes(fi.FullName);
                archive[relativePrefix + fi.Name].Write(content, 0, content.Length);
            }
        }

        public static void DecompressAtDirectory(string zipFile, string directoryPath)
        {
            ZipArchive newArchive = new ZipArchive(zipFile, FileAccess.Read);

            DecompressAtDirectory(newArchive, directoryPath);

            newArchive.Close();
        }

        public static void DecompressAtDirectory(ZipArchive archive, string directoryPath)
        {
            DirectoryInfo di = new DirectoryInfo(directoryPath);
            if (!di.Exists)
                di.Create();

            // Create directories
            foreach (ZipEntry ze in archive.GetAllEntries())
            {
                if (ze.Name.Contains("/"))
                    di.CreateSubdirectory(ze.Name.Substring(0, ze.Name.LastIndexOf('/')));
            }

            foreach (ZipEntry ze in archive.GetAllEntries())
            {
                if (!ze.IsDirectory)
                {
                    using (FileStream fs = new FileStream(di.FullName + "\\" + ze.Name, FileMode.OpenOrCreate))
                    {
                        byte[] file = new byte[ze.Length];
                        archive[ze.Name].Read(file, 0, file.Length);

                        fs.Write(file, 0, file.Length);
                    }
                }
            }
        }
    }
}
