using System;
using System.Data;
using System.Data.Common;

namespace DBUtil
{
    class dblogin
    {
        static DbProviderFactory _factory = null;
        static string _server = null;
        static string _database = null;
        static string _username = null;
        static string _password = null;

        DbProviderFactory factory()
        {
            if (_factory == null)
            {
                login();
            }

            return _factory;
        }

        string server()
        {
            return _server;
        }

        string database()
        {
            return _database;
        }

        string username()
        {
            return _username;
        }

        string password()
        {
            return _password;
        }

        void login()
        {
            //DBLogin win;

            // Get all providers (table with 4 columns).
            var dtDataProviders = DbProviderFactories.GetFactoryClasses();
            /*
            // Show all providers.
            win.cbDBProvider.DisplayMember = "NAME";
            win.cbDBProvider.DataSource = dtDataProviders;

            win.tbServer.Text = _server;
            win.tbDatabase.Text = _database;
            win.tbUsername.Text = _username;
            win.tbPassword.Text = _password;

            if(win.ShowDialog() = DialogResult.OK)
            {
                // Create the DbProviderFactory.
                _factory = DbProviderFactories.GetFactory(dtDataProviders.Rows[win.cbDBProvider.SelectedIndex]);
            }*/
        }

        /*
             TODO: Insert code to perform custom authentication using the provided username and password
             (See http://go.microsoft.com/fwlink/?LinkId=35339).
             The custom principal can then be attached to the current thread's principal as follows:
                     My.User.CurrentPrincipal = CustomPrincipal
             where CustomPrincipal is the IPrincipal implementation used to perform authentication.
             Subsequently, My.User will return identity information encapsulated in the CustomPrincipal object
             such as the username, display name, etc.
        */
        void OK_Click(object sender, EventArgs e)
        {
            /*_server = tbServer.Text;
            _database = tbDatabase.Text;
            _username = tbUsername.Text;
            _password = tbPassword.Text;*/

            //this.Close();
        }

        void Cancel_Click(object sender, EventArgs e)
        {
            //this.Close();
        }
    }
}
