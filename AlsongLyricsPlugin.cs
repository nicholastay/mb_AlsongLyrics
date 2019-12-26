using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "ALSong Lyrics";
            about.Description = "Gets lyrics from ALSong as a provider in MusicBee";
            about.Author = "Nicholas Tay (nexerq)";
            about.TargetApplication = "";   // current only applies to artwork, lyrics or instant messenger name that appears in the provider drop down selector or target Instant Messenger
            about.Type = PluginType.LyricsRetrieval;
            about.VersionMajor = 0;  // your plugin version
            about.VersionMinor = 2;
            about.Revision = 0;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = ReceiveNotificationFlags.StartupOnly;
            about.ConfigurationPanelHeight = 0;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function
            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            return true;
        }

        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
        }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
        }

        // return an array of lyric or artwork provider names this plugin supports
        // the providers will be iterated through one by one and passed to the RetrieveLyrics/ RetrieveArtwork function in order set by the user in the MusicBee Tags(2) preferences screen until a match is found
        public string[] GetProviders()
        {
            return new string[] { "ALSong" };
        }

        // It seems like alsong needs 'encData' - idk what this is, but use some random same value seems to work always, so just gonna use it
        public string ALSONG_API = "http://lyrics.alsong.co.kr/alsongwebservice/service1.asmx";
        public string ALSONG_ENC_DATA = "4e06a8c06f189e54e0f22e7f645f172bc6ba2702618c445c2973848e004d4709d745cad80f1fc63654bae492019e771af038de6822b1123687d6598f0064cae237c4e1ac873f4d3aa267a6c27197878a0638cf29b571f049d50add1f4303b8d46c05020516d5ca8000d05a10371829da7a90aad4f4c68a62c0c6083ede28f247";
        public string ALSONG_POST_TEMPLATE = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://www.w3.org/2003/05/soap-envelope""
xmlns:SOAP-ENC=""http://www.w3.org/2003/05/soap-encoding""
xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
xmlns:ns2=""ALSongWebServer/Service1Soap"" 
xmlns:ns1=""ALSongWebServer"" 
xmlns:ns3=""ALSongWebServer/Service1Soap12"">
<SOAP-ENV:Body>
<ns1:GetSyncLyricBySearch>
<ns1:encData>{2}</ns1:encData>
<ns1:title>{0}</ns1:title>
<ns1:artist>{1}</ns1:artist>
</ns1:GetSyncLyricBySearch>
</SOAP-ENV:Body>
</SOAP-ENV:Envelope>";
        public Regex AlsongLyricsReg = new Regex("<strLyric>(.*?)</strLyric>", RegexOptions.Singleline);
        public Regex AlsongTimingsReg = new Regex("^\\[\\d+:\\d+\\.\\d+\\] ?", RegexOptions.Multiline);

        // return lyrics for the requested artist/title from the requested provider
        // only required if PluginType = LyricsRetrieval
        // return null if no lyrics are found
        public string RetrieveLyrics(string sourceFileUrl, string artist, string trackTitle, string album, bool synchronisedPreferred, string provider)
        {
            string[] currTrack = new string[] { artist, trackTitle, album };

            // custom override above all else
            string searchTitle = trackTitle;
            string searchArtist = artist;
            if (isKeyDown(0xA0) && isKeyDown(0xA2)) // VK_LSHIFT && VK_LCONTROL
            {
                var qf = new mb_AlsongLyrics.AlsongCustomQueryForm(currTrack);
                qf.ShowDialog();

                searchTitle = qf.Title;
                searchArtist = qf.Artist;
            }
            else if (isKeyDown(0xA2)) // VK_LCONTROL
            {
                var af = new mb_AlsongLyrics.AlsongLyricPagingForm(currTrack, this);
                af.ShowDialog();

                string l = af.Lyrics;
                if (l == null)
                    return null;

                if (!synchronisedPreferred)
                    l = AlsongTimingsReg.Replace(l, ""); // remove timings
                else
                    l = fixSync(l);
                return l;
            }


            string page = AlsongMakeRequest(String.Format(ALSONG_POST_TEMPLATE, searchTitle, searchArtist, ALSONG_ENC_DATA));

            var match = AlsongLyricsReg.Match(page);
            if (!match.Success)
            {
                return null;
            }

            string lyrics = match.Groups[1].Value;

            if (!synchronisedPreferred)
                lyrics = AlsongTimingsReg.Replace(lyrics, ""); // remove timings
            else
                lyrics = fixSync(lyrics);

            return lyrics;
        }

        // return Base64 string representation of the artwork binary data from the requested provider
        // only required if PluginType = ArtworkRetrieval
        // return null if no artwork is found
        public string RetrieveArtwork(string sourceFileUrl, string albumArtist, string album, string provider)
        {
            //Return Convert.ToBase64String(artworkBinaryData)
            return null;
        }

        private string fixSync(string lyr)
        {
            // removes duplicate new line lyric lines so it syncs and musicbee blocks the lines together
            // https://stackoverflow.com/questions/1500194/c-looping-through-lines-of-multiline-string

            string lyrics = "";
            string prevTime = null;
            using (StringReader reader = new StringReader(lyr))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string currTime = null;
                    Match m = AlsongTimingsReg.Match(line);
                    if (m.Success)
                        currTime = m.Value;

                    if (prevTime != null && line.StartsWith(prevTime))
                        line = line.Replace(prevTime, "");
                    prevTime = currTime;

                    lyrics += line + "\n";
                }
            }

            return lyrics;
        }

        private bool isKeyDown(int k)
        {
            short s = GetAsyncKeyState(k);
            if ((s & 0x8000) > 0) // most sig bit for a short is set -> key down
                return true;
            return false;
        }

        public string AlsongMakeRequest(string formData)
        {
            var req = (HttpWebRequest)WebRequest.Create(ALSONG_API);
            var data = Encoding.UTF8.GetBytes(formData);

            req.Method = "POST";
            req.ContentType = "application/soap+xml; charset=UTF-8";
            req.ContentLength = data.Length;
            req.UserAgent = "gSOAP";
            using (var stream = req.GetRequestStream())
                stream.Write(data, 0, data.Length);

            var response = (HttpWebResponse)req.GetResponse();
            var page = new StreamReader(response.GetResponseStream()).ReadToEnd().Replace("&lt;br&gt;", "\n"); // html newlines
            return page;
        }
    }
}