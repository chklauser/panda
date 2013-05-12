using System;
using System.Linq;
using NUnit.Framework;
using Panda.Core;

namespace Panda.Test.Unit
{
    /// <summary>
    /// This unit test fixture focuses on testing the synchronization mechanism.
    /// Are block updated correctly?
    /// </summary>
    [TestFixture]
    public class DiskSynchronizationTests : DiskSynchronizationBase
    {
        [Test]
        public void JournalCreateDirectory()
        {
            var before = DateTime.Now - TimeSpan.FromSeconds(5);
            Client.Root.CreateDirectory("new");
            var jes = SyncClient.GetJournalEntriesSince(before).ToList();
            Assert.That(jes.Count,Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void JournalCreateDirectoryTwice()
        {
            var before = DateTime.Now - TimeSpan.FromSeconds(5);
            Client.Root.CreateDirectory("new");
            var firstCount = SyncClient.GetJournalEntriesSince(before).Count();
            Client.Root.CreateDirectory("old");
            var secondCount = SyncClient.GetJournalEntriesSince(before).Count();
            Assert.That(secondCount, Is.EqualTo(firstCount+1),"Only one additional block should have changed.");
        }

        [Test]
        public void CreateDirectoryOneWay()
        {
            var before = DateTime.Now - TimeSpan.FromSeconds(5);
            Client.Root.CreateDirectory("new");
            SyncTo(Client,Server,before);

            var dir = Server.Root.Navigate("new");
            Assert.That(dir,Is.Not.Null,"new dir should exist on server.");
            Assert.That(Server.Root.Count<VirtualNode>(),Is.EqualTo(1),"root should only have 1 child");
        }

        [Test]
        public void CreateDirectoryOneWayForward()
        {
            var before = DateTime.Now - TimeSpan.FromSeconds(5);
            Client.Root.CreateDirectory("new");
            SyncTo(Client, Server, before);
            SyncTo(Server,OtherClient,before);

            var dir = OtherClient.Root.Navigate("new");
            Assert.That(dir, Is.Not.Null, "new dir should exist on server.");
            Assert.That(OtherClient.Root.Count<VirtualNode>(), Is.EqualTo(1), "root should only have 1 child");
        }

        [Test]
        public void CreateFileOneWay()
        {
            var before = DateTime.Now - TimeSpan.FromSeconds(5);
            var data = GenerateData();
            Client.Root.CreateFile("new", data);
            
            SyncTo(Client,Server,before);

            var fileNode = Server.Root.Navigate("new");
            Assert.That(fileNode,Is.InstanceOf<VirtualFile>(),"file:new");
            var file = (VirtualFile) fileNode;
            Assert.That(ReadToEnd(file),Is.EqualTo(data),"data in synch'ed file");
        }

        [Test]
        public void CreateFileOneWayForward()
        {
            var before = DateTime.Now - TimeSpan.FromSeconds(5);
            var data = GenerateData();
            Client.Root.CreateFile("new", data);

            SyncTo(Client, Server, before);
            SyncTo(Server,OtherClient,before);

            var fileNode = OtherClient.Root.Navigate("new");
            Assert.That(fileNode, Is.InstanceOf<VirtualFile>(), "file:new");
            var file = (VirtualFile)fileNode;
            Assert.That(ReadToEnd(file), Is.EqualTo(data), "data in synch'ed file");
        }

        [Test]
        public void CreateNestedDirectories()
        {
            var before = DateTime.Now - TimeSpan.FromSeconds(5);
            var newDir = Client.Root.CreateDirectory("new");
            var d1 = newDir.CreateDirectory("d1");
            var d2 = newDir.CreateDirectory("d2");
            var d11 = d1.CreateDirectory("d11");
            d2.CreateDirectory("d21");
            d2.CreateDirectory("d22");
            for (var i = 1; i < 500; i++)
            {
                var d = d11.CreateDirectory("d11" + i);
                d.CreateDirectory("d" + i);
            }
            var dir = Client.Root.Navigate("new/d1/d11/d11450/d450");
            Assert.That(dir, Is.Not.Null, "/new/d1/d11/d11450/d450 exists on client");

            SyncTo(Client, Server, before);

            dir = Server.Root.Navigate("new/d1/d11/d11450/d450");
            Assert.That(dir, Is.Not.Null, "/new/d1/d11/d11450/d450 exists on server");
        }

        [Test]
        public void CreateNestedDirectoriesForwarded()
        {
            var before = DateTime.Now - TimeSpan.FromSeconds(5);
            var newDir = Client.Root.CreateDirectory("new");
            var d1 = newDir.CreateDirectory("d1");
            var d2 = newDir.CreateDirectory("d2");
            var d11 = d1.CreateDirectory("d11");
            d2.CreateDirectory("d21");
            d2.CreateDirectory("d22");
            for (var i = 1; i < 500; i++)
            {
                var d = d11.CreateDirectory("d11" + i);
                d.CreateDirectory("d" + i);
            }

            SyncTo(Client, Server, before);
            SyncTo(Server,OtherClient,before);

            var dir = OtherClient.Root.Navigate("new/d1/d11/d11450/d450");
            Assert.That(dir, Is.Not.Null, "/new/d1/d11/d11450/d450 exists");
        }

        [Test]
        public void Collaborate()
        {
            var before = DateTime.Now - TimeSpan.FromSeconds(5);

            Client.Root.CreateDirectory("new");

            SyncTo(Client,Server,before);
            SyncTo(Server,OtherClient,before);

            var after = DateTime.Now;
            var dirNode = OtherClient.Root.Navigate("new");
            Assert.That(dirNode,Is.InstanceOf<VirtualDirectory>());
            var dir = (VirtualDirectory) dirNode;
            var data = GenerateData();
            dir.CreateFile("hello", data);

            SyncTo(OtherClient,Server,after);
            SyncTo(Server,Client,after);

            var fileNode = Client.Navigate("new/hello");
            Assert.That(fileNode,Is.InstanceOf<VirtualFile>());
            var file = (VirtualFile) fileNode;
            Assert.That(ReadToEnd(file),Is.EqualTo(data),"synch'ed data");
        }

        [Test]
        public void Deletion()
        {
            var before = DateTime.Now - TimeSpan.FromSeconds(5);

            var dir = Client.Root.CreateDirectory("new");
            var data = GenerateData();
            dir.CreateFile("hello", data);
            dir.CreateDirectory("more");

            SyncTo(Client, Server, before);
            SyncTo(Server, OtherClient, before);

            var after = DateTime.Now;
            OtherClient.Navigate("new/more").Delete();
            ((VirtualDirectory)OtherClient.Navigate("new")).CreateDirectory("less");

            SyncTo(OtherClient, Server, after);
            SyncTo(Server, Client, after);

            var fileNode = Client.Navigate("new/hello");
            Assert.That(fileNode, Is.InstanceOf<VirtualFile>(),"new/hello");
            var file = (VirtualFile)fileNode;
            Assert.That(ReadToEnd(file), Is.EqualTo(data), "synch'ed data");
            var dirNode = Client.Root.Navigate("new/less");
            Assert.That(dirNode,Is.InstanceOf<VirtualDirectory>(),"new/less");
            Assert.Throws<PathNotFoundException>(() => Client.Root.Navigate("new/more"),"new/more should not exist");
        }

        [Test]
        public void ReplaceFileWithDir()
        {
            var before = DateTime.Now - TimeSpan.FromSeconds(5);

            var dir = Client.Root.CreateDirectory("new");
            var data = GenerateData();
            dir.CreateFile("more", data);

            SyncTo(Client, Server, before);
            SyncTo(Server, OtherClient, before);

            var after = DateTime.Now;
            OtherClient.Navigate("new/more").Delete();
            ((VirtualDirectory)OtherClient.Navigate("new")).CreateDirectory("more");

            SyncTo(OtherClient, Server, after);
            SyncTo(Server, Client, after);

            var dirNode = Client.Root.Navigate("new/more");
            Assert.That(dirNode, Is.InstanceOf<VirtualDirectory>(), "new/more");
        }
    }
}
