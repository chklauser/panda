using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Panda.Core;

namespace Panda.Test.Unit
{
    /// <summary>
    /// Here, we test the file system implementation against a mocked block API.
    /// If we have failures in this suite, we know that the file system implementation
    /// is to be blamed, and not the block API or the IO layer implementations.
    /// </summary>
    [TestFixture]  
    public class FileSystem : FileSystemBlackBox
    {
        /// <summary>
        /// Runs before every test-routine (e.g. CreateDirectory).
        /// </summary>
        [SetUp]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic",Justification = 
            "Setup routine must be an instance method\n" +
        "In this file, serves mainly as an example/reference for my team mates.\n" +
        "Also performance is not critical in unit tests (nice to have, but not ciritical, certainly not on the level of virtual vs static methods)")]
        public void SetUp()
        {
        }

        /// <summary>
        /// Runs after every test-routine.
        /// </summary>
        [TearDown]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Setup routine must be an instance method\n" +
        "In this file, serves mainly as an example/reference for my team mates.\n" +
        "Also performance is not critical in unit tests (nice to have, but not ciritical, certainly not on the level of virtual vs static methods)")]
        public void Teardown()
        {
        }

        [Test]
        public void CreateDirectory()
        {
            // You can customize your dummy disk via optional parameters.
            // For this test, we won't need a large disk.
            CreateMemDisk(totalBlockCount: 16);

            // Use the disk to create a directory 'd' in '/d'
            var dir = Disk.Root.CreateDirectory("d");

            Assert.That(dir, Is.Not.Null);

            Assert.That(dir.Count, Is.EqualTo(0), "directory should be empty");
            Assert.That(dir.Name, Is.EqualTo("d"), "directory name");
            Assert.That(dir.IsRoot, Is.False, "/d is not root");
            Assert.That(dir.ParentDirectory, Is.SameAs(Disk.Root), "/d should be child of /");
            Assert.That(dir.Size, Is.EqualTo(0), "directory should report size 0");

            // and so on and so forth.
        }

        [Test]
        public void CreateEmptyFile()
        {
            // For most small tests, the default disk configuration is enough.
            // (With a capacity of just 256 blocks, it's rather small)
            CreateMemDisk();

            var emptySize = Disk.Root.Size;

            var vf = Disk.Root.CreateFile("f", new byte[0]);
            Assert.That(Disk.Root.Contains("f"), Is.True, "Root should contain 'f'");
            Assert.That(Disk.Root.Navigate("f"), Is.SameAs(vf), "Navigating to /f should be the same as the file returned from CreateFile.");
            Assert.That(vf.Name, Is.EqualTo("f"), "File does not match.");
            Assert.That(vf.FullName, Is.EqualTo("/f"), "File full name does not match.");
            Assert.That(vf.ParentDirectory, Is.SameAs(Disk.Root), "/f should be child of /");
            Assert.That(vf.Size, Is.EqualTo(0), "File size");
            Assert.That(Disk.Root.Size, Is.EqualTo(emptySize), "reported root directory size.");

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

        /// <summary>
        /// Tests for navigating around in the file system with paths
        /// </summary>
        #region Navigate

        [Test, ExpectedException(typeof(PathNotFoundException))]
        public void NavigateToNotExistingDirectory()
        {
            CreateMemDisk();

            // check what happens when the directory doesn't exist
            Disk.Root.Navigate("idontexist");
        }

        [Test]
        public void Navigate()
        {
            CreateMemDisk();

            // create directory
            Disk.Root.CreateDirectory("peter");

            // check if navigate returns a directory
            Assert.That(Disk.Root.Navigate("peter"), Is.AssignableTo<VirtualDirectory>());

            // create a file in the directory
            ((VirtualDirectory) Disk.Root.Navigate("peter")).CreateFile("peter.txt", "");

            // check if navigate returns a file
            Assert.That(Disk.Root.Navigate("peter/peter.txt"), Is.AssignableTo<VirtualFile>());
        }

        #endregion

        [Test]
        public void CreateTextFile()
        {
            CreateMemDisk();

            Disk.Root.CreateFile("peter.txt", Encoding.UTF8.GetBytes("test"));

            //var returnStream = ((VirtualFile) Disk.Root.Navigate("peter.txt")).Open();
            //var stringReader = new StreamReader(returnStream, Encoding.UTF8);
            //var bla = stringReader.ReadToEnd();
            
            Assert.That((new StreamReader(((VirtualFile) Disk.Root.Navigate("peter.txt")).Open(), Encoding.UTF8)).ReadToEnd(), Is.EqualTo("test"));
        }
    }
}