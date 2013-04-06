using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Panda.Test.Integration
{
    public class SpecificationBase : IDisposable
    {
        public static readonly string DiskFileBaseName = typeof(Specification).Name;
        public string DiskFileName;
        public static int Count = 0;
        public VirtualDisk Disk;
        private VirtualDisk _managedDisk;
        public uint Capacity = 10*1024*1024;

        [SetUp]
        public virtual void SetUp()
        {
            Directory.CreateDirectory(DiskFileBaseName);
            DiskFileName = Path.Combine(DiskFileBaseName, Count + ".panda");
            if (File.Exists(DiskFileName))
                File.Delete(DiskFileName);
            Count++;
            Disk = _managedDisk = VirtualDisk.CreateNew(DiskFileName,Capacity);
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (Disk != null)
                Disk.Dispose();
            if (_managedDisk != null && _managedDisk != Disk)
                _managedDisk.Dispose();

            try
            {
                if (File.Exists(DiskFileName))
                {
                    File.Delete(DiskFileName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        protected static string ReadToEnd(VirtualFile file)
        {
            using (var fs = file.Open())
            {
                var reader = new StreamReader(fs, Encoding.UTF8, true, (int) VirtualFileSystem.DefaultBlockSize, true);
                return reader.ReadToEnd();
            }
        }

        protected static string GenerateData()
        {
            var dataSrc = Enumerable.Repeat("Hello World", 500);
            var dataBuilder = new StringBuilder((int) VirtualFileSystem.DefaultBlockSize);
            var i = 0;
            foreach (var src in dataSrc)
            {
                dataBuilder.Append(src);
                dataBuilder.Append(i++);
            }
            var data = dataBuilder.ToString();
            return data;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Disk != null)
                {
                    Disk.Dispose();
                    Disk = null;
                }

                var disposable = Disk as IDisposable;

                if (disposable != null)
                {
                    disposable.Dispose();
                }

                Disk = null;
            }
        }

        ~SpecificationBase()
        {
            Dispose(false);
        }
    }
}