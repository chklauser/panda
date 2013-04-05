using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using NUnit.Framework;
using Panda.Core.IO;
using Panda.Core.IO.MemoryMapped;

namespace Panda.Test.Unit
{
    [TestFixture]
    public class RawBlockManagerMappedTests : RawBlockManagerTestsBase
    {
        public static readonly string DiskFileBaseName = typeof (RawBlockManagerMappedTests).Name;
        public string DiskFileName;
        public int Count = 0;

        protected override IRawPersistenceSpace InstantiateSpace(uint blockCount, uint blockSize)
        {
            var memoryMappedFile = MemoryMappedFile.CreateFromFile(DiskFileName, FileMode.Open, null, blockCount*blockSize, MemoryMappedFileAccess.ReadWrite);
            return new MemoryMappedSpace(memoryMappedFile);
        }

        protected override void SetUp()
        {
            Directory.CreateDirectory(DiskFileBaseName);
            DiskFileName = Path.Combine(DiskFileBaseName, Count + ".panda");
            Count++;
            using (var fs = File.Create(DiskFileName))
            {
                var buf = new byte[1];
                fs.Write(buf,0,1);
                fs.Flush(true);
            }

            base.SetUp();
        }

        public override void TearDown()
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
            base.TearDown();
        }

        protected void DeleteDisk()
        {
            var count = 0;
            const int limit = 10;
            const int timeout = 200;

            while (File.Exists(DiskFileName))
            {
                try
                {
                    File.Delete(DiskFileName);
                }
                catch (InvalidOperationException)
                {
                    if (count < limit)
                    {
                        Thread.Sleep(timeout);
                        count++;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    if (count < limit)
                    {
                        Thread.Sleep(timeout);
                        count++;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}