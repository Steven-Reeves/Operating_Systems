// Assignment 4
// Pete Myers and Steven Reeves
// OIT, Spring 2018
// Handout

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleFileSystem;

namespace SimpleShell
{
    public class SimpleSessionManager : SessionManager
    {
        private class SimpleSession : Session
        {
            private int userID;
            private SecuritySystem security;
            private FileSystem filesystem;
            private ShellFactory shells;
            private Directory homeDir;
            private Shell shell;
            private Terminal terminal;

            public SimpleSession(SecuritySystem security, FileSystem filesystem, ShellFactory shells, Terminal terminal, int userID)
            {
                this.security = security;
                this.filesystem = filesystem;
                this.shells = shells;
                this.terminal = terminal;
                this.userID = userID;


                // get user's home directory
                homeDir = (Directory)filesystem.Find(security.UserHomeDirectory(userID));

                // identify user's shell
                shell = shells.CreateShell(security.UserPreferredShell(UserID), this);
            }

            public int UserID => userID;
            public string Username => security.UserName(userID);
            public Terminal Terminal => terminal;
            public Shell Shell => shell;
            public Directory HomeDirectory => homeDir;
            public FileSystem FileSystem => filesystem;
            public SecuritySystem SecuritySystem => security;

            public void Run()
            {
                shell.Run(terminal);
            }

            public void Logout()
            {
                // TODO
            }
        }

        private SecuritySystem security;
        private FileSystem filesystem;
        private ShellFactory shells;

        public SimpleSessionManager(SecuritySystem security, FileSystem filesystem, ShellFactory shells)
        {
            this.security = security;
            this.filesystem = filesystem;
            this.shells = shells;
        }

        public Session NewSession(Terminal terminal)
        {
            // ask the user to login
                int attempts = 0;
                do
                {
                    try
                    {
                        // prompt for user name
                        terminal.Echo = true;
                        terminal.Write("Hey, you! Log in. Username: ");
                        string username = terminal.ReadLine();

                        // determine if the user needs to set their password
                        if (security.NeedsPassword(username))
                        {
                            // give them 3 tries
                            SetNewPassword(username, terminal,3);
                            // return them to the login prompt
                            terminal.Write("Username: ");
                            username = terminal.ReadLine();
                        }

                        // prompt for password
                        // TODO: password prompt on same line as username
                        terminal.Echo = false;
                        terminal.Write("Enter Password: ");
                        string password = terminal.ReadLine();
                        terminal.WriteLine("");

                        // authenticate user
                        int UserID = security.Authenticate(username, password);

                        // create a new session and return it
                        SimpleSession session = new SimpleSession(security, filesystem, shells, terminal, UserID);
                        return session;
                    }
                    catch (Exception ex)
                    {
                        terminal.WriteLine("NOT authenticated");
                        attempts++;
                    }
                }
                while (attempts < 3);
                // user failed authentication too many times
                terminal.WriteLine("Too many attempts! You're out.");
            return null;
        }

        private void SetNewPassword(string username, Terminal terminal, int maxTries)
        {
            int tries = 0;
            do
            {
                try
                {
                    // prompt for new password
                    terminal.Echo = false;
                    terminal.Write("Enter New Password: ");
                    string newPassword = terminal.ReadLine();

                    security.SetPassword(username, newPassword);
                    return;
                }
                catch (Exception ex1)
                {           
                    terminal.WriteLine("Invalid password: " + ex1.Message);
                    tries++;
                }
            }
            while (tries < maxTries);

            throw new Exception("Failed to set new password!");
        }
    }
}
