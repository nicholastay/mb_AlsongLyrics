using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
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
            about.VersionMinor = 1;
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

        private const string ALSONG_API = "http://lyrics.alsong.co.kr/alsongwebservice/service1.asmx";
        private const string ALSONG_POST_TEMPLATE = "<Envelope xmlns=\"http://www.w3.org/2003/05/soap-envelope\"><Body><GetResembleLyric2 xmlns=\"ALSongWebServer\"><stQuery><strTitle>{0}</strTitle><strArtistName>{1}</strArtistName></stQuery></GetResembleLyric2></Body></Envelope>";

        private Regex lyricsReg = new Regex("<strLyric>(.*?)</strLyric>");
        private Regex timingsReg = new Regex("^\\[\\d\\d:\\d\\d.\\d\\d\\]", RegexOptions.Multiline);

        // return lyrics for the requested artist/title from the requested provider
        // only required if PluginType = LyricsRetrieval
        // return null if no lyrics are found
        public string RetrieveLyrics(string sourceFileUrl, string artist, string trackTitle, string album, bool synchronisedPreferred, string provider)
        {
            var req = (HttpWebRequest)WebRequest.Create(ALSONG_API);
            var data = Encoding.UTF8.GetBytes(String.Format(ALSONG_POST_TEMPLATE, trackTitle, artist));

            req.Method = "POST";
            req.ContentType = "application/soap+xml; charset=UTF-8";
            req.ContentLength = data.Length;
            req.UserAgent = "gSOAP";
            req.Headers.Add("SOAPAction", "\"ALSongWebServer/GetResembleLyric2\"");
            using (var stream = req.GetRequestStream())
                stream.Write(data, 0, data.Length);

            var response = (HttpWebResponse)req.GetResponse();
            var page = new StreamReader(response.GetResponseStream()).ReadToEnd();

            var match = lyricsReg.Match(page);
            if (!match.Success)
                return null;

            string lyrics =
                match.Groups[1].Value
                    .Replace("&lt;br&gt;", "\n"); // html newlines

            if (!synchronisedPreferred)
                lyrics = timingsReg.Replace(lyrics, ""); // remove timings
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
    }
}