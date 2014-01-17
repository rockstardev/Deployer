using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;

namespace Deployer.Logic
{
    public class FileSystemUtil
    {
        /// <summary>
        /// Wraps File class enabling coping of folders
        /// </summary>
        /// <param name="source">Source path</param>
        /// <param name="dest">Destination path</param>
        public static void CopyFolder(string source, string dest)
        {
            if (!dest.EndsWith("\\"))
                dest += "\\";

            if (!Directory.Exists(dest))
                Directory.CreateDirectory(dest);

            foreach (string file in Directory.GetFiles(source))
            {
                string fileDestionation = dest + Path.GetFileName(file);
                CopyFile(file, fileDestionation);
            }

            foreach (string folder in Directory.GetDirectories(source))
            {
                string subFolder = Path.GetFileName(folder);
                CopyFolder(folder, dest + "\\" + subFolder);
            }
        }

        /// <summary>
        /// Copy folder with option to exclude some folders/files
        /// </summary>
        /// <param name="source">Source path</param>
        /// <param name="dest">Destination path</param>
        /// <param name="excludePaths">List of strings of folders/files to exclude. If it is folder it must not end with \\ in order to have successful comparison</param>
        public static void CopyFolderWithExclude(string source, string dest, List<string> excludePaths)
        {
            if (excludePaths.Contains(source.TrimEnd('\\').ToLower()))
                return;

            if (!dest.EndsWith("\\"))
                dest += "\\";

            if (!Directory.Exists(dest))
                Directory.CreateDirectory(dest);

            foreach (string file in Directory.GetFiles(source))
            {
                string fileDestionation = dest + Path.GetFileName(file);

                if (!excludePaths.Contains(file))
                    CopyFile(file, fileDestionation);
            }

            foreach (string folder in Directory.GetDirectories(source))
            {
                string subFolder = Path.GetFileName(folder);
                CopyFolderWithExclude(folder, dest + "\\" + subFolder, excludePaths);
            }
        }

        /// <summary>
        /// Wraps File.Copy method because of read only and system files
        /// </summary>
        /// <param name="source">Path to file</param>
        /// <param name="destionation">Path to file</param>
        public static void CopyFile(string source, string destionation)
        {
            if (File.Exists(destionation))
                File.Delete(destionation);

            File.Copy(source, destionation);
        }

        /// <summary>
        /// Delete file from disk even if it is temporary taken by some other program
        /// </summary>
        /// <param name="filePath">Path to file</param>
        public static void DeleteFile(string filePath)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(deleteFileProcess), filePath);
        }
        private static void deleteFileProcess(object o)
        {
            try
            {
                File.Delete(o.ToString());
            }
            catch
            {
                Thread.Sleep(5000);
                deleteFileProcess(o);
                return;
            }
        }

        /// <summary>
        /// Reading file from disk with silent failing if it is taken
        /// </summary>
        /// <param name="filePath">Path to file</param>
        /// <returns>FileInfo if file is successfuly read, null if it is not</returns>
        public static FileInfo TryToReadFile(string filePath)
        {
            try
            {
                File.ReadAllBytes(filePath);
                return new FileInfo(filePath);
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// Deletes all content from folder
        /// </summary>
        /// <param name="dir">Path to folder</param>
        public static void ClearFolder(DirectoryInfo dir)
        {
            foreach (DirectoryInfo di in dir.GetDirectories())
                di.Delete();

            foreach (FileInfo fi in dir.GetFiles())
                fi.Delete();
        }
    }
}
