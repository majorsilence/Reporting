/* ====================================================================
   Copyright (C) 2004-2008  fyiReporting Software, LLC
   Copyright (C) 2011  Peter Gill <peter@majorsilence.com>

   This file is part of the fyiReporting RDL project.
	
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at 

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.


   For additional information, email info@fyireporting.com or visit
   the website www.fyiReporting.com.
*/
// File derived from http://www.c-sharpcorner.com/uploadfile/nschan/shapefile02252007134834pm/shapefile.aspx
// Filename:    ShapeFile.cs
// Description: Classes for reading ESRI shapefiles.
// Reference:   ESRI Shapefile Technical Description, July 1998.
//              http://www.esri.com/library/whitepapers/pdfs/shapefile.pdf
// 2007-01-22 nschan Initial revision.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Drawing;
using RdlMapFile.Resources;

namespace Majorsilence.Reporting.RdlMapFile
{
    /// <summary>
    /// Enumeration defining the various shape types. Each shapefile
    /// contains only one type of shape (e.g., all polygons or all
    /// polylines).
    /// </summary>
    public enum ShapeType
    {
        /// <summary>
        /// Nullshape / placeholder record.
        /// </summary>
        NullShape = 0,

        /// <summary>
        /// Point record, for defining point locations such as a city.
        /// </summary>
        Point = 1,

        /// <summary>
        /// One or more sets of connected points. Used to represent roads,
        /// hydrography, etc.
        /// </summary>
        PolyLine = 3,

        /// <summary>
        /// One or more sets of closed figures. Used to represent political
        /// boundaries for countries, lakes, etc.
        /// </summary>
        Polygon = 5,

        /// <summary>
        /// A cluster of points represented by a single shape record.
        /// </summary>
        Multipoint = 8

        // Unsupported types:
        // PointZ = 11,        
        // PolyLineZ = 13,        
        // PolygonZ = 15,        
        // MultiPointZ = 18,        
        // PointM = 21,        
        // PolyLineM = 23,        
        // PolygonM = 25,        
        // MultiPointM = 28,        
        // MultiPatch = 31
    }

    /// <summary>
    /// The ShapeFileHeader class represents the contents
    /// of the fixed length, 100-byte file header at the
    /// beginning of every shapefile.
    /// </summary>
    public class ShapeFileHeader
    {
        #region Private fields
        private int fileCode;
        private int fileLength;
        private int version;
        private int shapeType;

        // Bounding box.
        private double xMin;
        private double yMin;
        private double xMax;
        private double yMax;
        #endregion Private fields

        #region Constructor
        /// <summary>
        /// Constructor for the ShapeFileHeader class.
        /// </summary>
        public ShapeFileHeader()
        {
        }
        #endregion Constructor

        #region Properties
        /// <summary>
        /// Indicate the fixed-length of this header in bytes.
        /// </summary>
        public static int Length
        {
            get { return 100; }
        }

        /// <summary>
        /// Specifies the file code for an ESRI shapefile, which
        /// should be the value, 9994.
        /// </summary>
        public int FileCode
        {
            get { return this.fileCode; }
            set { this.fileCode = value; }
        }

        /// <summary>
        /// Specifies the length of the shapefile, expressed
        /// as the number of 16-bit words in the file.
        /// </summary>
        public int FileLength
        {
            get { return this.fileLength; }
            set { this.fileLength = value; }
        }

        /// <summary>
        /// Specifies the shapefile version number.
        /// </summary>
        public int Version
        {
            get { return this.version; }
            set { this.version = value; }
        }

        /// <summary>
        /// Specifies the shape type for the file. A shapefile
        /// contains only one type of shape.
        /// </summary>
        public int ShapeType
        {
            get { return this.shapeType; }
            set { this.shapeType = value; }
        }

        /// <summary>
        /// Indicates the minimum x-position of the bounding
        /// box for the shapefile (expressed in degrees longitude).
        /// </summary>
        public double XMin
        {
            get { return this.xMin; }
            set { this.xMin = value; }
        }

        /// <summary>
        /// Indicates the minimum y-position of the bounding
        /// box for the shapefile (expressed in degrees latitude).
        /// </summary>
        public double YMin
        {
            get { return this.yMin; }
            set { this.yMin = value; }
        }

        /// <summary>
        /// Indicates the maximum x-position of the bounding
        /// box for the shapefile (expressed in degrees longitude).
        /// </summary>       
        public double XMax
        {
            get { return this.xMax; }
            set { this.xMax = value; }
        }

        /// <summary>
        /// Indicates the maximum y-position of the bounding
        /// box for the shapefile (expressed in degrees latitude).
        /// </summary>
        public double YMax
        {
            get { return this.yMax; }
            set { this.yMax = value; }
        }
        #endregion Properties

        #region Public methods
        /// <summary>
        /// Output some of the fields of the file header.
        /// </summary>
        /// <returns>A string representation of the file header.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ShapeFileHeader: FileCode={0}, FileLength={1}, Version={2}, ShapeType={3}",
                this.fileCode, this.fileLength, this.version, this.shapeType);

            return sb.ToString();
        }
        #endregion Public methods
    }

    /// <summary>
    /// The ShapeFileRecord class represents the contents of
    /// a shape record, which is of variable length.
    /// </summary>
    public class ShapeFileRecord
    {
        #region Private fields
        // Record Header.
        private int recordNumber;
        private int contentLength;

        // Shape type.
        private int shapeType;

        // Bounding box for shape.
        private double xMin;
        private double yMin;
        private double xMax;
        private double yMax;

        // Part indices and points array.
        private Collection<int> parts = new Collection<int>();
        private Collection<PointF> points = new Collection<PointF>();

        // Shape attributes from a row in the dBASE file.
        private DataRow attributes;
        #endregion Private fields

        #region Constructor
        /// <summary>
        /// Constructor for the ShapeFileRecord class.
        /// </summary>
        public ShapeFileRecord()
        {
        }
        #endregion Constructor

        #region Properties
        /// <summary>
        /// Indicates the record number (or index) which starts at 1.
        /// </summary>
        public int RecordNumber
        {
            get { return this.recordNumber; }
            set { this.recordNumber = value; }
        }

        /// <summary>
        /// Specifies the length of this shape record in 16-bit words.
        /// </summary>
        public int ContentLength
        {
            get { return this.contentLength; }
            set { this.contentLength = value; }
        }

        /// <summary>
        /// Specifies the shape type for this record.
        /// </summary>
        public int ShapeType
        {
            get { return this.shapeType; }
            set { this.shapeType = value; }
        }

        /// <summary>
        /// Indicates the minimum x-position of the bounding
        /// box for the shape (expressed in degrees longitude).
        /// </summary>
        public double XMin
        {
            get { return this.xMin; }
            set { this.xMin = value; }
        }

        /// <summary>
        /// Indicates the minimum y-position of the bounding
        /// box for the shape (expressed in degrees latitude).
        /// </summary>
        public double YMin
        {
            get { return this.yMin; }
            set { this.yMin = value; }
        }

        /// <summary>
        /// Indicates the maximum x-position of the bounding
        /// box for the shape (expressed in degrees longitude).
        /// </summary>
        public double XMax
        {
            get { return this.xMax; }
            set { this.xMax = value; }
        }

        /// <summary>
        /// Indicates the maximum y-position of the bounding
        /// box for the shape (expressed in degrees latitude).
        /// </summary>
        public double YMax
        {
            get { return this.yMax; }
            set { this.yMax = value; }
        }

        /// <summary>
        /// Indicates the number of parts for this shape.
        /// A part is a connected set of points, analogous to
        /// a PathFigure in WPF.
        /// </summary>
        public int NumberOfParts
        {
            get { return this.parts.Count; }
        }

        /// <summary>
        /// Specifies the total number of points defining
        /// this shape record.
        /// </summary>
        public int NumberOfPoints
        {
            get { return this.points.Count; }
        }

        /// <summary>      
        /// A collection of indices for the points array.
        /// Each index identifies the starting point of the
        /// corresponding part (or PathFigure using WPF
        /// terminology).
        /// </summary>
        public Collection<int> Parts
        {
            get { return this.parts; }
        }

        /// <summary>
        /// A collection of all of the points defining the
        /// shape record.
        /// </summary>
        public Collection<PointF> Points
        {
            get { return this.points; }
        }

        /// <summary>
        /// Access the (dBASE) attribute values associated
        /// with this shape record.
        /// </summary>
        public DataRow Attributes
        {
            get { return this.attributes; }
            set { this.attributes = value; }
        }
        #endregion Properties

        #region Public methods
        /// <summary>
        /// Output some of the fields of the shapefile record.
        /// </summary>
        /// <returns>A string representation of the record.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ShapeFileRecord: RecordNumber={0}, ContentLength={1}, ShapeType={2}",
                this.recordNumber, this.contentLength, this.shapeType);
            
            return sb.ToString();
        }
        #endregion Public methods
    }

    /// <summary>
    /// The ShapeFileReadInfo class stores information about a shapefile
    /// that can be used by external clients during a shapefile read.
    /// </summary>
    public class ShapeFileReadInfo
    {
        #region Private fields
        private string fileName;
        private ShapeFile shapeFile;
        private Stream stream;
        private int numBytesRead;
        private int recordIndex;
        #endregion Private fields

        #region Constructor
        /// <summary>
        /// Constructor for the ShapeFileReadInfo class.
        /// </summary>
        public ShapeFileReadInfo()
        {
        }
        #endregion Constructor

        #region Properties
        /// <summary>
        /// The full pathname of the shapefile.
        /// </summary>
        public string FileName
        {
            get { return this.fileName; }
            set { this.fileName = value; }
        }

        /// <summary>
        /// A reference to the shapefile instance.
        /// </summary>
        public ShapeFile ShapeFile
        {
            get { return this.shapeFile; }
            set { this.shapeFile = value; }
        }

        /// <summary>
        /// An opened file stream for a shapefile.
        /// </summary>
        public Stream Stream
        {
            get { return this.stream; }
            set { this.stream = value; }
        }

        /// <summary>
        /// The number of bytes read from a shapefile so far.
        /// Can be used to compute a progress value.
        /// </summary>
        public int NumberOfBytesRead
        {
            get { return this.numBytesRead; }
            set { this.numBytesRead = value; }
        }

        /// <summary>
        /// A general-purpose record index.
        /// </summary>
        public int RecordIndex
        {
            get { return this.recordIndex; }
            set { this.recordIndex = value; }
        }       
        #endregion Properties

        #region Public methods
        /// <summary>
        /// Output some of the field values in the form of a string.
        /// </summary>
        /// <returns>A string representation of the field values.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ShapeFileReadInfo: FileName={0}, ", this.fileName);
            sb.AppendFormat("NumberOfBytesRead={0}, RecordIndex={1}", this.numBytesRead, this.recordIndex);

            return sb.ToString();
        }
        #endregion Public methods
    }

    /// <summary>
    /// The ShapeFile class represents the contents of a single
    /// ESRI shapefile. This is the class which contains functionality
    /// for reading shapefiles and their corresponding dBASE attribute
    /// files.
    /// </summary>
    /// <remarks>
    /// You can call the Read() method to import both shapes and attributes
    /// at once. Or, you can open the file stream yourself and read the file
    /// header or individual records one at a time. The advantage of this is
    /// that it allows you to implement your own progress reporting functionality,
    /// for example.
    /// </remarks>
    public class ShapeFile
    {
        #region Constants
        private const int expectedFileCode = 9994;
        #endregion Constants

        #region Private static fields
        private static byte[] intBytes = new byte[4];
        private static byte[] doubleBytes = new byte[8];
        #endregion Private static fields

        #region Private fields
        // File Header.
        private ShapeFileHeader fileHeader = new ShapeFileHeader();

        // Collection of Shapefile Records.
        private Collection<ShapeFileRecord> records = new Collection<ShapeFileRecord>();
        #endregion Private fields

        #region Constructor
        /// <summary>
        /// Constructor for the ShapeFile class.
        /// </summary>
        public ShapeFile()
        {
        }
        #endregion Constructor

        #region Properties
        /// <summary>
        /// Access the file header of this shapefile.
        /// </summary>
        public ShapeFileHeader FileHeader
        {
            get { return this.fileHeader; }
        }

        /// <summary>
        /// Access the collection of records for this shapefile.
        /// </summary>
        public Collection<ShapeFileRecord> Records
        {
            get { return this.records; }
        }
        #endregion Properties

        #region Public methods
        /// <summary>
        /// Read both shapes and attributes from the given
        /// shapefile (and its corresponding dBASE file).
        /// This is the top-level method for reading an ESRI
        /// shapefile.
        /// </summary>
        /// <param name="fileName">Full pathname of the shapefile.</param>
        public void Read(string fileName)
        {
            if ( string.IsNullOrEmpty(fileName) )
                throw new ArgumentNullException("fileName");

            // Read shapes first (geometry).
            this.ReadShapes(fileName);

            // Construct name and path of dBASE file. It's basically
            // the same name as the shapefile except with a .dbf extension.
            string dbaseFile = fileName.Replace(".shp", ".dbf");
            dbaseFile = dbaseFile.Replace(".SHP", ".DBF");

            // Read the attributes.
            this.ReadAttributes(dbaseFile);
        }

        /// <summary>
        /// Read shapes (geometry) from the given shapefile.
        /// </summary>
        /// <param name="fileName">Full pathname of the shapefile.</param>
        public void ReadShapes(string fileName)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                this.ReadShapes(stream);
            }
        }

        /// <summary>
        /// Read shapes (geometry) from the given stream.
        /// </summary>
        /// <param name="stream">Input stream for a shapefile.</param>
        public void ReadShapes(Stream stream)
        {
            // Read the File Header.
            this.ReadShapeFileHeader(stream);

            // Read the shape records.
            this.records.Clear();
            while (true)
            {
                try
                {
                    this.ReadShapeFileRecord(stream);
                }
                catch(IOException)
                {
                    // Stop reading when EOF exception occurs.
                    break;
                }
            }
        }

        /// <summary>
        /// Read the file header of the shapefile.
        /// </summary>
        /// <param name="stream">Input stream.</param>
        public void ReadShapeFileHeader(Stream stream)
        {
            // File Code.
            this.fileHeader.FileCode = ShapeFile.ReadInt32_BE(stream);
            if ( this.fileHeader.FileCode != ShapeFile.expectedFileCode )
            {
                string msg = String.Format(System.Globalization.CultureInfo.InvariantCulture, Strings.ShapeFile_ErrorA_InvalidFileCode, ShapeFile.expectedFileCode);
                throw new ApplicationException(msg);
            }

            // 5 unused values.
            ShapeFile.ReadInt32_BE(stream);
            ShapeFile.ReadInt32_BE(stream);
            ShapeFile.ReadInt32_BE(stream);
            ShapeFile.ReadInt32_BE(stream);
            ShapeFile.ReadInt32_BE(stream);

            // File Length.
            this.fileHeader.FileLength = ShapeFile.ReadInt32_BE(stream);

            // Version.
            this.fileHeader.Version = ShapeFile.ReadInt32_LE(stream);

            // Shape Type.
            this.fileHeader.ShapeType = ShapeFile.ReadInt32_LE(stream);

            // Bounding Box.
            this.fileHeader.XMin = ShapeFile.ReadDouble64_LE(stream);
            this.fileHeader.YMin = ShapeFile.ReadDouble64_LE(stream);
            this.fileHeader.XMax = ShapeFile.ReadDouble64_LE(stream);
            this.fileHeader.YMax = ShapeFile.ReadDouble64_LE(stream);

            // Adjust the bounding box in case it is too small.
            if ( Math.Abs(this.fileHeader.XMax - this.fileHeader.XMin) < 1 )
            {
                this.fileHeader.XMin -= 5;
                this.fileHeader.XMax += 5;
            }
            if ( Math.Abs(this.fileHeader.YMax - this.fileHeader.YMin) < 1 )
            {
                this.fileHeader.YMin -= 5;
                this.fileHeader.YMax += 5;
            }

            // Skip the rest of the file header.
            stream.Seek(100, SeekOrigin.Begin);
        }

        /// <summary>
        /// Read a shapefile record.
        /// </summary>
        /// <param name="stream">Input stream.</param>
        public ShapeFileRecord ReadShapeFileRecord(Stream stream)
        {
            ShapeFileRecord record = new ShapeFileRecord();

            // Record Header.
            record.RecordNumber = ShapeFile.ReadInt32_BE(stream);
            record.ContentLength = ShapeFile.ReadInt32_BE(stream);

            // Shape Type.
            record.ShapeType = ShapeFile.ReadInt32_LE(stream);

            // Read the shape geometry, depending on its type.
            switch (record.ShapeType)
            {
                case (int)ShapeType.NullShape:
                    // Do nothing.
                    break;
                case (int)ShapeType.Point:
                    ShapeFile.ReadPoint(stream, record);
                    break;
                case (int)ShapeType.PolyLine:
                    // PolyLine has exact same structure as Polygon in shapefile.
                    ShapeFile.ReadPolygon(stream, record);
                    break;
                case (int)ShapeType.Polygon:
                    ShapeFile.ReadPolygon(stream, record);
                    break;
                case (int)ShapeType.Multipoint:
                    ShapeFile.ReadMultipoint(stream, record);
                    break;
                default:
                    {
                        string msg = String.Format(System.Globalization.CultureInfo.InvariantCulture, Strings.ShapeFile_ErrorA_ShapeTypeNotSupported, (int)record.ShapeType);
                        throw new ApplicationException(msg);
                    }
            }

            // Add the record to our internal list.
            this.records.Add(record);

            return record;
        }

        /// <summary>
        /// Read the table from specified dBASE (DBF) file and
        /// merge the rows with shapefile records.
        /// </summary>
        /// <remarks>
        /// The filename of the dBASE file is expected to follow 8.3 naming
        /// conventions. If it doesn't follow the convention, we try to
        /// determine the 8.3 short name ourselves (but the name we come up
        /// with may not be correct in all cases).
        /// </remarks>
        /// <param name="dbaseFile">Full file path of the dBASE (DBF) file.</param>
        public void ReadAttributes(string dbaseFile)
        {
            if ( string.IsNullOrEmpty(dbaseFile) )
                throw new ArgumentNullException("dbaseFile");

            // Check if the file exists. If it doesn't exist,
            // this is not an error.
            if ( !File.Exists(dbaseFile) )
                return;

            // Get the directory in which the dBASE (DBF) file resides.
            FileInfo fi = new FileInfo(dbaseFile);
            string directory = fi.DirectoryName;

            // Get the filename minus the extension.
            string fileNameNoExt = fi.Name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            if ( fileNameNoExt.EndsWith(".DBF") )
                fileNameNoExt = fileNameNoExt.Substring(0, fileNameNoExt.Length - 4);

            // Convert to a short filename (may not work in every case!).
            if ( fileNameNoExt.Length > 8 )
            {
                if ( fileNameNoExt.Contains(" ") )
                {
                    string noSpaces = fileNameNoExt.Replace(" ", "");
                    if ( noSpaces.Length > 8 )
                        fileNameNoExt = noSpaces;
                }
                fileNameNoExt = fileNameNoExt.Substring(0, 6) + "~1";
            }

            // Set the connection string.
            string connectionString = "PROVIDER=Microsoft.Jet.OLEDB.4.0;Data Source=" + directory + ";Extended Properties=dBASE 5.0";

            // Set the select query.
            string selectQuery = "SELECT * FROM [" + fileNameNoExt + "#DBF];";

            // Create a database connection object using the connection string.
            OleDbConnection connection = new OleDbConnection(connectionString);

            // Create a database command on the connection using the select query.
            OleDbCommand command = new OleDbCommand(selectQuery, connection);

            try
            {
                // Open the connection.          
                connection.Open();

                // Create a data adapter to fill a dataset.
                OleDbDataAdapter dataAdapter = new OleDbDataAdapter();
                dataAdapter.SelectCommand = command;
                DataSet dataSet = new DataSet();
                dataSet.Locale = System.Globalization.CultureInfo.InvariantCulture;
                dataAdapter.Fill(dataSet);

                // Merge attributes into the shape file.
                if ( dataSet.Tables.Count > 0 )
                    this.MergeAttributes(dataSet.Tables[0]);
            }
            catch ( OleDbException )
            {
                // Note: An exception will occur if the filename of the dBASE
                // file does not follow 8.3 naming conventions. In this case,
                // you must use its short (MS-DOS) filename.

                // Rethrow the exception.
                throw;
            }
            finally
            {
                // Dispose of connection.
                ((IDisposable)connection).Dispose();
            }
        }

        /// <summary>
        /// Output the File Header in the form of a string.
        /// </summary>
        /// <returns>A string representation of the file header.</returns>
        public override string ToString()
        {
            return "ShapeFile: " + this.fileHeader.ToString();
        }
        #endregion Public methods

        #region Private methods
        /// <summary>
        /// Read a 4-byte integer using little endian (Intel)
        /// byte ordering.
        /// </summary>
        /// <param name="stream">Input stream to read.</param>
        /// <returns>The integer value.</returns>
        private static int ReadInt32_LE(Stream stream)
        {
            for (int i = 0; i < 4; i++)
            {
                int b = stream.ReadByte();
                if ( b == -1 )
                    throw new EndOfStreamException();
                intBytes[i] = (byte)b;
            }

            return BitConverter.ToInt32(intBytes, 0);
        }

        /// <summary>
        /// Read a 4-byte integer using big endian
        /// byte ordering.
        /// </summary>
        /// <param name="stream">Input stream to read.</param>
        /// <returns>The integer value.</returns>
        private static int ReadInt32_BE(Stream stream)
        {
            for (int i = 3; i >= 0; i--)
            {
                int b = stream.ReadByte();
                if ( b == -1 )
                    throw new EndOfStreamException();
                intBytes[i] = (byte)b;
            }

            return BitConverter.ToInt32(intBytes, 0);
        }

        /// <summary>
        /// Read an 8-byte double using little endian (Intel)
        /// byte ordering.
        /// </summary>
        /// <param name="stream">Input stream to read.</param>
        /// <returns>The double value.</returns>
        private static double ReadDouble64_LE(Stream stream)
        {            
            for (int i = 0; i < 8; i++)
            {
                int b = stream.ReadByte();
                if ( b == -1 )
                    throw new EndOfStreamException();
                doubleBytes[i] = (byte)b;
            }

            return BitConverter.ToDouble(doubleBytes, 0);
        }        

        /// <summary>
        /// Read a shapefile Point record.
        /// </summary>
        /// <param name="stream">Input stream.</param>
        /// <param name="record">Shapefile record to be updated.</param>
        private static void ReadPoint(Stream stream, ShapeFileRecord record)
        {
            // Points - add a single point.
            PointF p = new PointF();  
            p.X = (float)ShapeFile.ReadDouble64_LE(stream);
            p.Y = (float)ShapeFile.ReadDouble64_LE(stream);
            record.Points.Add(p);

            // Bounding Box.
            record.XMin = p.X;
            record.YMin = p.Y;
            record.XMax = record.XMin;
            record.YMax = record.YMin;
        }

        /// <summary>
        /// Read a shapefile MultiPoint record.
        /// </summary>
        /// <param name="stream">Input stream.</param>
        /// <param name="record">Shapefile record to be updated.</param>
        private static void ReadMultipoint(Stream stream, ShapeFileRecord record)
        {
            // Bounding Box.
            record.XMin = ShapeFile.ReadDouble64_LE(stream);
            record.YMin = ShapeFile.ReadDouble64_LE(stream);
            record.XMax = ShapeFile.ReadDouble64_LE(stream);
            record.YMax = ShapeFile.ReadDouble64_LE(stream);

            // Num Points.
            int numPoints = ShapeFile.ReadInt32_LE(stream);

            // Points.           
            for (int i = 0; i < numPoints; i++)
            {
                PointF p = new PointF();
                p.X = (float)ShapeFile.ReadDouble64_LE(stream);
                p.Y = (float)ShapeFile.ReadDouble64_LE(stream);
                record.Points.Add(p);
            }
        }

        /// <summary>
        /// Read a shapefile Polygon record.
        /// </summary>
        /// <param name="stream">Input stream.</param>
        /// <param name="record">Shapefile record to be updated.</param>
        private static void ReadPolygon(Stream stream, ShapeFileRecord record)
        {
            // Bounding Box.
            record.XMin = ShapeFile.ReadDouble64_LE(stream);
            record.YMin = ShapeFile.ReadDouble64_LE(stream);
            record.XMax = ShapeFile.ReadDouble64_LE(stream);
            record.YMax = ShapeFile.ReadDouble64_LE(stream);

            // Num Parts and Points.
            int numParts = ShapeFile.ReadInt32_LE(stream);
            int numPoints = ShapeFile.ReadInt32_LE(stream);

            // Parts.           
            for (int i = 0; i < numParts; i++)
            {
                record.Parts.Add(ShapeFile.ReadInt32_LE(stream));
            }

            // Points.           
            for (int i = 0; i < numPoints; i++)
            {
                PointF p = new PointF();
                p.X = (float)ShapeFile.ReadDouble64_LE(stream);
                p.Y = (float)ShapeFile.ReadDouble64_LE(stream);
                record.Points.Add(p);
            }
        }

        /// <summary>
        /// Merge data rows from the given table with
        /// the shapefile records.
        /// </summary>
        /// <param name="table">Attributes table.</param>
        private void MergeAttributes(DataTable table)
        {
            // For each data row, assign it to a shapefile record.
            int index = 0;
            foreach (DataRow row in table.Rows)
            {
                if ( index >= this.records.Count )
                    break;
                this.records[index].Attributes = row;
                ++index;
            }
        }
        #endregion Private methods
    }
}

// END

