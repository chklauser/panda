using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Panda.Core;
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

        /// <summary>
        /// disposes a virtual disk
        /// </summary>
        [Test]
        public void Req2_1_4()
        {
            Disk.Dispose();
        }

        /// <summary>
        /// creates, deletes, renames directories and files
        /// </summary>
        [Test]
        public void Req2_1_5()
        {
            // create a file
            Assert.That(Disk.Root.CreateFile("peter.txt", Encoding.UTF8.GetBytes("test")), Is.AssignableTo<VirtualFile>());

            // check that the file exists and is a file
            Assert.That(Disk.Root.Navigate("peter.txt"), Is.AssignableTo<VirtualFile>());

            // delete the file
            Disk.Root.Navigate("peter.txt").Delete();

            // check that the file is deleted
            Assert.That(Disk.Root, Is.All.Null);

            // create a directory
            Assert.That(Disk.Root.CreateDirectory("dir"), Is.AssignableTo<VirtualDirectory>());

            // create a subdirectory
            Assert.That(((VirtualDirectory)Disk.Root.Navigate("dir")).CreateDirectory("asdf"), Is.AssignableTo<VirtualDirectory>());

            // create a file in the subdirectory
            Assert.That(((VirtualDirectory)Disk.Root.Navigate("dir/asdf")).CreateFile("peter.txt", Encoding.UTF8.GetBytes("test")), Is.AssignableTo<VirtualFile>());

            // delete the subdirectory
            Disk.Root.Navigate("dir/asdf").Delete();

            // check if the directory is empty
            Assert.That(Disk.Root.Navigate("dir"), Is.All.Null);
        }

        [Test]
        public void RenameFile()
        {
            const string data = "Hello World";
            var peter = Disk.Root.CreateFile("peter.txt", data);

            var sizeOrig = peter.Size;
            const string newName = "bob.txt";
            peter.Rename(newName);

            Assert.That(Disk.Root.ContentNames,Is.EquivalentTo(new[]{newName}));
            Assert.That(peter.Name,Is.EqualTo(newName));
            Assert.That(peter.FullName,Is.EqualTo(VirtualFileSystem.SeparatorChar + newName));

            Assert.That(peter.Size,Is.EqualTo(sizeOrig),"Size stays the same");
            Assert.That(ReadToEnd(peter),Is.EqualTo(data));
        }

        [Test]
        public void RenameDirectory()
        {
            const string data = "Hello World";
            var dir = Disk.Root.CreateDirectory("dir");
            var peter = dir.CreateFile("peter.txt", data);

            const string newName = "dori";
            dir.Rename(newName);

            Assert.That(Disk.Root.ContentNames, Is.EquivalentTo(new[] { newName }));
            Assert.That(dir.Name, Is.EqualTo(newName));
            Assert.That(dir.FullName, Is.EqualTo(VirtualFileSystem.SeparatorChar + newName));

            Assert.That(ReadToEnd(peter), Is.EqualTo(data));
            Assert.That(peter.FullName,Is.StringStarting(dir.FullName));
        }

        [Test,ExpectedException(typeof(PathAlreadyExistsException))]
        public void RenameFileConflict()
        {
            const string data = "Hello World";
            const string newName = "bob.txt";
            var peter = Disk.Root.CreateFile("peter.txt", data);
            Disk.Root.CreateFile("bob.txt", data);
            
            peter.Rename(newName);
        }

        /// <summary>
        /// creates, lists, and navigates
        /// </summary>
        [Test]
        public void Req2_1_6()
        {
            // create two directories
            Assert.That(Disk.Root.CreateDirectory("a"), Is.AssignableTo<VirtualDirectory>());
            Assert.That(Disk.Root.CreateDirectory("b"), Is.AssignableTo<VirtualDirectory>());

            // list the directories
            Assert.That(Disk.Root.Count, Is.EqualTo(2));

            // create some files in one of it
            Assert.That(((VirtualDirectory)Disk.Root.Navigate("a")).CreateFile("x", Encoding.UTF8.GetBytes("test")), Is.AssignableTo<VirtualFile>());
            Assert.That(((VirtualDirectory)Disk.Root.Navigate("a")).CreateFile("y", Encoding.UTF8.GetBytes("test")), Is.AssignableTo<VirtualFile>());
            Assert.That(((VirtualDirectory)Disk.Root.Navigate("a")).CreateFile("z", Encoding.UTF8.GetBytes("test")), Is.AssignableTo<VirtualFile>());

            // list the files
            Assert.That(((VirtualDirectory)Disk.Root.Navigate("a")).Count, Is.EqualTo(3));

            // navigate with absolute path
            Assert.That(Disk.Root.Navigate("/a/x"), Is.AssignableTo<VirtualFile>());

            // navigate with relative path
            Assert.That(((VirtualDirectory)Disk.Root.Navigate("a")).Navigate("x"), Is.AssignableTo<VirtualFile>());
        }

        /// <summary>
        /// moves, copies around files and directories
        /// </summary>
        [Test]
        public void Req2_1_7()
        {
            // create directory
            Assert.That(Disk.Root.CreateDirectory("a"), Is.AssignableTo<VirtualDirectory>());

            // create a file in directory
            Assert.That(((VirtualDirectory)Disk.Root.Navigate("a")).CreateFile("peter.txt", Encoding.UTF8.GetBytes("test")), Is.AssignableTo<VirtualFile>());

            // create another directroy
            Assert.That(Disk.Root.CreateDirectory("b"), Is.AssignableTo<VirtualDirectory>());

            // move file into other directory
            ((VirtualFile) Disk.Root.Navigate("/a/peter.txt")).Move((VirtualDirectory)Disk.Root.Navigate("b"));

            // check if file was moved
            Assert.That(Disk.Root.Navigate("/b/peter.txt"), Is.AssignableTo<VirtualFile>());
            Assert.That(((VirtualDirectory)Disk.Root.Navigate("/a")).Count, Is.EqualTo(0));

            // copy file to original directory
            ((VirtualFile) Disk.Root.Navigate("/b/peter.txt")).Copy((VirtualDirectory)Disk.Root.Navigate("a"));
            
            // check if both files have the same content
            Assert.That(
                (new StreamReader(((VirtualFile) Disk.Root.Navigate("/b/peter.txt")).Open(), Encoding.UTF8)).ReadToEnd(),
                Is.EqualTo((new StreamReader(((VirtualFile) Disk.Root.Navigate("/a/peter.txt")).Open(), Encoding.UTF8)).ReadToEnd()));

            // move directory into other directory
            ((VirtualDirectory)Disk.Root.Navigate("a")).Move((VirtualDirectory)Disk.Root.Navigate("b"));

            // check if directory was moved
            Assert.That(Disk.Root.Navigate("/b/a"),Is.AssignableTo<VirtualDirectory>());
            Assert.That(Disk.Root.Count, Is.EqualTo(1));

            // copy directory to root
            ((VirtualDirectory)Disk.Root.Navigate("/b/a")).Copy((VirtualDirectory)Disk.Root);

            // check if file was copied too
            Assert.That(
                (new StreamReader(((VirtualFile) Disk.Root.Navigate("/a/peter.txt")).Open(), Encoding.UTF8)).ReadToEnd(),
                Is.EqualTo((new StreamReader(((VirtualFile) Disk.Root.Navigate("/b/a/peter.txt")).Open(), Encoding.UTF8)).ReadToEnd()));
        }

        /// <summary>
        /// imports files and directories from host filesystem
        /// </summary>
        [Test]
        public void Req2_1_8_and_9()
        {
            const string oldFileName = "peter.txt";
            const string newFileName = "peter_new.txt";

            if(File.Exists(oldFileName))
                File.Delete(oldFileName);
            
            if (File.Exists(newFileName))
                File.Delete(newFileName);

            // create a file
            Assert.That(Disk.Root.CreateFile(oldFileName, Encoding.UTF8.GetBytes("test0mat")), Is.AssignableTo<VirtualFile>());

            // export the file
            ((VirtualFile)Disk.Root.Navigate(oldFileName)).Export(@"peter.txt");

            // move file on host filesytem
            File.Move(oldFileName, newFileName);

            // import the file
            Disk.Root.Import(newFileName);

            // compare the file contents
            Assert.That(
                (new StreamReader(((VirtualFile)Disk.Root.Navigate(oldFileName)).Open(), Encoding.UTF8)).ReadToEnd(),
                Is.EqualTo((new StreamReader(((VirtualFile)Disk.Root.Navigate(newFileName)).Open(), Encoding.UTF8)).ReadToEnd()));

            if (File.Exists(oldFileName))
                File.Delete(oldFileName);

            if (File.Exists(newFileName))
                File.Delete(newFileName);
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public void Req2_1_10()
        {
            // directory size should be 0 in a new disk
            Assert.That(Disk.Root.Size, Is.EqualTo(0));

            // create a file with 43285 bytes in the root directory
            Assert.That(Disk.Root.CreateFile("f", new byte[43285]), Is.AssignableTo<VirtualFile>());

            // check size of root block
            Assert.That(Disk.Root.Size, Is.EqualTo(43285));

            // create a subdirectory
            Assert.That(Disk.Root.CreateDirectory("q"), Is.AssignableTo<VirtualDirectory>());

            // copy file into a subdirectory
            Disk.Root.Navigate("f").Copy((VirtualDirectory)Disk.Root.Navigate("q"));

            // check size of root block
            Assert.That(Disk.Root.Size, Is.EqualTo(2*43285));
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
