using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Panda.Test.Unit
{
    public abstract class DiskSynchronizationBase
    {
        public readonly string DiskFileBaseName;
        public static int Count = 0;
        public VirtualDisk Server;
        public VirtualDisk Client;
        public VirtualDisk OtherClient;
        public ISynchronizingDisk SyncClient;
        public ISynchronizingDisk SyncOtherClient;
        public ISynchronizingDisk SyncServer;
        private List<VirtualDisk> _managedDisks;
        public List<string> DiskNames;
        public uint Capacity = 10*1024*1024;

        public DiskSynchronizationBase()
        {
            DiskFileBaseName = GetType().Name;
        }

        [SetUp]
        public virtual void SetUp()
        {
            Directory.CreateDirectory(DiskFileBaseName);

            var clientDiskName = Path.Combine(DiskFileBaseName, Count + ".client.panda");
            var otherClientDiskName = Path.Combine(DiskFileBaseName, Count + ".client2.panda");
            var serverDiskName = Path.Combine(DiskFileBaseName, Count + ".server.panda");

            if (File.Exists(clientDiskName))
                File.Delete(clientDiskName);
            if (File.Exists(otherClientDiskName))
                File.Delete(otherClientDiskName);
            if(File.Exists(serverDiskName))
                File.Delete(serverDiskName);

            Client = VirtualDisk.CreateNew(clientDiskName,Capacity);
            OtherClient = VirtualDisk.CreateNew(otherClientDiskName, Capacity);
            Server = VirtualDisk.CreateNew(serverDiskName, Capacity);

            SyncClient = (ISynchronizingDisk) Client;
            SyncServer = (ISynchronizingDisk) Server;
            SyncOtherClient = (ISynchronizingDisk) OtherClient;

            SyncClient.Associate(serverDiskName);
            SyncOtherClient.Associate(serverDiskName);

            DiskNames = new List<string> {clientDiskName, otherClientDiskName,serverDiskName};

            _managedDisks = new List<VirtualDisk> {Client,OtherClient,Server};

            Count++;
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (Client != null)
            {
                Client.Dispose();
            }

            if (Server != null)
            {
                Server.Dispose();
            }

            if (_managedDisks != null)
            {
                foreach (var managedDisk in _managedDisks)
                {
                    if (managedDisk != Client && managedDisk != Server)
                    {
                        managedDisk.Dispose();
                    }
                }
            }

            Client = Server = null;
            _managedDisks = null;

            foreach (var diskName in DiskNames)
            {
                try
                {
                
                    if (File.Exists(diskName))
                    {
                        File.Delete(diskName);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                
            }
        }

        protected void SyncTo(VirtualDisk source, VirtualDisk destination, DateTime since)
        {
            Assert.That(source, Is.InstanceOf<ISynchronizingDisk>(), "source");
            Assert.That(destination, Is.InstanceOf<ISynchronizingDisk>(), "destination");

            var syncSource = (ISynchronizingDisk)source;
            var syncDestination = (ISynchronizingDisk)destination;

            Assert.That(syncSource.BlockSize, Is.EqualTo(syncDestination.BlockSize), "block sizes source should match destination");

            var buffer = new byte[syncSource.BlockSize];
            foreach (var offset in syncSource.GetJournalEntriesSince(since).Select(x => x.BlockOffset))
            {
                syncSource.DirectRead(offset, buffer, 0);
                syncDestination.ReceiveChanges(offset, buffer);
            }

            syncSource.NotifySynchronized();
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
                TearDown();
            }
        }

        ~DiskSynchronizationBase()
        {
            Dispose(false);
        }
    }
}