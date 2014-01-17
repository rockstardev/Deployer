using System;
using System.Collections.Generic;
using System.Text;
using UsefulHeap.Rar;

namespace UsefulHeap.Rar
{
    class RarDirectory
    {
        public static void DecompressAtDirectory(string rarFile, string directoryPath)
        {
            // Create new unrar class and attach event handlers for
            // progress, missing volumes, and password
            Unrar unrar = new Unrar();
            //attachHandlers(unrar);

            // Set destination path for all files
            try
            {
                unrar.DestinationPath = directoryPath;

                // Open archive for extraction
                unrar.Open(rarFile, Unrar.OpenMode.Extract);

                // Extract each file found in hashtable
                while (unrar.ReadHeader())
                {
                    unrar.Extract();
                }
            }
            finally
            {
                unrar.Close();
            }
        }

        //private static void attachHandlers(Unrar unrar)
        //{
        //    unrar.ExtractionProgress += new ExtractionProgressHandler(unrar_ExtractionProgress);
        //    unrar.MissingVolume += new MissingVolumeHandler(unrar_MissingVolume);
        //    unrar.PasswordRequired += new PasswordRequiredHandler(unrar_PasswordRequired);
        //}
    }
}
