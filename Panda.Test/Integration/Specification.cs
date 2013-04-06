using System.Collections.Generic;
using System.Diagnostics;
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
    // ReSharper disable InconsistentNaming
    [TestFixture]
    public class Specification : SpecificationBase
    {
        /// <summary>
        /// creates a disk at specified location with specified size
        /// </summary>
        [Test]
        public void Req2_1_1_and_2()
        {
            // location
            string vfsFileName = Path.Combine(typeof(Specification).Name, @"vfs.panda");

            // remove the file if it exists (this is to deal with the case where an earlier test failed.
            if (File.Exists(vfsFileName))
                File.Delete(vfsFileName);

            // capacity 10 MB
            const uint cap = 10*1024*1024;

            // create the virtual file system on the harddisk
            using (var Disk2 = VirtualDisk.CreateNew(vfsFileName, cap))
            {
                Assert.That(Disk2.Capacity, Is.GreaterThanOrEqualTo(cap), "Disk2 capacity");
            }

            // remove the file if it exists
            if (File.Exists(vfsFileName))
                File.Delete(vfsFileName);
        }

        /// <summary>
        /// creates two virtual disks and shows that they are not the same
        /// </summary>
        [Test]
        public void Req2_1_3()
        {
            // create a second disk
            var vfsFileName = Path.Combine(typeof(Specification).Name, @"vfs.panda");

            // remove the file if it exists (this is to deal with the case where an earlier test failed.
            if(File.Exists(vfsFileName))
                File.Delete(vfsFileName);

            using (var Disk2 = VirtualDisk.CreateNew(vfsFileName, Capacity))
            {

                // create a file on both disks with different content
                Disk.Root.CreateFile("peter.txt", Encoding.UTF8.GetBytes("test"));
                Disk2.Root.CreateFile("peter.txt", Encoding.UTF8.GetBytes("test2"));

                // check that the content can be read correctly
                Assert.That(
                    (new StreamReader(((VirtualFile) Disk.Root.Navigate("peter.txt")).Open(), Encoding.UTF8)).ReadToEnd(),
                    Is.EqualTo("test"));
                Assert.That(
                    (new StreamReader(((VirtualFile) Disk2.Root.Navigate("peter.txt")).Open(), Encoding.UTF8)).ReadToEnd
                        (), Is.EqualTo("test2"));

                // check that the content is not the same
                Assert.That(
                    (new StreamReader(((VirtualFile) Disk.Root.Navigate("peter.txt")).Open(), Encoding.UTF8)).ReadToEnd(),
                    Is.Not.EqualTo(
                        (new StreamReader(((VirtualFile) Disk2.Root.Navigate("peter.txt")).Open(), Encoding.UTF8))
                            .ReadToEnd()));

            }

            if (File.Exists(vfsFileName))
                File.Delete(vfsFileName);
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

        [Test]
        public void CreateFileLong()
        {
            var data = GenerateData();

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
            Assert.That(bytesRead, Is.EqualTo(20), "bytes read");
            Assert.That(Encoding.UTF8.GetString(buffer,0,bytesRead), Is.EqualTo(data.Substring(0,bytesRead)));
            var readSoFar = bytesRead;

            // read again
            bytesRead = str.Read(buffer, 0, 20);
            Assert.That(bytesRead, Is.EqualTo(20), "bytes read");
            Assert.That(Encoding.UTF8.GetString(buffer.Take(bytesRead).ToArray()), Is.EqualTo(data.Substring(readSoFar, bytesRead)));
            readSoFar += bytesRead;

            // read across edge
            var largeBuffer = new byte[VirtualFileSystem.DefaultBlockSize];
            bytesRead = str.Read(largeBuffer, 0, largeBuffer.Length);
            Assert.That(bytesRead,Is.LessThanOrEqualTo(largeBuffer.Length),"Bytes read compared to buffer length.");
            Assert.That(Encoding.UTF8.GetString(largeBuffer,0,bytesRead),Is.EqualTo(data.Substring(readSoFar,bytesRead)));
        }

        [Test]
        public void Import()
        {
            Disk.Root.Import("../../../Panda.Core/Core");

            var ioAny = Disk.Navigate("/Core/IO");
            Assert.That(ioAny,Is.Not.Null,"result of navigating to IO");
            Assert.That(ioAny,Is.AssignableTo<VirtualDirectory>(),"IO");
            var ioDir = (VirtualDirectory) ioAny;

            Assert.That(ioDir.Contains("RawBlockManager.cs"),Is.True,"IO should contain entity named RawBlockManager.cs");
            var rbmAny = ioDir.Navigate("RawBlockManager.cs");

            Assert.That(rbmAny, Is.Not.Null, "result of navigating to RawBlockManager.cs");
            Assert.That(rbmAny, Is.AssignableTo<VirtualFile>(), "RawBlockManager.cs");
            var rbmFile = (VirtualFile) rbmAny;
            Debug.Assert(rbmFile != null);
            Assert.That(rbmFile.Size, Is.GreaterThanOrEqualTo(VirtualFileSystem.DefaultBlockSize));
            var contents = ReadToEnd(rbmFile);
            Assert.That(contents.Length,Is.GreaterThanOrEqualTo(VirtualFileSystem.DefaultBlockSize));
            Assert.That(contents, Is.StringContaining("destinationRemainingLength"));

            Assert.That(rbmAny.FullName, Is.EqualTo("/Core/IO/RawBlockManager.cs".Replace('/',VirtualFileSystem.SeparatorChar)));
        }

        #region Disposal

        #endregion
    }
}
