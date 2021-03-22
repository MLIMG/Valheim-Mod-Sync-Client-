using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Valheim_Mod_Sync
{
    public partial class Settings : Form
    {
        public Settings()
        {
            InitializeComponent();

        }

        private void on_close_settings(object sender, FormClosingEventArgs e)
        {
            Form mainscreen = Application.OpenForms["MainScreen"];
            DataRow workRow = ((MainScreen)mainscreen).app_settings.NewRow();
            workRow["config_name"] = "custom_commands";
            workRow["config_value"] = textBox1.Text;
            ((MainScreen)mainscreen).app_settings.Rows.Add(workRow);
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            Form mainscreen = Application.OpenForms["MainScreen"];
            DataRow[] foundRows;
            foundRows = ((MainScreen)mainscreen).dataSet1.Tables["app_settings"].Select("config_name = 'custom_commands'");
            if (foundRows.Length > 0)
            {
                textBox1.Text = foundRows[0]["config_value"].ToString();
            }
        }
    }
}
