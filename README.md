Panda Virtual File System
=========================
Course project for Java and C# in depth 2013

Overview
-------------------------
Panda is a virtual file system that allows the user
to have one or more virtual disks with its own 
directory tree and files.

Panda consists of a library module, a graphical browser
 and a disk synchronization server.

The library module (the "core") handles disk file input/output and lets the user 
 navigate through the file system's directory tree and 
 move files and folders to and from other file systems.

Core Architecture (Panda and Panda.Core)
-------------------------
The core library consists of three layers: the **file system API**, 
the **block API** and the **IO layer**.

The **file system API** is what clients of the Panda library use.
It models the virtual disk and the file system on that disk
on a logical level. The main prupose of this API is to hide
the complexity of the underlying file system implementation, essentially a facade pattern.

The implementation of the file system API is based on the
**block API**, an abstract view of blocks ("clusters","nodes") on the virtual
disk. Strictly separating the IO layer from the block API allows
the creation of mock and debugging implementations of the 
block storage for testing. 

The **IO layer**, finally, is used to implement the block API
and handles the actual storage and retrieval of data on
the host file system.

Acknowledgements
----------------------------------------
*Panda application icon* by http://www.visualpharm.com/ licensed as Linkware
