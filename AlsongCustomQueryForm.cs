using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace mb_AlsongLyrics
{
    public partial class AlsongCustomQueryForm : Form
    {
        public string Title;
        public string Artist;

        public AlsongCustomQueryForm(string[] track)
        {
            InitializeComponent();

            Artist = track[0];
            Title = track[1];

            artistTextBox.Text = Artist;
            titleTextBox.Text = Title;

            statusLabel.Text = $"Modifying query: {Artist} - {Title}";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(artistTextBox.Text) || String.IsNullOrEmpty(titleTextBox.Text))
                return;

            Artist = artistTextBox.Text;
            Title = titleTextBox.Text;

            Close();
        }
    }
}
