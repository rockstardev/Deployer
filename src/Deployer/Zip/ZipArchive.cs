// Copyright (C) 2001 Gerry Shaw
//
// This software is provided 'as-is', without any express or implied
// warranty.	In no event will the authors be held liable for any damages
// arising from the use of this software.
//
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
//
// 1. The origin of this software must not be misrepresented; you must not
//	claim that you wrote the original software. If you use this software
//	in a product, an acknowledgment in the product documentation would be
//	appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
//	misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.
//
// Gerry Shaw (gerry_shaw@yahoo.com)

/* #zlib - Wrapping and enhancing the zlib
 * Copyright (C) 2005-06, Tyron Madlener <zlib@tyron.at>
 * 
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundantion; either version 2 of the License, or
 * (at your option) any later version.
 *
 * See COPYING for details
 */

using System;
using System.IO;
using System.Collections;
using System.Runtime.Serialization;

namespace UsefulHeap.Zip
{
    /// <summary>Provides support for reading files in the ZIP file format. Includes support for both compressed and uncompressed entries.</summary>
    public class ZipArchive : IEnumerator, IEnumerable, IDisposable
    {
        #region Fields
        /// <summary>ZipFile handle to read data from.</summary>
        IntPtr _handle = IntPtr.Zero;

        /// <summary>Name of zip file.</summary>
        string _fileName = null;

        /// <summary>Contents of zip file directory.</summary>
        ZipEntryCollection _entries = null;

        /// <summary>Global zip file comment.</summary>
        string _comment = null;

        /// <summary>True if an entry is open for reading.</summary>
        bool _entryOpen = false;

        /// <summary>Current zip entry open for reading.</summary>
        ZipEntry _current = null;

        FileAccess CurrentAccess;
        #endregion

        #region Properties
        FileAccess _access;
        public FileAccess Access
        {
            get
            {
                return _access;
            }
            set
            {
                _access = value;
                if (value == FileAccess.ReadWrite)
                    CurrentAccess = FileAccess.Read;
                else CurrentAccess = value;
            }
        }

        public IntPtr Handle
        {
            get
            {
                return _handle;
            }
            set
            {
                _handle = value;
            }
        }

        public Stream this[string file]
        {
            get
            {
                ZipStream z = new ZipStream(this, file);
                return (Stream)z;
            }
        }

        /// <summary>Gets the name of the zip file that was passed to the constructor.</summary>
        public string Name
        {
            get { return _fileName; }
        }

        object IEnumerator.Current
        {
            get
            {
                return _current;
            }
        }

        /// <summary>Gets the current entry in the zip file..</summary>
        public ZipEntry Current
        {
            get
            {
                return _current;
            }
        }

        /// <summary>Gets the global comment for the zip file.</summary>
        // sucks
        /*public string Comment {
            get {
            if (_comment == null) {
                FileAccess _lastmode = _access;
                // Assure that we are in Read Mode (does nothing if we already are)
                OpenWith(FileAccess.Read);

                ZipFileInfo info;
                int result = 0;
					
                result = ZipLib.unzGetGlobalInfo(_handle, out info);
					
                if (result < 0) {
                string msg = String.Format("Could not read comment from zip file '{0}'.", Name);
                throw new ZipException(msg);
                }

                sbyte[] buffer = new sbyte[info.CommentLength];
                result = ZipLib.unzGetGlobalComment(_handle, buffer, (uint) buffer.Length);
                if (result < 0) {
                string msg = String.Format("Could not read comment from zip file '{0}'.", Name);
                throw new ZipException(msg);
                }
                _comment = ZipLib.AnsiToString(buffer);

                OpenWith(_lastmode);
            }
            return _comment;
            }
        }*/
        #endregion

        #region Constructore, Dispose
        /// <summary>Initializes a instance of the <see cref="ZipReader"/> class for reading the zip file with the given name.</summary>
        /// <param name="fileName">The name of zip file that will be read.</param>
        public ZipArchive(string fileName, FileAccess access)
        {
            _fileName = fileName;
            Access = access;

            if (access != FileAccess.Write)
                Open();
        }

        /// <summary>Cleans up the resources used by this zip file.</summary>
        ~ZipArchive()
        {
            CloseFile();
        }

        /// <remarks>Dispose is synonym for Close.</remarks>
        void IDisposable.Dispose()
        {
            Close();
        }
        #endregion

        #region Opening and Closing of the Archive
        private void Open()
        {
            if (CurrentAccess == FileAccess.Read)
            {
                _handle = ZipLib.unzOpen(_fileName);
                if (_handle == IntPtr.Zero)
                    throw new ZipException("Unable to open archive '" + _fileName + "' for reading");

            }
            else
            {
                AppendStatus stat = AppendStatus.AddInZip;
                if (!File.Exists(_fileName)) stat = AppendStatus.Create;

                _handle = ZipLib.zipOpen(_fileName, (int)stat);

                if (_handle == IntPtr.Zero)
                    throw new ZipException("Unable to open archive '" + _fileName + "' for writing");
            }
        }

        public void OpenWith(FileAccess newaccess)
        {
            if (newaccess == FileAccess.ReadWrite)
                throw new ArgumentException("Read and Write simultanously is not possible", "access");

            if (newaccess != CurrentAccess || Handle == IntPtr.Zero)
            {
                Close();
                CurrentAccess = newaccess;
                Open();
            }
        }

        /// <summary>Closes the zip file and releases any resources.</summary>
        public void Close()
        {
            // Free unmanaged resources.
            CloseFile();

            // If base type implements IDisposable we would call it here.

            // Request the system not call the finalizer method for this object.
            GC.SuppressFinalize(this);
        }

        private void CloseFile()
        {
            int result;

            if (_handle != IntPtr.Zero)
            {
                if (CurrentAccess == FileAccess.Read)
                {
                    CloseEntry();
                    result = ZipLib.unzClose(_handle);
                }
                else
                    result = ZipLib.zipClose(_handle, _comment);

                if (result < 0)
                {
                    throw new ZipException("Could not close zip file.");
                }
                _handle = IntPtr.Zero;
            }
        }
        #endregion

        #region Getting/Iterating Entries
        /// <summary>Gets a <see cref="ZipEntryCollection"/> object that contains all the entries in the zip file directory.</summary>
        public ZipEntryCollection GetAllEntries()
        {
            if (Access == FileAccess.Write)
                throw new ZipException("You can't read information in write mode");

            if (Access == FileAccess.ReadWrite)
                OpenWith(FileAccess.Read);

            if (_entries == null)
            {
                _entries = new ZipEntryCollection();

                int result = ZipLib.unzGoToFirstFile(_handle);
                while (result == 0)
                {
                    ZipEntry entry = new ZipEntry(_handle);
                    _entries.Add(entry);
                    result = ZipLib.unzGoToNextFile(_handle);
                }
            }

            return _entries;
        }

        /// <summary>Advances the enumerator to the next element of the collection.</summary>
        /// <summary>Sets <see cref="Current"/> to the next zip entry.</summary>
        /// <returns><c>true</c> if the next entry is not <c>null</c>; otherwise <c>false</c>.</returns>
        public bool MoveNext()
        {
            if (Access == FileAccess.Write)
                throw new ZipException("You can't read information in write mode");

            if (Access == FileAccess.ReadWrite)
                OpenWith(FileAccess.Read);

            // close any open entry
            CloseEntry();

            int result;
            if (_current == null)
            {
                result = ZipLib.unzGoToFirstFile(_handle);
            }
            else
            {
                result = ZipLib.unzGoToNextFile(_handle);
            }
            if (result < 0)
            {
                // last entry found - not an exceptional case
                _current = null;
            }
            else
            {
                // entry found
                OpenEntry();
            }

            return (_current != null);
        }

        /// <summary>Move to just before the first entry in the zip directory.</summary>
        public void Reset()
        {
            CloseEntry();
            _current = null;
        }


        private void OpenEntry()
        {
            if (!_entryOpen)
            {
                _current = new ZipEntry(_handle);
                int result = ZipLib.unzOpenCurrentFile(_handle);
                if (result < 0)
                {
                    _current = null;
                    throw new ZipException("Could not open entry for reading.");
                }
                _entryOpen = true;
            }
        }

        private void CloseEntry()
        {
            if (_entryOpen)
            {
                int result = ZipLib.unzCloseCurrentFile(_handle);
                if (result < 0)
                {
                    switch ((ErrorCode)result)
                    {
                        case ErrorCode.CrcError:
                            throw new ZipException("All the file was read but the CRC did not match.");

                        default:
                            throw new ZipException("Could not close zip entry.\nErrorcode: " + result);
                    }
                }
                _entryOpen = false;
            }
        }
        #endregion

        #region Deleting/Checking a file from archive
        public bool Contains(string file)
        {
            if (Access == FileAccess.Write)
                throw new Exception("You can't write to archive in read mode");

            OpenWith(FileAccess.Read);

            if (ZipLib.unzLocateFile(Handle, file, 0) == 0) return true;

            return false;
        }

        public bool Delete(string del_file)
        {
            bool some_was_del = false;

            string tmp_name = this.Name + ".tmp";

            OpenWith(FileAccess.Read);

            IntPtr szip = this.Handle;

            if (szip == IntPtr.Zero)
                throw new ZipException("Unable to open zip file for reading");

            IntPtr dzip = ZipLib.zipOpen(tmp_name, (int)AppendStatus.Create);

            if (dzip == IntPtr.Zero)
                throw new ZipException("Unable to open temp file for writing");

            ZipFileInfo glob_info = new ZipFileInfo();

            int result = ZipLib.unzGetGlobalInfo(szip, out glob_info);

            // get global commentary
            if (result < 0)
                throw new ZipException("Unable to get global info from reading zip.\nErrorcode: " + result);

            string glob_comment = String.Empty;
            if (glob_info.CommentLength > 0)
            {
                sbyte[] buffer = new sbyte[glob_info.CommentLength];
                result = ZipLib.unzGetGlobalComment(szip, buffer, (uint)buffer.Length);
                if (result < 0)
                {
                    string msg = String.Format("Could not read comment from zip file '{0}'.", Name);
                    throw new ZipException(msg);
                }
                glob_comment = ZipLib.AnsiToString(buffer);
            }

            int n_files = 0;

            int rv = ZipLib.unzGoToFirstFile(szip);
            sbyte[] dos_fn = new sbyte[ZipLib.MAX_PATH];

            byte[] buf;

            while (rv == (int)ErrorCode.Ok)
            {
                ZipEntry entry = new ZipEntry(szip);

                // copy all entries excepted the one to be deleted
                if (entry.Name == del_file)
                    some_was_del = true;
                else
                {

                    // open file for RAW reading
                    int method;
                    int level;
                    if (ZipLib.unzOpenCurrentFile2(szip, out method, out level, 1) < 0)
                        break;


                    buf = new byte[entry.CompressedLength];

                    // read file
                    int sz = ZipLib.unzReadCurrentFile(szip, buf, (UInt32)entry.CompressedLength);
                    if (sz != entry.CompressedLength) break;

                    // open desitnation file
                    ZipFileEntryInfo info;
                    info.DateTime = entry.ModifiedTime;

                    byte[] extra = null;
                    uint extraLength = 0;
                    if (entry.ExtraField != null)
                    {
                        extra = entry.ExtraField;
                        extraLength = (uint)entry.ExtraField.Length;
                    }

                    result = ZipLib.zipOpenNewFileInZip2(dzip, entry.Name, out info, extra,
                    extraLength, null, 0, entry.Comment, (int)method, level, 1);

                    if (result < 0) throw new ZipException("Unable to open file in dest. zip for writing.\nErrorcode: " + result);

                    // write file
                    result = ZipLib.zipWriteInFileInZip(dzip, buf, (UInt32)entry.CompressedLength);
                    if (result < 0) throw new ZipException("Unable to write to opened file in dest. zip.\nErrorcode: " + result);

                    result = ZipLib.zipCloseFileInZipRaw(dzip, (UInt32)entry.Length, (UInt32)entry.Crc);
                    if (result < 0) throw new ZipException("Unable to close opened file in dest. zip.\nErrorcode: " + result);

                    result = ZipLib.unzCloseCurrentFile(szip);
                    if (result == (int)ErrorCode.CrcError) break;

                    n_files++;
                }

                rv = ZipLib.unzGoToNextFile(szip);
            }

            ZipLib.zipClose(dzip, glob_comment);

            Close();

            // if failes
            if (!some_was_del || rv != (int)ErrorCode.EndOfListOfFile)
            {
                File.Delete(tmp_name);
                return false;
            }

            File.Delete(Name);

            if (n_files == 0)
                File.Delete(tmp_name);
            else File.Move(tmp_name, Name);

            return true;
        }
        #endregion

        #region IEnumerable Member

        public IEnumerator GetEnumerator()
        {
            return (IEnumerator)this;
        }

        #endregion
    }
}
