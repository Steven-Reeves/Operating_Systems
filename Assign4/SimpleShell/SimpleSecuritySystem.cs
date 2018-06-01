// Assignment 4
// Pete Myers and Steven Reeves
// OIT, Spring 2018
// Handout

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using SimpleFileSystem;


namespace SimpleShell
{
    public class SimpleSecurity : SecuritySystem
    {
        private class User
        {
            public int userID;
            public string userName;
            public string password;
            public string homeDirectory;
            public string shell;
        }

        private int nextUserID;
        private Dictionary<int, User> usersById;        // userID -> User

        private FileSystem filesystem;
        private string passwordFileName;
        
        public SimpleSecurity()
        {
            nextUserID = 1;
            usersById = new Dictionary<int, User>();
        }

        public SimpleSecurity(FileSystem filesystem, string passwordFileName)
        {
            nextUserID = 1;
            usersById = new Dictionary<int, User>();
            this.filesystem = filesystem;
            this.passwordFileName = passwordFileName;

            LoadPasswordFile();
        }

        private void LoadPasswordFile()
        {
            // Read all users from the password file

            // open if it exists
            Directory root = filesystem.GetRootDirectory();
            FSEntry entry = filesystem.Find("/" + passwordFileName);
            if (entry != null)
            {
                // Read contents
                File pwfile = entry as File;
                FileStream stream = pwfile.Open();
                byte[] bytes = stream.Read(0, pwfile.Length);
                stream.Close();

                // convert to string
                string data = ASCIIEncoding.ASCII.GetString(bytes);

                // parse based on format below
                // userID;username;password;homedir;shell
                string[] rows = data.Split('\n');
                foreach(string row in rows)
                {
                    if (row.Length > 0)
                    {
                        string[] fields = row.Split(';');
                        if (fields.Length == 5)
                        {
                            User user = new User();
                            user.userID = Convert.ToInt32(fields[0]);
                            user.userName = fields[1];
                            user.password = fields[2];
                            user.homeDirectory = fields[3];
                            user.shell = fields[4];

                            usersById[user.userID] = user;

                            // Update UserID
                            if (user.userID >= nextUserID)
                                nextUserID = user.userID + 1;
                        }
                        else
                        {
                            throw new Exception("found malformed row in password file: " + row);
                        }
                    }
                }
                // TODO: check this logic
            }
        }

        private void SavePasswordFile()
        {
            // Save all users to the password file

            // format the data as such
            // userID;username;password;homedir;shell
            string data = "";
            foreach (User u in usersById.Values)
            {
                data += u.userID.ToString() + ";";
                data += u.userName + ";";
                data += u.password + ";";
                data += u.homeDirectory + ";";
                data += u.shell + "\n";
            }


            // Write to file
            Directory root = filesystem.GetRootDirectory();
            File pwfile;
            FSEntry entry = filesystem.Find("/" + passwordFileName);
            if(entry == null)
            {
                pwfile = root.CreateFile(passwordFileName);
            }
            else
            {
                pwfile = entry as File;
            }
            FileStream stream = pwfile.Open();
            stream.Write(0, ASCIIEncoding.ASCII.GetBytes(data));
            stream.Close();
        }

        private User UserByName(string username)
        {
            return usersById.Values.FirstOrDefault(u => u.userName == username);
        }

        public int AddUser(string username)
        {
            if (usersById.Count(u => u.Value.userName == username) != 0)
                throw new Exception("User: " + username + " already exists!");

            // create a new user with default home directory and shell
            // initially empty password
            User user = new User();
            user.userID = nextUserID++;
            user.userName = username;
            user.password = null;
            user.shell = "pshell";
            user.homeDirectory = "/home/" + username;
            usersById[user.userID] = user;

            // create user's home directory if needed
            if(filesystem != null)
            {
                Directory root = filesystem.GetRootDirectory();
                FSEntry entry = filesystem.Find(user.homeDirectory);
                if (entry == null)
                {
                    Directory home;
                    entry = filesystem.Find("/home");
                    if (entry == null)
                    {
                        home = root.CreateDirectory("home");
                    }
                    else
                    {
                        home = entry as Directory;
                    }
                    root.CreateDirectory(user.userName);
                }
            }

            // save the user to the password file
            SavePasswordFile();

            // return user id
            return user.userID;
        }

        public int UserID(string username)
        {
            // lookup user by username and return user id
            User u = UserByName(username);
            if (u == null)
                throw new Exception("User: " + username + " not found!");
            return u.userID;
        }

        public bool NeedsPassword(string username)
        {
            // return true if user needs a password set
            User u = UserByName(username);
            if (u == null)
                throw new Exception("User: " + username + " not found!");

            return string.IsNullOrEmpty(u.password);
        }

        public void SetPassword(string username, string password)
        {
            User u = UserByName(username);
            if (u == null)
                throw new Exception("User: " + username + " not found!");

            // validate it meets any rules
            // >= 8 characters
            if(password.Length < 8)
                throw new Exception("Password must be at least 8 characters long!");

            // set user's password
            u.password = password;

            // save it to the password file
            SavePasswordFile();
        }

        public int Authenticate(string username, string password)
        {
            // authenticate user by username/password
            User u = UserByName(username);
            if (u == null || u.password != password)
                throw new Exception("User/password combination not found!");

            // return user id
            return u.userID;
        }

        public string UserName(int userID)
        {
            // lookup user by user id and return username
            if(!usersById.ContainsKey(userID))
                throw new Exception("UserID: " + userID + " not found!");

            return usersById[userID].userName;
        }

        public string UserHomeDirectory(int userID)
        {
            // lookup user by user id and return home directory
            if (!usersById.ContainsKey(userID))
                throw new Exception("UserID: " + userID + " not found!");

            return usersById[userID].homeDirectory;
        }

        public string UserPreferredShell(int userID)
        {
            // lookup user by user id and return shell name
            if (!usersById.ContainsKey(userID))
                throw new Exception("UserID: " + userID + " not found!");

            return usersById[userID].shell;
        }
    }
}
