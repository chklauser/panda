using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Panda.Core.Blocks;
using Panda.Core.IO;
using Panda.Core.IO.MemoryMapped;
using Panda.Test.Unit;

namespace Panda.Test.Integration
{
    [TestFixture]
    public class Specification : IDisposable
    {
        public static readonly string DiskFileBaseName = typeof(Specification).Name;
        public string DiskFileName;
        public static int Count = 0;

        public VirtualDisk Disk;
        public uint Capacity = 10*1024*1024;

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(DiskFileBaseName);
            DiskFileName = Path.Combine(DiskFileBaseName, Count + ".panda");
            if (File.Exists(DiskFileName))
                File.Delete(DiskFileName);
            Count++;
            Disk = VirtualDisk.CreateNew(DiskFileName,Capacity);
        }

        [TearDown]
        public void TearDown()
        {
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

        [Test]
        public void CreateFile()
        {
            var vf = Disk.Root.CreateFile("f", new byte[0]);
            Assert.That(Disk.Root.Contains("f"), Is.True, "Root should contain 'f'");
            Assert.That(Disk.Root.Navigate("f"), Is.SameAs(vf), "Navigating to /f should be the same as the file returned from CreateFile.");
            Assert.That(vf.Name, Is.EqualTo("f"), "File does not match.");
            Assert.That(vf.FullName, Is.EqualTo("/f"), "File full name does not match.");
            Assert.That(vf.ParentDirectory, Is.SameAs(Disk.Root), "/f should be child of /");
            Assert.That(vf.Size, Is.EqualTo(0), "File size");

            // "read" the empty file
            var str = vf.Open();
            Assert.That(str, Is.Not.Null);
            Assert.That(str.CanRead, Is.True, "Stream should be readable");

            const byte guard = 0xEF;
            var buffer = Enumerable.Repeat(guard, 20).ToArray();
            var bytesRead = str.Read(buffer, 0, 20);
            Assert.That(bytesRead, Is.EqualTo(0), "bytes read");
            Assert.That(buffer, Is.All.EqualTo(guard), "The buffer was changed, even though no bytes were read. This indicates an error in the stream implementation");
        }

        [Test]
        public void CreateFileNoNav()
        {
            var vf = Disk.Root.CreateFile("f", new byte[0]);
            Assert.That(Disk.Root.Contains("f"), Is.True, "Root should contain 'f'");
            Assert.That(vf.Name, Is.EqualTo("f"), "File does not match.");
            Assert.That(vf.FullName, Is.EqualTo("/f"), "File full name does not match.");
            Assert.That(vf.ParentDirectory, Is.SameAs(Disk.Root), "/f should be child of /");
            Assert.That(vf.Size, Is.EqualTo(0), "File size");

            // "read" the empty file
            var str = vf.Open();
            Assert.That(str, Is.Not.Null);
            Assert.That(str.CanRead, Is.True, "Stream should be readable");

            const byte guard = 0xEF;
            var buffer = Enumerable.Repeat(guard, 20).ToArray();
            var bytesRead = str.Read(buffer, 0, 20);
            Assert.That(bytesRead, Is.EqualTo(0), "bytes read");
            Assert.That(buffer, Is.All.EqualTo(guard), "The buffer was changed, even though no bytes were read. This indicates an error in the stream implementation");
        }

        [Test]
        public void CreateFileShort()
        {
            const string data = "Hello World";
            var vf = Disk.Root.CreateFile("f", data);
            Assert.That(Disk.Root.Contains("f"), Is.True, "Root should contain 'f'");
            Assert.That(vf.Name, Is.EqualTo("f"), "File does not match.");
            Assert.That(vf.FullName, Is.EqualTo("/f"), "File full name does not match.");
            Assert.That(vf.ParentDirectory, Is.SameAs(Disk.Root), "/f should be child of /");
            Assert.That(vf.Size, Is.EqualTo(data.Length), "File size");

            // "read" the empty file
            var str = vf.Open();
            Assert.That(str, Is.Not.Null);
            Assert.That(str.CanRead, Is.True, "Stream should be readable");

            const byte guard = 0xEF;
            var buffer = Enumerable.Repeat(guard, 20).ToArray();
            var bytesRead = str.Read(buffer, 0, 20);
            Assert.That(bytesRead, Is.EqualTo(data.Length), "bytes read");
            Assert.That(Encoding.UTF8.GetString(buffer.Take(bytesRead).ToArray()),Is.EqualTo(data));
            Assert.That(buffer.Skip(data.Length), Is.All.EqualTo(guard), "The extra region of the buffer was changed. This indicates an error in the stream implementation");
        }

        #region Disposal

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

        ~Specification()
        {
            Dispose(false);
        }

        #endregion
    }
}
