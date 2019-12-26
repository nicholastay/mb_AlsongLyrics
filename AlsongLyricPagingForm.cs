using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace mb_AlsongLyrics
{
    public partial class AlsongLyricPagingForm : Form
    {
        public string Title;
        public string Artist;
        public string Lyrics = null;

        public const string ALSONG_LIST_FORM = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://www.w3.org/2003/05/soap-envelope""
xmlns:SOAP-ENC=""http://www.w3.org/2003/05/soap-encoding""
xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
xmlns:ns2=""ALSongWebServer/Service1Soap"" 
xmlns:ns1=""ALSongWebServer"" 
xmlns:ns3=""ALSongWebServer/Service1Soap12"">
<SOAP-ENV:Body>
<ns1:GetResembleLyricList2>
<ns1:encData>{2}</ns1:encData>
<ns1:title>{0}</ns1:title>
<ns1:artist>{1}</ns1:artist>
</ns1:GetResembleLyricList2>
</SOAP-ENV:Body>
</SOAP-ENV:Envelope>";
        public const string ALSONG_ID_GET_FORM = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://www.w3.org/2003/05/soap-envelope""
xmlns:SOAP-ENC=""http://www.w3.org/2003/05/soap-encoding""
xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
xmlns:ns2=""ALSongWebServer/Service1Soap"" 
xmlns:ns1=""ALSongWebServer"" 
xmlns:ns3=""ALSongWebServer/Service1Soap12"">
<SOAP-ENV:Body>
<ns1:GetLyricByID2>
<ns1:encData>{1}</ns1:encData>
<ns1:lyricID>{0}</ns1:lyricID>
</ns1:GetLyricByID2>
</SOAP-ENV:Body>
</SOAP-ENV:Envelope>";

        public Regex AlsongListReg = new Regex("<lyricID>(.*?)</lyricID>", RegexOptions.Singleline);
        public Regex AlsongListLyricReg = new Regex("<lyric>(.*?)</lyric>", RegexOptions.Singleline);

        private MusicBeePlugin.Plugin self;

        public AlsongLyricPagingForm(string[] track, MusicBeePlugin.Plugin self)
        {
            InitializeComponent();

            Artist = track[0];
            Title = track[1];
            this.self = self;

            statusLabel.Text = $"Selecting for: {Artist} - {Title}";
        }

        private string getIdLyric(string id)
        {
            string idPage = self.AlsongMakeRequest(String.Format(ALSONG_ID_GET_FORM, id, self.ALSONG_ENC_DATA));
            var match = AlsongListLyricReg.Match(idPage);
            return match.Groups[1].Value;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox1.Enabled = false;
            button1.Enabled = false;

            Lyrics = getIdLyric(comboBox1.Text);
            textBox1.Text = Lyrics;

            comboBox1.Enabled = true;
            button1.Enabled = true;
        }

        private void AlsongLyricPagingForm_Load(object sender, EventArgs e)
        {
            // get list
            string listPage = self.AlsongMakeRequest(String.Format(ALSONG_LIST_FORM, Title, Artist, self.ALSONG_ENC_DATA));
            foreach (Match m in AlsongListReg.Matches(listPage))
                comboBox1.Items.Add(m.Groups[1].Value);
            comboBox1.SelectedIndex = 0;

            comboBox1.Enabled = true;
            button1.Enabled = true;
        }
    }
}
