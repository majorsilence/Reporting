

using System;
using Majorsilence.Reporting.Rdl;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Majorsilence.Reporting.Rdl
{
	
	///<summary>
	/// An implementation of IStreamGen.  Constructor is passed the name of a
	/// directory to place work files and the primary name of resultant file.  
	///</summary>

	[Serializable]
	public class StreamGen : IStreamGen, IDisposable
	{
		string _Directory;
		string _RelativeDirectory;
		StreamWriter _SW;
		Stream _io;
		string _FileName;
		Random _rand;
		List<string> _FileList;

		public StreamGen(string directory, string relativeDirectory, string ext)
		{
			_Directory = directory;
			_RelativeDirectory = relativeDirectory;
			if (_Directory[_Directory.Length-1] == Path.DirectorySeparatorChar ||
				_Directory[_Directory.Length-1] == Path.AltDirectorySeparatorChar)
				_Directory = _Directory.Substring(0, _Directory.Length-1);

			// ensure we have a separator before and after the relative directory name
			if (_RelativeDirectory == null)
				_RelativeDirectory = Path.DirectorySeparatorChar.ToString();

			if (!(_RelativeDirectory[0] == Path.DirectorySeparatorChar ||
				_RelativeDirectory[0] == Path.AltDirectorySeparatorChar))
				_RelativeDirectory = Path.DirectorySeparatorChar + _RelativeDirectory;

			if (!(_RelativeDirectory[_RelativeDirectory.Length-1] == Path.DirectorySeparatorChar ||
				 _RelativeDirectory[_RelativeDirectory.Length-1] == Path.AltDirectorySeparatorChar))
				_RelativeDirectory = _RelativeDirectory + Path.DirectorySeparatorChar;

			_FileList = new List<string>();

			string relativeName;
			_io = GetIOStream(out relativeName, ext);
			_FileName = _Directory + relativeName;
			_rand = null;
		
		}

		public List<string> FileList
		{
			get { return _FileList; }
		}
		
		public string FileName
		{
			get { return _FileName; }
		}
		
		public string RelativeDirectory
		{
			get { return _RelativeDirectory; }
		}

		#region IStreamGen Members
		public void CloseMainStream()
		{
			if (_SW != null)
			{
				_SW.Close();
				_SW = null;
			}
			if (_io != null)
			{
				_io.Close();
				_io = null;
			}
			return;
		}

		public Stream GetStream()
		{
			return this._io;
		}

		public TextWriter GetTextWriter()
		{
			if (_SW == null)
				_SW = new StreamWriter(_io);
			return _SW;
		}

		// create a new file in the directory specified and return
		//   a Stream caller can then write to.   relativeName is filled in with
		//   name we generate (sans the directory).
		// A private lock: locking on typeof(StreamGen) would also serialise with anyone else locking the type.
		private static readonly object _createFileLock = new object();

		public Stream GetIOStream(out string relativeName, string extension)
		{
			Stream io=null;
			lock (_createFileLock)	// single thread lock while we're creating a file
			{							//  this assumes no other routine creates files in this directory
				// Obtain a new file name
				if (_rand == null)
					_rand = new Random();
				int rnd = _rand.Next();

				string filename = _Directory + _RelativeDirectory + "f" + rnd.ToString() + "." + extension; 
				FileInfo fi = new FileInfo(filename);
				while (fi.Exists)
				{
					rnd = _rand.Next();
					filename = _Directory + _RelativeDirectory + "f" + rnd.ToString() + "." + extension; 
					fi = new FileInfo(filename);
				}

				relativeName = _RelativeDirectory + "f" + rnd.ToString() + "." + extension;
				io = fi.Create();
				_FileList.Add(filename);
			}
			return io; 
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			if (_SW != null)
			{
				_SW.Flush();
				_SW.Close();
				_SW = null;
			}
			if (_io != null)
			{
				_io.Flush();
				_io.Close();
				_io = null;
			}
		}

		#endregion
	}
}
