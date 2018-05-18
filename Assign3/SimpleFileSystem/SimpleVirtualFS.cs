// SimpleVirtualFS.cs
// Pete Myers and Steven Reeves
// 5/5/2018
//
// NOTE: Implement the methods and classes in this file

using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleFileSystem
{
    // NOTE:  Blocks are used for file data, directory contents are just stored in linked sectors (not blocks)

    public class VirtualFS
    {
        private const int DRIVE_INFO_SECTOR = 0;
        private const int ROOT_DIR_SECTOR = 1;
        private const int ROOT_DATA_SECTOR = 2;

        private Dictionary<string, VirtualDrive> drives;    // mountPoint --> drive
        private VirtualNode rootNode;

        public VirtualFS()
        {
            this.drives = new Dictionary<string, VirtualDrive>();
            this.rootNode = null;
        }

        public void Format(DiskDriver disk)
        {
            // wipe all sectors of disk and create minimum required DRIVE_INFO, DIR_NODE and DATA_SECTOR

            FREE_SECTOR free = new FREE_SECTOR(disk.BytesPerSector);

            for (int i = 0; i < disk.SectorCount; i++)
                disk.WriteSector(i, free.RawBytes);

            // DRIVE_INFO
            DRIVE_INFO drive = new DRIVE_INFO(disk.BytesPerSector, ROOT_DIR_SECTOR);
            disk.WriteSector(DRIVE_INFO_SECTOR, drive.RawBytes);

            // DIR_NODE for root node
            DIR_NODE rootDir = new DIR_NODE(disk.BytesPerSector, ROOT_DATA_SECTOR, FSConstants.PATH_SEPARATOR.ToString(), 0);
            disk.WriteSector(ROOT_DIR_SECTOR, rootDir.RawBytes);

            // DATA_SECTOR for root node
            DATA_SECTOR data = new DATA_SECTOR(disk.BytesPerSector, 0, new byte[] { 0 });
            disk.WriteSector(ROOT_DATA_SECTOR, data.RawBytes);
        }

        public void Mount(DiskDriver disk, string mountPoint)
        {
            // for the first mounted drive, expect mountPoint to be named FSConstants.PATH_SEPARATOR as the root
            if(drives.Count == 0 && mountPoint != FSConstants.PATH_SEPARATOR.ToString())
            {
                throw new Exception("Expected first mounted dist to be at root directory!");
            }

            // read drive info from disk, load root node and connect to mountPoint

            DRIVE_INFO driveInfo = DRIVE_INFO.CreateFromBytes(disk.ReadSector(DRIVE_INFO_SECTOR));
            VirtualDrive drive = new VirtualDrive(disk, DRIVE_INFO_SECTOR, driveInfo);

            DIR_NODE rootSector = DIR_NODE.CreateFromBytes(disk.ReadSector(ROOT_DIR_SECTOR));
            rootNode = new VirtualNode(drive, ROOT_DIR_SECTOR, rootSector, null);

            drives.Add(mountPoint, drive);

        }

        public void Unmount(string mountPoint)
        {
            // look up the drive and remove it's mountPoint
            if (!drives.ContainsKey(mountPoint))
            {
                throw new Exception("No drive mounted at mount point: " + mountPoint);
            }

            // Unset root node if needed
            VirtualDrive drive = drives.Where(x => x.Key == mountPoint).FirstOrDefault().Value;
            if (rootNode.Drive == drive)
                rootNode = null;

            drives.Remove(mountPoint);
        }

        public VirtualNode RootNode => rootNode;
    }

    public class VirtualDrive
    {
        private int bytesPerDataSector;
        private DiskDriver disk;
        private int driveInfoSector;
        private DRIVE_INFO sector;      // caching entire sector for now

        public VirtualDrive(DiskDriver disk, int driveInfoSector, DRIVE_INFO sector)
        {
            this.disk = disk;
            this.driveInfoSector = driveInfoSector;
            this.bytesPerDataSector = DATA_SECTOR.MaxDataLength(disk.BytesPerSector);
            this.sector = sector;
        }

        public int[] GetNextFreeSectors(int count)
        {
            // find count available free sectors on the disk and return their addresses

            int[] result = new int[count];

            int foundIndex = 0;
            for(int address = 0; address < disk.SectorCount && foundIndex < count; address++)
            {
                byte[] raw = disk.ReadSector(address);
                if (SECTOR.GetTypeFromBytes(raw) == SECTOR.SectorType.FREE_SECTOR)
                {
                    result[foundIndex++] = address;
                }
            }

            return result;
        }

        public DiskDriver Disk => disk;
        public int BytesPerDataSector => bytesPerDataSector;
    }

    public class VirtualNode
    {
        private VirtualDrive drive;
        private int nodeSector;
        private NODE sector;                                // caching entire sector for now
        private VirtualNode parent;
        private Dictionary<string, VirtualNode> children;   // child name --> child node
        private List<VirtualBlock> blocks;                  // cache of file blocks

        public VirtualNode(VirtualDrive drive, int nodeSector, NODE sector, VirtualNode parent)
        {
            this.drive = drive;
            this.nodeSector = nodeSector;
            this.sector = sector;
            this.parent = parent;
            this.children = null;                           // initially empty cache
            this.blocks = null;                             // initially empty cache
        }

        public VirtualDrive Drive => drive;
        public string Name => sector.Name;
        public VirtualNode Parent => parent;
        public bool IsDirectory { get { return sector.Type == SECTOR.SectorType.DIR_NODE; } }
        public bool IsFile { get { return sector.Type == SECTOR.SectorType.FILE_NODE; } }
        public int ChildCount => (sector as DIR_NODE).EntryCount;
        public int FileLength => (sector as FILE_NODE).FileSize;

        public void Rename(string newName)
        {
            // rename this node, update parent's children as needed, save new name on disk
            string oldName = Name;

            // Update parents children first
            if (parent.children != null)
            {
                parent.children.Remove(oldName);
                parent.children.Add(newName, this);
            }

            // Rename node on disk
            sector.Name = newName;
            drive.Disk.WriteSector(nodeSector, sector.RawBytes);
        }

        public void Move(VirtualNode destination)
        {
            // remove this node from it's current parent and attach it to it's new parent
            // update the directory information for both parents on disk

            if (!destination.IsDirectory)
                throw new Exception("Destination must be a directory!");

            destination.LoadChildren();
            destination.children.Add(Name, this);
            destination.CommitChildren();

            parent.LoadChildren();
            parent.children.Remove(Name);
            parent.CommitChildren();

            parent = destination;

        }

        public void Delete()
        {
            // make sectors free!
            // wipe this node and sector(s) from the disk
            FREE_SECTOR free = new FREE_SECTOR(drive.Disk.BytesPerSector);

            /*
            int wantsToBeFree = this.nodeSector;
            while(wantsToBeFree != 0)
            {
                byte[] currentBytes = drive.Disk.ReadSector(wantsToBeFree);
                int nextSector = drive.Disk.
                drive.Disk.WriteSector(wantsToBeFree, free.RawBytes);
            }
            */

            // if this is a file, then nuke its node sector and data sectors
            // if directory, nuke just the node sector, and children

            // remove this node from parent directory
            parent.LoadChildren();
            parent.children.Remove(Name);
            parent.CommitChildren();
        }

        private void LoadChildren()
        {
            if (children == null)
            {
                children = new Dictionary<string, VirtualNode>();
                DATA_SECTOR data = DATA_SECTOR.CreateFromBytes(drive.Disk.ReadSector(sector.FirstDataAt));

                for (int i = 0; i < ChildCount; i++)
                {
                    int childAddress = BitConverter.ToInt32(data.DataBytes, i * 4);

                    NODE childSector = NODE.CreateFromBytes(drive.Disk.ReadSector(childAddress));

                    VirtualNode vn = new VirtualNode(drive, childAddress, childSector, this);
                    children.Add(childSector.Name, vn);
                }
            }

        }

        private void CommitChildren()
        {
            if (children != null)
            {
                // Create empty byte array
                byte[] childListBytes = new byte[DATA_SECTOR.MaxDataLength(drive.Disk.BytesPerSector)];

                int i = 0;
                foreach (VirtualNode childNode in children.Values)
                {
                    int childAddress = childNode.nodeSector;
                    BitConverter.GetBytes(childAddress).CopyTo(childListBytes, i * 4);
                    i++;
                }

                DATA_SECTOR data = new DATA_SECTOR(drive.Disk.BytesPerSector, 0, childListBytes);
                drive.Disk.WriteSector(sector.FirstDataAt, data.RawBytes);

                // Update entry count
                (sector as DIR_NODE).EntryCount = children.Count;

                // save the entry count
                drive.Disk.WriteSector(nodeSector, sector.RawBytes);

            }
        }

        public VirtualNode CreateDirectoryNode(string name)
        {
            // Only create children if we're a directory
            if(!IsDirectory)
            {
                throw new Exception("Must be a directory to create children!");            
            }

            // read current list of children
            LoadChildren();

            // allocate a new DIR_NODE and DATA_SECTOR on the disk

            // Find the first two FREE_SECTORs on the disk
            int[] freeSectors = drive.GetNextFreeSectors(2);

            //DIR_NODE
            DIR_NODE dirSector = new DIR_NODE(drive.Disk.BytesPerSector, freeSectors[1], name, 0);
            drive.Disk.WriteSector(freeSectors[0], dirSector.RawBytes);

            //DATA_SECTOR
            DATA_SECTOR dataSector = new DATA_SECTOR(drive.Disk.BytesPerSector, 0, new byte[] { 0 });
            drive.Disk.WriteSector(freeSectors[1], dataSector.RawBytes);

            // Create a new virtual node
            VirtualNode newNode = new VirtualNode(drive, freeSectors[0], dirSector, this);

            // Add it to its parent
            children.Add(name, newNode);

            CommitChildren();

            // Return new node        
            return newNode;
        }

        public VirtualNode CreateFileNode(string name)
        {
            if (!IsDirectory)
            {
                throw new Exception("Must be a directory to create children!");
            }

            // read current list of children
            LoadChildren();

            // Find the first two FREE_SECTORs on the disk
            int[] freeSectors = drive.GetNextFreeSectors(2);

            // allocate a new FILE_NODE and DATA_SECTOR on the disk

            //FILE_NODE
            FILE_NODE fileSector = new FILE_NODE(drive.Disk.BytesPerSector, freeSectors[1], name, 0);
            drive.Disk.WriteSector(freeSectors[0], fileSector.RawBytes);

            //DATA_SECTOR
            DATA_SECTOR dataSector = new DATA_SECTOR(drive.Disk.BytesPerSector, 0, new byte[] { 0 });
            drive.Disk.WriteSector(freeSectors[1], dataSector.RawBytes);

            // Create a new virtual node
            VirtualNode newNode = new VirtualNode(drive, freeSectors[0], fileSector, this);

            // Add it to its parent
            children.Add(name, newNode);

            CommitChildren();

            return newNode;
        }

        public IEnumerable<VirtualNode> GetChildren()
        {
            LoadChildren();
            return children.Values;
        }

        public VirtualNode GetChild(string name)
        {
            LoadChildren();

           return children.Where(x => x.Value.Name == name).FirstOrDefault().Value;
        }

        private void LoadBlocks()
        {
            if (blocks == null)
            {
                blocks = new List<VirtualBlock>();

                // find data sectors
                int dataSectorAddr = sector.FirstDataAt;
                while (dataSectorAddr != 0)
                {
                    DATA_SECTOR dataSector = DATA_SECTOR.CreateFromBytes(drive.Disk.ReadSector(dataSectorAddr));
                    VirtualBlock block = new VirtualBlock(drive, dataSectorAddr, dataSector);
                    blocks.Add(block);

                    // Go on to next data sector
                    dataSectorAddr = dataSector.NextSectorAt;
                }
            }
        }

        private void CommitBlocks()
        {
            // Write dirty blocks to disk
            if (blocks != null)
            {
                foreach (VirtualBlock vb in blocks)
                {
                    vb.CommitBlock();
                }
            }
        }

        public byte[] Read(int index, int length)
        {
            // Make sure this is a file
            if (!IsFile)
                throw new Exception("Must read from a file!");
            // Check for reading past end
            if ((index + length) > FileLength)
                throw new Exception("Can't read beyond end of file! /n");

            // Load the cache of blocks for the file
            LoadBlocks();

            // Write the bytes to the cache
            return VirtualBlock.ReadBlockData(drive, blocks, index, length);

        }

        public void Write(int index, byte[] data)
        {
            // Make sure this is a file
            if (!IsFile)
                throw new Exception("Must write to a file!");

            // Load the cache of blocks for the file
            LoadBlocks();

            // Grow the cached blocks if needed
            int finalLength = Math.Max(FileLength, index + data.Length);

            VirtualBlock.ExtendBlocks(drive, blocks, FileLength, finalLength);

            // Write the bytes to the cache
            VirtualBlock.WriteBlockData(drive, blocks, index, data);

            // Flush the cache of blocks
            CommitBlocks();

            // Adjust file size as needed
            if (finalLength > FileLength)
            {
                (sector as FILE_NODE).FileSize = index + data.Length;
                drive.Disk.WriteSector(nodeSector, sector.RawBytes);
            }

        }
    }

    public class VirtualBlock
    {
        private VirtualDrive drive;
        private DATA_SECTOR sector;
        private int sectorAddress;
        private bool dirty;

        public VirtualBlock(VirtualDrive drive, int sectorAddress, DATA_SECTOR sector, bool dirty = false)
        {
            this.drive = drive;
            this.sector = sector;
            this.sectorAddress = sectorAddress;
            this.dirty = dirty;
        }

        public int SectorAddress => sectorAddress;
        public DATA_SECTOR Sector => sector;
        public bool Dirty => dirty;

        public byte[] Data
        {
            get { return (byte[])sector.DataBytes.Clone(); }
            set
            {
                sector.DataBytes = value;
                dirty = true;
            }
        }

        public void CommitBlock()
        {
            // Write this block's data to disk, if it's dirty
            if(dirty)
            {
                drive.Disk.WriteSector(sectorAddress, sector.RawBytes);
                dirty = false;
            }
        }

        public static byte[] ReadBlockData(VirtualDrive drive, List<VirtualBlock> blocks, int startIndex, int length)
        {
            // Read data from list of blocks
            // Assumes block list contains all data
            // Assumes list of blcoks is long enough

            byte[] result = new byte[length];
            int blockSize = drive.BytesPerDataSector;
            int startBlock = startIndex / blockSize;
            int endBlock = (startIndex + length) / blockSize;
            int toStart = 0;

            // Read data from first block

            VirtualBlock vb = blocks[startBlock];
            byte[] blockData = vb.Data;

            // Copy data from here
            int fromStart = startIndex % blockSize;
            int copyCount = Math.Min(length, blockSize - fromStart);
            CopyBytes(copyCount, blockData, fromStart, result, toStart);

            toStart += copyCount;


            // read data from rest of blocks
            for (int i = startBlock + 1; i <= endBlock; i++)
            {
                vb = blocks[i];
                blockData = vb.Data;

                // Overwrite result data with this block's data
                fromStart = 0;
                copyCount = Math.Min((length - toStart), blockSize);
                CopyBytes(copyCount, blockData, fromStart, result, toStart);

                toStart += copyCount;
            }

            return result;
        }

        public static void WriteBlockData(VirtualDrive drive, List<VirtualBlock> blocks, int startIndex, byte[] data)
        {
            // Write data into the list of blocks, starting at index

            // calculate starting block and ending block
            int blockSize = drive.BytesPerDataSector;
            int startBlock = startIndex / blockSize;
            int endBlock = (startIndex + data.Length) / blockSize;

            // Write data to first block
            int fromStart = 0;

            VirtualBlock vb = blocks[startBlock];
            byte[] blockData = vb.Data;
            // Overwrite old data


            int toStart = startIndex % blockSize;
            int copyCount = Math.Min(data.Length, blockSize - toStart);

            CopyBytes(copyCount, data, fromStart, blockData, toStart);

            // Put new data in
            vb.Data = blockData;

            fromStart += copyCount;


            // Write data to each affected block
            for (int i = startBlock+1; i <= endBlock; i++)
            {
                vb = blocks[i];
                blockData = vb.Data;

                // Overwrite old data

                toStart = 0;
                copyCount = Math.Min((data.Length - fromStart), blockSize - toStart);
                CopyBytes(copyCount, data, fromStart, blockData, toStart);

                // Put new data in
                vb.Data = blockData;

                fromStart += copyCount;
            }   

        }

        public static void ExtendBlocks(VirtualDrive drive, List<VirtualBlock> blocks, int initialFileLength, int finalFileLength)
        {
            // if current number of blocks is too small, then append more blocks as needed
            // allocate free sectors for each new block
            int finalBlockCount = BlocksNeeded(drive, finalFileLength);
            int additionalBlocks = finalBlockCount - blocks.Count;
            if (additionalBlocks > 0)
            {
                int[] freeSectorAddresses = drive.GetNextFreeSectors(additionalBlocks);

                VirtualBlock prevBlock = blocks.Last();
                foreach (int i in freeSectorAddresses)
                {
                    // connect sectors
                    prevBlock.sector.NextSectorAt = i;
                    prevBlock.dirty = true;

                    // create new block
                    DATA_SECTOR dataSector = new DATA_SECTOR(drive.Disk.BytesPerSector, 0, new byte[] { 0 });
                    VirtualBlock newBlock = new VirtualBlock(drive, i, dataSector, true);

                    // add to the end of block list
                    blocks.Add(newBlock);

                    // prepare next block
                    prevBlock = newBlock;
                }
            }

        }

        private static int BlocksNeeded(VirtualDrive drive, int numBytes)
        {
            return Math.Max(1, (int)Math.Ceiling((double)numBytes / drive.BytesPerDataSector));
        }

        private static void CopyBytes(int copyCount, byte[] from, int fromStart, byte[] to, int toStart)
        {
            for (int i = 0; i < copyCount; i++)
            {
                to[toStart + i] = from[fromStart + i];
            }
        }
    }
}
