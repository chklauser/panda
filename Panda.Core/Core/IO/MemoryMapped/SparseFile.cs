﻿/****************************** Module Header ******************************\
* Module Name:  SparseFile.cs
* Project:      CSSparseFile
* Copyright (c) Microsoft Corporation.
* 
* SparseFile encapsulates the common operations on sparse files.
* 
* This source is subject to the Microsoft Public License.
* See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.
* All other rights reserved.
* 
* History:
* * 7/5/2009 11:39 AM Jialiang Ge Created
\***************************************************************************/

#region Using directives

using System;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Threading;
using System.Runtime.InteropServices;

#endregion

namespace Panda.Core.IO.MemoryMapped
{
    public static class SparseFile
    {
        /// <summary>
        /// VolumeSupportsSparseFiles determines if the volume supports sparse 
        /// streams.
        /// </summary>
        /// <param name="rootPathName">Volume root path e.g. C:\</param>
        /// <returns>True if the volumne supports sparse files; false otherwise.</returns>
        public static bool VolumeSupportsSparseFiles(string rootPathName)
        {
            if (rootPathName == null)
                throw new ArgumentNullException("rootPathName");
            if(!Path.IsPathRooted(rootPathName))
                throw new ArgumentException("Path must be a root path name.");

            // Get volume information
            FileSystemFeatures fileSystemFlags;
            uint volumeSerialNumber;
            uint maxComponentLength;
            NativeMethods.GetVolumeInformation(rootPathName, null, 
                NativeMethods.MAX_PATH, out volumeSerialNumber, 
                out maxComponentLength, out fileSystemFlags, null,
                NativeMethods.MAX_PATH);

            return ((fileSystemFlags & FileSystemFeatures.SupportsSparseFiles) == 
                FileSystemFeatures.SupportsSparseFiles);
        }

        /// <summary>
        /// Determine if a file is sparse.
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns></returns>
        public static bool IsSparseFile(string fileName)
        {
            // Get file information
            FileInfo fileInfo = new FileInfo(fileName);
        
            // Check for sparse file
            return (fileInfo.Attributes & FileAttributes.SparseFile) == 
                FileAttributes.SparseFile;
        }

        /// <summary>
        /// Get sparse file sizes.
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns></returns>
        /// <see cref="http://msdn.microsoft.com/en-us/library/aa365276.aspx"/>
        public static ulong GetSparseFileSize(string fileName)
        {
            // Retrieves the file's actual size on disk, in bytes. The size 
            // does not include the sparse ranges.
            uint high;
            uint low = NativeMethods.GetCompressedFileSize(fileName, out high);
            var sparseFileCompressedSize = ((ulong)high << 32) + low;

            // Print the result
            return sparseFileCompressedSize;
        }

        /// <summary>
        /// Create a sparse file.
        /// </summary>
        /// <param name="hFileHandle">Handle of the file to convert to a sparse file.</param>
        /// <returns></returns>
        public static void Convert(SafeFileHandle hFileHandle)
        {        
            // Use the DeviceIoControl function with the FSCTL_SET_SPARSE control 
            // code to mark the file as sparse. If you don't mark the file as 
            // sparse, the FSCTL_SET_ZERO_DATA control code will actually write 
            // zero bytes to the file instead of marking the region as sparse 
            // zero area.
            int bytesReturned = 0;
            var lpOverlapped = new NativeOverlapped();
            NativeMethods.DeviceIoControl(hFileHandle, 
                EIoControlCodes.FsctlSetSparse, IntPtr.Zero, 0, IntPtr.Zero, 0, 
                ref bytesReturned, ref lpOverlapped);
        }

        /// <summary>
        /// Converting a file region to A sparse zero area.
        /// </summary>
        /// <param name="hSparseFile">Safe handle of the sparse file</param>
        /// <param name="start">Start address of the sparse zero area</param>
        /// <param name="size">
        /// Size of the sparse zero block. The minimum sparse size is 64KB.
        /// </param>
        /// <remarks>
        /// Note that SetSparseRange does not perform actual file I/O, and unlike 
        /// the WriteFile function, it does not move the current file I/O pointer 
        /// or sets the end-of-file pointer. That is, if you want to place a 
        /// sparse zero block in the end of the file, you must move the file 
        /// pointer accordingly using the FileStream.Seek function, otherwise 
        /// DeviceIoControl will have no effect. 
        /// </remarks>
        public static void SetSparseRange(SafeFileHandle hSparseFile, uint start, 
                                          uint size)
        {
            // Specify the starting and the ending address (not the size) of the 
            // sparse zero block
            FILE_ZERO_DATA_INFORMATION fzdi;
            fzdi.FileOffset = start;
            fzdi.BeyondFinalZero = start + size;
            GCHandle hfzdi = GCHandle.Alloc(fzdi, GCHandleType.Pinned);

            // Mark the range as sparse zero block
            int bytesReturned = 0;
            NativeOverlapped lpOverlapped = new NativeOverlapped();
            NativeMethods.DeviceIoControl(hSparseFile, 
                EIoControlCodes.FsctlSetZeroData, hfzdi.AddrOfPinnedObject(), 
                Marshal.SizeOf(fzdi), IntPtr.Zero, 0, ref bytesReturned, 
                ref lpOverlapped);

            hfzdi.Free();
        }

        /// <summary>
        /// Query the sparse file layout.
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns></returns>
        public static bool GetSparseRanges(string fileName)
        {
            // Open the file for read
            using (FileStream fs = new FileStream(fileName, FileMode.Open, 
                FileAccess.Read))
            {
                // Set the range to be examined (the whole file)
                FILE_ALLOCATED_RANGE_BUFFER queryRange;
                queryRange.FileOffset = 0;
                queryRange.Length = fs.Length;
                GCHandle hQueryRange = GCHandle.Alloc(queryRange, GCHandleType.Pinned);

                // Allocated areas info
                // DeviceIoControl will return as many results as fit into this 
                // buffer and will report error code ERROR_MORE_DATA as long as 
                // more data is available
                FILE_ALLOCATED_RANGE_BUFFER[] allocRanges = new 
                    FILE_ALLOCATED_RANGE_BUFFER[1024];
                GCHandle hAllocRanges = GCHandle.Alloc(allocRanges, GCHandleType.Pinned);

                int nbytes = 0;
                bool bFinished = false;
                Console.WriteLine("\nAllocated ranges in the file:");
                do
                {
                    NativeOverlapped lpOverlapped = new NativeOverlapped();
                    bFinished = NativeMethods.DeviceIoControl(fs.SafeFileHandle,
                        EIoControlCodes.FsctlQueryAllocatedRanges,
                        hQueryRange.AddrOfPinnedObject(),
                        Marshal.SizeOf(queryRange),
                        hAllocRanges.AddrOfPinnedObject(),
                        Marshal.SizeOf(typeof(FILE_ALLOCATED_RANGE_BUFFER)) * 1024,
                        ref nbytes, ref lpOverlapped);

                    if (!bFinished)
                    {
                        int error = Marshal.GetLastWin32Error();

                        // ERROR_MORE_DATA is the only error that is normal
                        if (error != NativeMethods.ERROR_MORE_DATA)
                        {
                            Console.WriteLine("DeviceIoControl failed w/err 0x{0:X}", 
                                error);
                            return false;
                        }
                    }

                    // Calculate the number of records returned
                    int allocRangeCount = nbytes / 
                        Marshal.SizeOf(typeof(FILE_ALLOCATED_RANGE_BUFFER));

                    // Print each allocated range
                    for (int i = 0; i < allocRangeCount; i++)
                    {
                        Console.WriteLine("allocated range: {0} {1}",
                            allocRanges[i].FileOffset, allocRanges[i].Length);
                    }

                    // Set starting address and size for the next query
                    if (!bFinished && allocRangeCount > 0)
                    {
                        queryRange.FileOffset =
                            allocRanges[allocRangeCount - 1].FileOffset +
                                allocRanges[allocRangeCount - 1].Length;

                        queryRange.Length = fs.Length - queryRange.FileOffset;
                    }

                } while (!bFinished);

                // Release the pinned GC handles
                hAllocRanges.Free();
                hQueryRange.Free();
            }

            return true;
        }
    }
}