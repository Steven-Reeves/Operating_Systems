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
                disk.WriteSector(0, free.RawBytes);

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

            // TODO: VirtualFS.Unmount()
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

        public void Rename(string name)
        {
            // rename this node, update parent as needed, save new name on disk
            // TODO: VirtualNode.Rename()
        }

        public void Move(VirtualNode destination)
        {
            // remove this node from it's current parent and attach it to it's new parent
            // update the directory information for both parents on disk
            // TODO: VirtualNode.Move()
        }

        public void Delete()
        {
            // make sectors free!
            // wipe data for this node from the disk
            // wipe this node from parent directory from the disk
            // remove this node from it's parent node

            // TODO: VirtualNode.Delete()
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

            // Create new child node as a directory

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

            // Increment child count
            (sector as DIR_NODE).EntryCount++;

            CommitChildren();

            // Return new node        
            return newNode;
        }

        public VirtualNode CreateFileNode(string name)
        {
            // TODO: VirtualNode.CreateFileNode()
            return null;
        }

        public IEnumerable<VirtualNode> GetChildren()
        {
            LoadChildren();
            return children.Values;
        }

        public VirtualNode GetChild(string name)
        {
            // TODO: VirtualNode.GetChild()

            return null;
        }

        private void LoadBlocks()
        {
            // TODO: VirtualNode.LoadBlocks()
        }

        private void CommitBlocks()
        {
            // TODO: VirtualNode.CommitBlocks()
        }

        public byte[] Read(int index, int length)
        {
            // TODO: VirtualNode.Read()
            return null;
        }

        public void Write(int index, byte[] data)
        {
            // TODO: VirtualNode.Write()
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
            // TODO: VirtualBlock.CommitBlock()
        }

        public static byte[] ReadBlockData(VirtualDrive drive, List<VirtualBlock> blocks, int startIndex, int length)
        {
            // TODO: VirtualBlock.ReadBlockData()
            return null;
        }

        public static void WriteBlockData(VirtualDrive drive, List<VirtualBlock> blocks, int startIndex, byte[] data)
        {
            // TODO: VirtualBlock.WriteBlockData()
        }

        public static void ExtendBlocks(VirtualDrive drive, List<VirtualBlock> blocks, int initialFileLength, int finalFileLength)
        {
            // TODO: VirtualBlock.ExtendBlocks()
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
