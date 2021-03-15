using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using System.Diagnostics;

namespace Valheim_Mod_Sync
{
    public partial class MainScreen : Form
    {
        Process vh_process;
        BepInExVcontrol bievc = new BepInExVcontrol();
        public MainScreen()
        {
            Cursor.Current = Cursors.WaitCursor;

            InitializeComponent();

            VersionControl vcontr = new VersionControl();
            vcontr.get_version();

            listView1.View = View.Details;
            listView1.HeaderStyle = ColumnHeaderStyle.None;

            ColumnHeader header = new ColumnHeader();
            header.Text = "";
            header.Name = "col1";
            header.Width = listView1.Width - 10;
            listView1.Columns.Add(header);

            listView2.View = View.Details;
            listView2.HeaderStyle = ColumnHeaderStyle.None;
            header = new ColumnHeader();
            header.Text = "";
            header.Name = "col1";
            header.Width = listView1.Width - 10;
            listView2.Columns.Add(header);

            //load dataset
            if (File.Exists("dataset.xml")){ dataSet1.ReadXml("dataset.xml", XmlReadMode.ReadSchema);}

            //Check if BepInEx is installed
            if (!bievc.is_bepinex_installed())
            {
                if(MessageBox.Show("BepInEx is required to run valheim mods.\n\nBepInEx is not installed. Install now?", "Missing", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
                {
                    //Install BepInEx
                    // Create a request for the URL.
                    WebRequest request = WebRequest.Create("https://valheim-mod-sync.easy-develope.ch/api/?action=get_bepinex_download");

                    // If required by the server, set the credentials.
                    request.Credentials = CredentialCache.DefaultCredentials;

                    // Get the response.
                    WebResponse response = request.GetResponse();

                    if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                    {
                        var reader = new StreamReader(response.GetResponseStream());
                        var bep_url = reader.ReadToEnd();

                        if (bep_url.Length == 0)
                        {
                            MessageBox.Show("BepInEx Download link not found", "Sorry", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        } else
                        {
                            Cursor.Current = Cursors.WaitCursor;
                            Console.WriteLine(bep_url);
                            WebClient webClient = new WebClient();
                            webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(CompletedBepDownload);
                            webClient.DownloadFileAsync(new Uri(bep_url), "BepInEx.zip");
                        }
                    }
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            else
            {
                //Populate protected config list
                var config_dir = new DirectoryInfo("BepInEx/config/");
                foreach (FileInfo fi in config_dir.GetFiles())
                {
                    DataRow[] foundRows;
                    bool is_checked = false;
                    foundRows = dataSet1.Tables["protected_configs"].Select("name = '" + fi.Name + "'");
                    if (foundRows.Length != 0)
                    {
                        is_checked = true;
                    }
                    checkedListBox2.Items.Add(fi.Name, is_checked);
                }

                //Populate protected plugins list
                var plugins_dir = new DirectoryInfo("BepInEx/plugins/");
                foreach (FileInfo fi in plugins_dir.GetFiles())
                {
                    DataRow[] foundRows;
                    bool is_checked = false;
                    foundRows = dataSet1.Tables["protected_plugins"].Select("name = '" + fi.Name + "'");
                    if (foundRows.Length != 0)
                    {
                        is_checked = true;
                    }
                    checkedListBox1.Items.Add(fi.Name, is_checked);
                }
            }

            //Populate Local server list
            DataRow[] currentRows = local_server_list.Select(null, null, DataViewRowState.CurrentRows);
            if (currentRows.Length < 1)
                Console.WriteLine("No Current Rows Found");
            else
            {
                foreach (DataRow row in currentRows)
                {
                    foreach (DataColumn column in local_server_list.Columns)
                    {
                        if (column.ColumnName.Equals("name"))
                        {
                            listView1.Items.Add(row[column].ToString());
                        }
                    }
                }
            }

            //Populate global server list
            //TODO
            listView2.Items.Add("In Development...");
            Cursor.Current = Cursors.Default;
        }

        //Connect with server btn
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Equals(""))
            {
                MessageBox.Show("Server key is required!", "Sorry", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } else
            {
                Cursor.Current = Cursors.WaitCursor;
                // Create a request for the URL.
                WebRequest request = WebRequest.Create("https://valheim-mod-sync.easy-develope.ch/api/?action=get_server_name&server_id=" + textBox1.Text);

                // If required by the server, set the credentials.
                request.Credentials = CredentialCache.DefaultCredentials;

                // Get the response.
                WebResponse response = request.GetResponse();

                if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                {
                    var reader = new StreamReader(response.GetResponseStream());
                    var content = reader.ReadToEnd();

                    if(content.Length == 0)
                    {
                        MessageBox.Show("This server dsnt exists.", "Sorry", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    Console.WriteLine(content);

                    DataRow[] foundRows;
                    foundRows = dataSet1.Tables["local_server_list"].Select("id = '"+ textBox1.Text + "'");

                    if(foundRows.Length == 0)
                    {
                        DataRow workRow = local_server_list.NewRow();
                        workRow["id"] = Int32.Parse(textBox1.Text);
                        workRow["name"] = content;
                        local_server_list.Rows.Add(workRow);
                        listView1.Items.Add(content);
                        Cursor.Current = Cursors.Default;
                    }
                    start_valheim(textBox1.Text);
                }
                Cursor.Current = Cursors.Default;
            }
        }

        private void on_close_form(object sender, FormClosingEventArgs e)
        {
            if(vh_process != null)
            {
                vh_process.Refresh();

                // close application if it is still open       
                if (!vh_process.HasExited)
                {
                    if (MessageBox.Show("Valheim is Running.\nAre you sure you want close Valheim?", "Valheim still running", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
                    {
                        vh_process.Kill();
                        restore_default_data();
                    }
                }
                else
                {
                    restore_default_data();
                }
            } else
            {
                restore_default_data();
            }
            string xmlData = dataSet1.GetXml();
            string filePath = "dataset.xml";
            dataSet1.WriteXml(filePath);
        }

        private void restore_default_data()
        {
            var plugins_info = new DirectoryInfo("BepInEx/plugins/");
            foreach (FileInfo fi in plugins_info.GetFiles())
            {
                File.Delete("BepInEx/plugins/" + fi.Name);
            }
            if (!Directory.Exists("BepInEx/plugins/protected/"))
            {
                Directory.CreateDirectory("BepInEx/plugins/protected/");
            }
            plugins_info = new DirectoryInfo("BepInEx/plugins/protected/");
            foreach (FileInfo fi in plugins_info.GetFiles())
            {
                    File.Copy("BepInEx/plugins/protected/" + fi.Name, "BepInEx/plugins/" + fi.Name, true);
            }


            var config_info = new DirectoryInfo("BepInEx/config/");
            foreach (FileInfo fi in config_info.GetFiles())
            {
                File.Delete("BepInEx/config/" + fi.Name);
            }
            if (!Directory.Exists("BepInEx/config/protected/"))
            {
                Directory.CreateDirectory("BepInEx/config/protected/");
            }
            config_info = new DirectoryInfo("BepInEx/config/protected/");
            foreach (FileInfo fi in config_info.GetFiles())
            {
                File.Copy("BepInEx/config/protected/" + fi.Name, "BepInEx/config/" + fi.Name, true);
            }
        }

        private void CompletedBepDownload(object sender, AsyncCompletedEventArgs e)
        {
            if (Directory.Exists("tmp/"))
            {
                Directory.Delete("tmp/",true);
            }
            string zipPath = "BepInEx.zip";
            string extractPath = "tmp/";
            ZipFile.ExtractToDirectory(zipPath, extractPath);

            string strWorkPath = Directory.GetCurrentDirectory();
            string sourceDirectory = "tmp/BepInExPack_Valheim/";
            string targetDirectory = @strWorkPath;
            Cursor.Current = Cursors.WaitCursor;
            Copy(sourceDirectory, targetDirectory);

            if (Directory.Exists("tmp/"))
            {
                Directory.Delete("tmp/", true);
            }
            if (File.Exists("BepInEx.zip"))
            {
                File.Delete("BepInEx.zip");
            }
            Cursor.Current = Cursors.Default;

            MessageBox.Show("BepInEx installation successful.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            var diSource = new DirectoryInfo(sourceDirectory);
            var diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(checkedListBox1.GetItemCheckState(checkedListBox1.SelectedIndex) == CheckState.Checked)
            {
                Console.WriteLine("Checked");
                Console.WriteLine(checkedListBox1.SelectedItem.ToString());

                DataRow[] foundRows;
                foundRows = dataSet1.Tables["protected_plugins"].Select("name = '" + checkedListBox1.SelectedItem.ToString() + "'");

                if (foundRows.Length == 0)
                {
                    DataRow workRow = protected_plugins.NewRow();
                    workRow["name"] = checkedListBox1.SelectedItem.ToString();
                    protected_plugins.Rows.Add(workRow);
                }
            }
            else
            {
                Console.WriteLine("Unchecked");
                Console.WriteLine(checkedListBox1.SelectedItem.ToString());

                DataRow[] foundRows;
                foundRows = dataSet1.Tables["protected_plugins"].Select("name = '" + checkedListBox1.SelectedItem.ToString() + "'");
                if (foundRows.Length != 0)
                {
                    protected_plugins.Rows.Remove(foundRows[0]);
                }
            }
        }

        private void checkedListBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (checkedListBox2.GetItemCheckState(checkedListBox2.SelectedIndex) == CheckState.Checked)
            {
                Console.WriteLine("Checked");
                Console.WriteLine(checkedListBox2.SelectedItem.ToString());

                DataRow[] foundRows;
                foundRows = dataSet1.Tables["protected_configs"].Select("name = '" + checkedListBox2.SelectedItem.ToString() + "'");

                if (foundRows.Length == 0)
                {
                    DataRow workRow = protected_configs.NewRow();
                    workRow["name"] = checkedListBox2.SelectedItem.ToString();
                    protected_configs.Rows.Add(workRow);
                }
            }
            else
            {
                Console.WriteLine("Unchecked");
                Console.WriteLine(checkedListBox2.SelectedItem.ToString());

                DataRow[] foundRows;
                foundRows = dataSet1.Tables["protected_configs"].Select("name = '" + checkedListBox2.SelectedItem.ToString() + "'");
                if (foundRows.Length != 0)
                {
                    protected_configs.Rows.Remove(foundRows[0]);
                }
            }
        }

        private void start_valheim(string server_id)
        {
            string steam_dir = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam","InstallPath","0");
            Console.WriteLine(steam_dir);
            if (steam_dir != null)
            {
                string steam_exe = steam_dir + @"\steam.exe";
                string valheim_local_low = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\IronGate\Valheim\";
                string valheim_character_path = valheim_local_low + @"characters\";
                if (Directory.Exists(valheim_character_path))
                {
                    //Backup characters
                    string startPath = valheim_character_path;
                    DateTime dt = DateTime.Now;
                    string date = dt.ToString("g", DateTimeFormatInfo.InvariantInfo).Replace(":", "-").Replace("/", "-").Replace(" ", "-");
                    string zipPath = valheim_character_path + date + ".zip";
                    try
                    {
                        ZipFile.CreateFromDirectory(startPath, zipPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        File.Delete(valheim_character_path + date + ".zip");
                        if (!Directory.Exists(valheim_character_path + @"\char_backup\"))
                        {
                            Directory.CreateDirectory(valheim_character_path + @"\char_backup\");
                        }
                        var char_info = new DirectoryInfo(valheim_character_path);
                        foreach (FileInfo fi in char_info.GetFiles())
                        {
                            File.Copy(valheim_character_path + fi.Name, valheim_character_path + @"\char_backup\" + fi.Name, true);
                        }
                    }
                }

                //Get server mods & clear mod dir exept protected ones
                var plugins_info = new DirectoryInfo("BepInEx/plugins/");
                if (!Directory.Exists("BepInEx/plugins/protected/"))
                {
                    Directory.CreateDirectory("BepInEx/plugins/protected/");
                }
                foreach (FileInfo fi in plugins_info.GetFiles())
                {
                    DataRow[] foundRows;
                    foundRows = dataSet1.Tables["protected_plugins"].Select("name = '" + fi.Name + "'");
                    if (foundRows.Length == 0)
                    {
                        File.Delete("BepInEx/plugins/" + fi.Name);
                    } 
                    else
                    {
                        File.Copy("BepInEx/plugins/" + fi.Name, "BepInEx/plugins/protected/" + fi.Name, true);
                    }
                }

                // Create a request for the URL.
                WebRequest request = WebRequest.Create("https://valheim-mod-sync.easy-develope.ch/api/?action=get_server_data_mods&server_id=" + server_id);

                // If required by the server, set the credentials.
                request.Credentials = CredentialCache.DefaultCredentials;

                // Get the response.
                WebResponse response = request.GetResponse();

                if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                {
                    var reader = new StreamReader(response.GetResponseStream());
                    var content = reader.ReadToEnd();
                    StringReader strReader = new StringReader(content);
                    string line;
                    while (null != (line = strReader.ReadLine()))
                    {
                        Cursor.Current = Cursors.WaitCursor;
                        string file_url = line.Split('|')[0];
                        string file_name = line.Split('|')[1];
                        Console.WriteLine(line);
                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(new Uri(file_url), "BepInEx/plugins/" + file_name);
                        Console.WriteLine(line);
                    }
                }

                //Get server configs & clear config dir exept protected ones
                var config_info = new DirectoryInfo("BepInEx/config/");
                if (!Directory.Exists("BepInEx/config/protected/"))
                {
                    Directory.CreateDirectory("BepInEx/config/protected/");
                }
                foreach (FileInfo fi in config_info.GetFiles())
                {
                    DataRow[] foundRows;
                    foundRows = dataSet1.Tables["protected_configs"].Select("name = '" + fi.Name + "'");
                    if (foundRows.Length == 0)
                    {
                        File.Delete("BepInEx/config/" + fi.Name);
                    }
                    else
                    {
                        File.Copy("BepInEx/config/" + fi.Name, "BepInEx/config/protected/" + fi.Name,true);
                    }
                }
                // Create a request for the URL.
                request = WebRequest.Create("https://valheim-mod-sync.easy-develope.ch/api/?action=get_server_data_configs&server_id=" + server_id);

                // If required by the server, set the credentials.
                request.Credentials = CredentialCache.DefaultCredentials;

                // Get the response.
                response = request.GetResponse();

                if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                {
                    var reader = new StreamReader(response.GetResponseStream());
                    var content = reader.ReadToEnd();
                    StringReader strReader = new StringReader(content);
                    string line;
                    while (null != (line = strReader.ReadLine()))
                    {
                        Cursor.Current = Cursors.WaitCursor;
                        string file_url = line.Split('|')[0];
                        string file_name = line.Split('|')[1];
                        Console.WriteLine(line);
                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(new Uri(file_url), "BepInEx/config/" + file_name);
                        Console.WriteLine(line);
                    }
                }

                //get server ip and port
                // Create a request for the URL.
                request = WebRequest.Create("https://valheim-mod-sync.easy-develope.ch/api/?action=get_server_ip_port&server_id=" + server_id);

                // If required by the server, set the credentials.
                request.Credentials = CredentialCache.DefaultCredentials;

                // Get the response.
                response = request.GetResponse();

                if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                {
                    var reader = new StreamReader(response.GetResponseStream());
                    var content = reader.ReadToEnd();

                    DataRow[] foundRows;
                    foundRows = dataSet1.Tables["local_server_list"].Select("id = '" + server_id + "'");
                    if (foundRows.Length != 0)
                    {
                        string server_name = foundRows[0]["name"].ToString();
                        string server_pw = foundRows[0]["password"].ToString();
                        string result = Prompt.ShowDialog("Login", "Password", server_pw);
                        if (result.Length != 0)
                        {
                            foundRows[0]["password"] = result;
                            foundRows[0]["id"] = server_id;
                            foundRows[0]["name"] = server_name;
                            local_server_list.Rows.Remove(foundRows[0]);

                            DataRow workRow = local_server_list.NewRow();
                            workRow["password"] = result;
                            workRow["id"] = server_id;
                            workRow["name"] = server_name;
                            local_server_list.Rows.Add(workRow);
                            string exec_data;
                            Console.WriteLine(content);
                            if (content.Equals(":"))
                            {
                                exec_data = " -applaunch 892970 +password " + result;
                            } else
                            {
                                exec_data = " -applaunch 892970 +connect " + content + " +password " + result;
                            }
                            Console.WriteLine(exec_data);
                            vh_process = Process.Start(@steam_exe, exec_data);
                        }
                        else
                        {
                            MessageBox.Show("Cant launch server", "Sorry", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Steam installation path not found!", "Sorry", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void local_server_clik(object sender, EventArgs e)
        {
            string server_name = listView1.SelectedItems[0].Text;
            Console.WriteLine(server_name);
            DataRow[] foundRows;
            foundRows = dataSet1.Tables["local_server_list"].Select("name = '" + server_name + "'");
            if (foundRows.Length != 0)
            {
                Console.WriteLine(foundRows[0]["id"]);
                start_valheim(foundRows[0]["id"].ToString());
            }
        }

        private void global_server_clik(object sender, EventArgs e)
        {
            string server_name = listView2.SelectedItems[0].Name;
        }
    }
    public static class Prompt
    {
        public static string ShowDialog(string text, string caption,string default_val)
        {
            Form prompt = new Form()
            {
                Width = 300,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            Label textLabel = new Label() { Left = 16, Top = 20, Text = text };
            TextBox textBox = new TextBox() { Left = 16, Top = 40, Width = 250 };
            textBox.Text = default_val;
            Button confirmation = new Button() { Text = "Ok", Left = 100, Width = 100, Top = 70, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }
}
