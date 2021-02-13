using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VRCWorldPersistency
{
    public partial class GUI : Form
    {
        public GUI()
        {
            InitializeComponent();
            UpdateTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            RuntimeLogfileWatcher.Update();
        }

        private void GUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            Console.WriteLine("Form closing, end log watcher");
            RuntimeLogfileWatcher.CleanupLogWatcher();
        }
    }
}
