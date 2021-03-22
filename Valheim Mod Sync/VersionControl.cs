using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Valheim_Mod_Sync
{
    class VersionControl
    {
        string current_version = "1.0.2.beta";

        public VersionControl() { }

        public string get_version()
        {
            // Create a request for the URL.
            WebRequest request = WebRequest.Create("https://valheim-mod-sync.easy-develope.ch/api/version/");

            // If required by the server, set the credentials.
            request.Credentials = CredentialCache.DefaultCredentials;

            // Get the response.
            WebResponse response = request.GetResponse();

            if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
            {
                var reader = new StreamReader(response.GetResponseStream());
                var content = reader.ReadToEnd();
                if (!content.Equals(current_version))
                {
                    MessageBox.Show("New app version " + content + " is ready to download.", "New Version", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // Create a request for the URL.
                    request = WebRequest.Create("https://valheim-mod-sync.easy-develope.ch/api/version/downloads/");

                    // If required by the server, set the credentials.
                    request.Credentials = CredentialCache.DefaultCredentials;

                    // Get the response.
                    response = request.GetResponse();

                    if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                    {
                        reader = new StreamReader(response.GetResponseStream());
                        content = reader.ReadToEnd();
                        System.Diagnostics.Process.Start(content);
                    }
                }
            }
            return current_version;
        }
    }
}
