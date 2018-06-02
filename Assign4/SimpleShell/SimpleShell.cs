// Assignment 4
// Pete Myers and Steven Reeves
// OIT, Spring 2018
// Handout

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleFileSystem;

namespace SimpleShell
{
    public class SimpleShell : Shell
    {
        private abstract class Cmd
        {
            private string name;
            private SimpleShell shell;

            public Cmd(string name, SimpleShell shell) { this.name = name; this.shell = shell; }

            public string Name => name;
            public SimpleShell Shell => shell;
            public Session Session => shell.session;
            public Terminal Terminal => shell.session.Terminal;
            public FileSystem FileSystem => shell.session.FileSystem;
            public SecuritySystem SecuritySystem => shell.session.SecuritySystem;

            abstract public void Execute(string[] args);
            virtual public string HelpText { get { return ""; } }
            virtual public void PrintUsage() { Terminal.WriteLine("Help not available for this command"); }
        }

        private Session session;
        private Directory cwd;
        private Dictionary<string, Cmd> cmds;   // name -> Cmd
        private bool running;

        public SimpleShell(Session session)
        {
            this.session = session;
            cwd = null;
            cmds = new Dictionary<string, Cmd>();
            running = false;

            AddCmd(new ExitCmd(this));
            AddCmd(new PwdCmd(this));
            AddCmd(new CdCmd(this));
            AddCmd(new LsCmd(this));
            AddCmd(new CatCmd(this));
            AddCmd(new MkFileCmd(this));
            AddCmd(new HelpCmd(this));
            AddCmd(new CpCmd(this));
            // TODO add more commands
        }

        private void AddCmd(Cmd c) { cmds[c.Name] = c; }

        public void Run(Terminal terminal)
        {
            // NOTE: takes over the current thread, returns only when shell exits
            // expects terminal to already be connected

            // set the initial current working directory
            cwd = session.HomeDirectory;

            // main loop...
            running = true;
            while (running)
            { 
            // print command prompt
            // username:cwd --->
            terminal.Echo = true;
            terminal.Write(session.Username + ":" + cwd.FullPathName + "--->");
            // get command line

            string cmdLine = terminal.ReadLine();
            string[] args = cmdLine.Split(' ');

                if (args.Length >= 1 && args[0].Trim().Length > 0)
                {
                    string cmdname = args[0].Trim();
                    if (cmds.ContainsKey(cmdname))
                    {
                        // identify and execute command
                        Cmd cmd = cmds[cmdname];
                        cmd.Execute(args);
                    }
                    else
                    {
                        terminal.WriteLine("Command not found: " + cmdname);
                    }
                }
            }
        }

        #region commands

        // example command: exit
        private class ExitCmd : Cmd
        {
            public ExitCmd(SimpleShell shell) : base("exit", shell) { }

            public override void Execute(string[] args)
            {
                Terminal.WriteLine("Bye!");
                Shell.running = false;
            }

            override public string HelpText { get { return "Exits shell"; } }

            override public void PrintUsage()
            {
                Terminal.WriteLine("usage: exit");
            }
        }

        private class PwdCmd : Cmd
        {
            public PwdCmd(SimpleShell shell) : base("pwd", shell) { }

            public override void Execute(string[] args)
            {
                Terminal.WriteLine(Shell.cwd.FullPathName);
            }

            override public string HelpText { get { return "Prints current working directory"; } }

            override public void PrintUsage()
            {
                Terminal.WriteLine("usage: pwd");
            }
        }

        private class CatCmd : Cmd
        {
            public CatCmd(SimpleShell shell) : base("cat", shell) { }

            public override void Execute(string[] args)
            {
                // change the current working directory
                // validate cmd line
                if (args.Length >= 2)
                {
                    foreach (string filename in args.Skip(1))
                    {
                        string fullfilename = filename;
                        if (fullfilename[0] != '/')
                        {
                            fullfilename = Shell.cwd.FullPathName;
                            if (fullfilename.Last() != '/')
                                fullfilename += "/";
                            fullfilename += filename;
                        }

                        //get the file
                        File file = (File)FileSystem.Find(fullfilename);

                        if (file != null)
                        {
                            FileStream stream = file.Open();
                            byte[] content = stream.Read(0, file.Length);
                            stream.Close();
                            string contentstring = ASCIIEncoding.ASCII.GetString(content);
                            Terminal.WriteLine(contentstring);
                        }
                    }
                }
                else
                {
                    Terminal.WriteLine("Error: missing file name!");
                    PrintUsage();
                }
            }

            override public string HelpText { get { return "Prints contents of files"; } }

            override public void PrintUsage()
            {
                Terminal.WriteLine("usage: cat <file>..");
                Terminal.WriteLine("          <file> prints contents of named text file");
            }
        }

        private class CdCmd : Cmd
        {
            public CdCmd(SimpleShell shell) : base("cd", shell) { }

            public override void Execute(string[] args)
            {
                // change the current working directory
                // validate cmd line
                if (args.Length == 2)
                {
                    string path = args[1];
                    // Handle ..
                    if (path == "..")
                    {
                        if(Shell.cwd.Parent != null)
                            Shell.cwd = Shell.cwd.Parent;
                    }
                    else
                    { 

                        if (path[0] != '/')
                        {
                            path = Shell.cwd.FullPathName;
                            if (path.Last() != '/')
                                path += "/";
                            path += args[1];
                        }

                        Directory newcwd = (Directory)FileSystem.Find(path);
                        // validate directory
                        if (newcwd != null)
                        {
                            Shell.cwd = newcwd;
                        }
                        else
                        {
                            Terminal.WriteLine("Error: Directory not found!");
                        }
                    }
                }
                else
                {
                    Terminal.WriteLine("Error: missing path name!");
                    PrintUsage();
                }
            }

            override public string HelpText { get { return "Changes the current working directory"; } }

            override public void PrintUsage()
            {
                Terminal.WriteLine("usage: cd <path>|..");
                Terminal.WriteLine("          <path> can be full or partial");
            }
        }

        private class LsCmd : Cmd
        {
            public LsCmd(SimpleShell shell) : base("ls", shell) { }

            public override void Execute(string[] args)
            {
                foreach (Directory subdir in Shell.cwd.GetSubDirectories())
                {
                    Terminal.WriteLine(subdir.Name + "/");
                }
                foreach (File subfile in Shell.cwd.GetFiles())
                {
                    Terminal.WriteLine(subfile.Name);
                }
            }

            override public string HelpText { get { return "Prints contents of current working directory"; } }

            override public void PrintUsage()
            {
                Terminal.WriteLine("usage: ls");
            }
        }

        private class MkFileCmd : Cmd
        {
            public MkFileCmd(SimpleShell shell) : base("mkfile", shell) { }

            public override void Execute(string[] args)
            {
                // change the current working directory
                // validate cmd line
                if (args.Length >= 3)
                {
                    // get file name
                    string filename = args[1];

                    //create the file
                    File file = Shell.cwd.CreateFile(filename);

                    if (file != null)
                    {
                        //open filestream
                        FileStream stream = file.Open();

                        //Write other cmd line as text
                        int index = 0;
                        foreach (string s in args.Skip(2))
                        {
                            string contentstring = s + " ";
                            byte[] content = ASCIIEncoding.ASCII.GetBytes(contentstring);
                            stream.Write(index, content);
                            index += content.Length;
                        }
                        stream.Close();
                    }

                }
                else
                {
                    Terminal.WriteLine("Error: Cannot create file!");
                    PrintUsage();
                }
            }

            override public string HelpText { get { return "Creates new text file"; } }

            override public void PrintUsage()
            {
                Terminal.WriteLine("usage: mkfile <file> <contents>");
            }
        }

        private class HelpCmd : Cmd
        {
            public HelpCmd(SimpleShell shell) : base("help", shell) { }

            public override void Execute(string[] args)
            {
                if (args.Length == 1)
                {
                    foreach (string cmdname in Shell.cmds.Keys)
                    {
                        Terminal.WriteLine("  " + cmdname + "--" + Shell.cmds[cmdname].HelpText);
                    }
                }
                else
                {
                    string cmdname = args[1];
                    Terminal.WriteLine(Shell.cmds[cmdname].HelpText);
                    Shell.cmds[cmdname].PrintUsage();
                }

            }

            override public string HelpText { get { return "Help for available commands"; } }

            override public void PrintUsage()
            {
                Terminal.WriteLine("usage: help [cmd]");
                Terminal.WriteLine("            [cmd] prints usage statement");
            }
        }

        private class CpCmd : Cmd
        {
            public CpCmd(SimpleShell shell) : base("cp", shell) { }

            public override void Execute(string[] args)
            {
                // TODO finish this, end of lab 6/2
                /*
                // change the current working directory
                // validate cmd line
                if (args.Length == 3)
                {
                    // get file name
                    string filename = args[1];

                    // get directory name
                    string dirname = args[2];

                    // Read contents of file


                    //create the file
                    File file = Shell.cwd.CreateFile(filename);
                    if (file != null)
                    {
                        //open filestream
                        FileStream stream = file.Open();

                        //Write other cmd line as text
                        int index = 0;
                        foreach (string s in args.Skip(2))
                        {
                            string contentstring = s + " ";
                            byte[] content = ASCIIEncoding.ASCII.GetBytes(contentstring);
                            stream.Write(index, content);
                            index += content.Length;
                        }
                        stream.Close();
                    }

                }
                else
                {
                    Terminal.WriteLine("Error: Need 3 arugments");
                    PrintUsage();
                }
                */
            }

            override public string HelpText { get { return "Copies file to new directory"; } }

            override public void PrintUsage()
            {
                Terminal.WriteLine("usage: cp <file> <dir>");
                Terminal.WriteLine("          <file> name from current directory");
                Terminal.WriteLine("          <dir> desitnation to be copied to");
            }
        }
        // TODO  more commands here

        #endregion
    }
}
