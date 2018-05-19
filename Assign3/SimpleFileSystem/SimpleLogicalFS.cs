// SimpleFS.cs
// Pete Myers and Steven Reeves
// 5/17/2018

// NOTE: Implement the methods and classes in this file

using System;
using System.Collections.Generic;
using System.Linq;


namespace SimpleFileSystem
{
    public class SimpleFS : FileSystem
    {
        #region filesystem

        //
        // File System
        //

        private const char PATH_SEPARATOR = FSConstants.PATH_SEPARATOR;
        private const int MAX_FILE_NAME = FSConstants.MAX_FILENAME;
        private const int BLOCK_SIZE = 500;     // 500 bytes... 2 sectors of 256 bytes each (minus sector overhead)

        private VirtualFS virtualFileSystem;

        public SimpleFS()
        {
            virtualFileSystem = new VirtualFS();
        }

        public void Mount(DiskDriver disk, string mountPoint)
        {
            virtualFileSystem.Mount(disk, mountPoint);
        }

        public void Unmount(string mountPoint)
        {
            virtualFileSystem.Unmount(mountPoint);
        }

        public void Format(DiskDriver disk)
        {
            virtualFileSystem.Format(disk);
        }

        public Directory GetRootDirectory()
        {
            return new SimpleDirectory(virtualFileSystem.RootNode);
        }

        public FSEntry Find(string path)
        {
            // good:  /foo/bar, /foo/bar/
            // bad:  foo, foo/bar, //foo/bar, /foo//bar, /foo/../foo/bar

            // Make sure path starts correctly
            if(path.Length <= 0 || path[0] != PATH_SEPARATOR)
            {
                //Console.WriteLine("invalid path");
                return null;
            }
            if (path.Length == 1)
                return new SimpleDirectory(virtualFileSystem.RootNode);
            try
            {
                string[] elements = path.Split(PATH_SEPARATOR);
                VirtualNode currentNode = virtualFileSystem.RootNode;

                for (int i = 1; i < elements.Length; i++)
                {
                    if (currentNode.IsFile)
                    {
                        //Console.WriteLine("File, not directory");
                        return null;
                    }
                    else
                    {
                        if(elements[i].Length != 0)
                        {
                            // Check if valid child
                            currentNode = currentNode.GetChild(elements[i]);
                            if (currentNode == null)
                            {
                                //Console.WriteLine("can't find child...");
                                return null;
                            }
                        }
                        else
                        {
                            // Allow trailing path separator
                            if (i < elements.Length -1)
                            {
                                //Console.WriteLine("empty path element!");
                                return null;
                            }
                        } 
                    }
                }

                return currentNode.IsDirectory ? (FSEntry) new SimpleDirectory(currentNode) : new SimpleFile(currentNode);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Caught exception in simplefs.find(): " + ex.Message);
            }

            return null;
        }

        public char PathSeparator { get { return PATH_SEPARATOR; } }
        public int MaxNameLength { get { return MAX_FILE_NAME; } }

        #endregion

        #region implementation

        //
        // FSEntry
        //

        abstract private class SimpleEntry : FSEntry
        {
            protected VirtualNode node;

            protected SimpleEntry(VirtualNode node)
            {
                this.node = node;
            }

            public string Name => node.Name;
            public Directory Parent => node.Parent == null ? null : new SimpleDirectory(node.Parent);

            public string FullPathName
            {
                get
                {
                    string fullPath = node.Name;
                    VirtualNode parent = node.Parent;
                    while (parent != null)
                    {
                        if (parent.Name != "/")
                            fullPath = PATH_SEPARATOR + fullPath;
                        fullPath = parent.Name + fullPath;
                        parent = parent.Parent;
                    }
                    return fullPath;
                }
             }

            // override in derived classes
            public virtual bool IsDirectory => node.IsDirectory;
            public virtual bool IsFile => node.IsFile;

            public void Rename(string name)
            {
                node.Rename(name);
            }

            public void Move(Directory destination)
            {
                node.Move((destination as SimpleDirectory).node);
            }

            public void Delete()
            {
                node.Delete();
            }
        }

        //
        // Directory
        //

        private class SimpleDirectory : SimpleEntry, Directory
        {
            public SimpleDirectory(VirtualNode node) : base(node)
            {
            }

            public IEnumerable<Directory> GetSubDirectories()
            {
                // get all directory children
                List<Directory> subdirs = new List<Directory>();
                foreach (VirtualNode child in node.GetChildren())
                {
                    if (child.IsDirectory)
                        subdirs.Add(new SimpleDirectory(child));
                }

                return subdirs;
            }

            public IEnumerable<File> GetFiles()
            {
                List<File> subfiles = new List<File>();
                foreach (VirtualNode child in node.GetChildren())
                {
                    if (child.IsFile)
                        subfiles.Add(new SimpleFile(child));
                }

                return subfiles;
            }

            public Directory CreateDirectory(string name)
            {
                return new SimpleDirectory(node.CreateDirectoryNode(name));
            }

            public File CreateFile(string name)
            {
                return new SimpleFile(node.CreateFileNode(name));
            }
        }

        //
        // File
        //

        private class SimpleFile : SimpleEntry, File
        {
            public SimpleFile(VirtualNode node) : base(node)
            {
            }

            public int Length => node.FileLength;

            public FileStream Open()
            {
                return new SimpleStream(node);
            }

        }

        //
        // FileStream
        //

        private class SimpleStream : FileStream
        {
            private VirtualNode node;

            public SimpleStream(VirtualNode node)
            {
                this.node = node;
            }

            public void Close()
            {
                // clean up resources and buffers
            }

            public byte[] Read(int index, int length)
            {
                return node.Read(index, length);
            }

            public void Write(int index, byte[] data)
            {
                node.Write(index, data);
            }
        }

        #endregion
    }
}
