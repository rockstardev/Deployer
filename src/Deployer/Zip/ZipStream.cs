/* #zlib - Wrapping and enhancing the zlib
 * Copyright (C) 2005-06, Tyron Madlener <zlib@tyron.at>
 * 
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * See COPYING for details
 */

using System;
using System.IO;
using System.Text;
using System.Collections;

namespace UsefulHeap.Zip {

	public class ZipStream : Stream {
		string _filename;
		ZipArchive archive;
		ZipEntry entry;

		#region Stream properties
		public override bool CanRead {
			get {
				return true;
			}
		}

		public override bool CanWrite {
			get {
				if(File.Exists(archive.Name)) {
					if((File.GetAttributes(archive.Name) & FileAttributes.ReadOnly) != 0)
						return false;
				} else {
					DirectoryInfo info = new DirectoryInfo(Path.GetDirectoryName(archive.Name));
					if((info.Attributes & FileAttributes.ReadOnly) != 0)
						return false;
				}
				return true;
			}
		}

		public override bool CanSeek {
			get {
				return false;
			}
		}

		public override long Length {
			get {
				if(entry==null) {
					archive.OpenWith(FileAccess.Read);

					int result = ZipLib.unzLocateFile(archive.Handle, _filename, 0);
					if (result < 0) {
						string msg = String.Format("Could not locate entry named '{0}'. Errorcode "+result, _filename);
						throw new ZipException(msg);
					}

					result = ZipLib.unzOpenCurrentFile(archive.Handle);
					if (result < 0) {			
						throw new ZipException("Could not open entry for reading.");
					}

					entry = new ZipEntry(archive.Handle);
				}

				return (long)entry.Length;
			}
		}

		public override long Position {
			get {
				throw new Exception("ZipStream does not support Postion setting/getting");			
			}
			set {
				throw new Exception("ZipStream does not support Postion setting/getting");
			}
		}
		#endregion

		public ZipStream(ZipArchive archive, string FileName) {			
			_filename = FileName;
			this.archive = archive;
		}

		#region Read&Write
		public string ReadToEnd() {
			byte []buf = new byte[Length];
			Read(buf,0,(int)Length);
			return System.Text.Encoding.Default.GetString(buf);
		}

		public override int Read(byte[] buffer, int offset, int count) {
			if(archive.Access == FileAccess.Write)
				throw new Exception("You can't read from archive in write mode");

			archive.OpenWith(FileAccess.Read);

			int result = ZipLib.unzLocateFile(archive.Handle, _filename, 0);
			if (result < 0) {
				string msg = String.Format("Could not locate entry named '{0}'. Errorcode "+result, _filename);
				throw new ZipException(msg);
			}

			result = ZipLib.unzOpenCurrentFile(archive.Handle);
			if (result < 0)
				throw new ZipException("Could not open entry for reading.");
			

			if (offset != 0)
				throw new ArgumentException("offset", "Only offset values of zero currently supported.");
			
			int bytesRead = ZipLib.unzReadCurrentFile(archive.Handle, buffer, (uint) count);
			if (bytesRead < 0)
				throw new ZipException("Error reading zip entry.");

			return bytesRead;		
		}

		public override void Write(byte[] buffer, int offset, int count) {
			ZipFileEntryInfo info;
			info.DateTime = DateTime.Now;
			bool FileExists = false;

			if(archive.Access == FileAccess.Read)
				throw new Exception("You can't write to archive in read mode");			

			// Check wether the file already exists in the archive
			if(File.Exists(archive.Name)) {
				archive.OpenWith(FileAccess.Read);				

				if(ZipLib.unzLocateFile(archive.Handle, _filename, 0)==0) 
					FileExists = true;
			}

			// Delete old file first
			if(FileExists) 
				archive.Delete(_filename);

			archive.OpenWith(FileAccess.Write);

			// Add the file
			int result = ZipLib.zipOpenNewFileInZip(archive.Handle, _filename, out info, null, 0, null, 0, String.Empty, (int)CompressionMethod.Deflated, (int)CompressionLevel.Default);
			if (result < 0) {			
				throw new ZipException("Could not open entry for writing.");
			}

			result = ZipLib.zipWriteInFileInZip(archive.Handle, buffer, (uint) count);
			if (result < 0) {			
				throw new ZipException("Could not write entry.");
			}
		}
		#endregion

		#region Stream stubs
		public override long Seek(long offset, SeekOrigin origin) {
			return 0;
		}

		public override void SetLength(long value) {

		}

		public override void Flush() {
			return;
		}
		#endregion
	}
}