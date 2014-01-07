﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DeadDog
{
    /// <summary>
    /// Identifies a http-accesible address and uses <see cref="HttpWebRequest"/> and <see cref="HttpWebResponse"/> to allow simple http-download.
    /// </summary>
    public class URL : IEquatable<URL>
    {
        private string url;

        /// <summary>
        /// Initializes a new instance of the <see cref="URL"/> class.
        /// </summary>
        /// <param name="url">The http url associated with this instance. Must begin with "http://"</param>
        public URL(string url)
        {
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                throw new ArgumentException("The url specified must always begin with \"http://\"", "url");
            this.url = url;
        }

        /// <summary>
        /// Creates a webrequest and returns the response stream converted to a <see cref="String"/> using ASCII encoding.
        /// </summary>
        /// <returns>The contents of the URL as a <see cref="String"/>.</returns>
        public string GetHTML()
        {
            return GetHTML(Encoding.ASCII);
        }
        /// <summary>
        /// Creates a webrequest and returns the response stream converted to a <see cref="String"/> using automatic (html only) detection of encoding.
        /// </summary>
        /// <param name="detectEncoding">A boolean value indicating if encoding should be automatically determined.</param>
        /// <returns>The contents of the URL as a <see cref="String"/>.</returns>
        public string GetHTML(bool detectEncoding)
        {
            byte[] buffer;
            using (MemoryStream ms = new MemoryStream())
            {
                LoadToMemoryStream(ms);
                buffer = ms.GetBuffer();
            }

            string temp = Encoding.ASCII.GetString(buffer);
            if (!detectEncoding)
                return temp;

            Encoding enc = DetermineEncoding(temp) ?? Encoding.ASCII;
            return enc.GetString(buffer);
        }
        /// <summary>
        /// Creates a webrequest and returns the response stream converted to a <see cref="String"/> using the specified encoding.
        /// </summary>
        /// <param name="encoding">The encoding used to convert the binary stream.</param>
        /// <returns>The contents of the URL as a <see cref="String"/>.</returns>
        public string GetHTML(Encoding encoding)
        {
            byte[] buffer;
            using (MemoryStream ms = new MemoryStream())
            {
                LoadToMemoryStream(ms);
                buffer = ms.GetBuffer();
            }

            return encoding.GetString(buffer);
        }

        /// <summary>
        /// Creates a webrequest and returns the response stream converted to a <see cref="String"/> using ASCII encoding.
        /// </summary>
        /// <param name="readURL">When the method returns, contains the url address that was read (in case of redirection).</param>
        /// <returns>The contents of the URL as a <see cref="String"/>.</returns>
        public string GetHTML(out URL readURL)
        {
            return GetHTML(Encoding.ASCII, out readURL);
        }
        /// <summary>
        /// Creates a webrequest and returns the response stream converted to a <see cref="String"/> using the specified encoding.
        /// </summary>
        /// <param name="encoding">The encoding used to convert the binary stream.</param>
        /// <param name="readURL">When the method returns, contains the url address that was read (in case of redirection).</param>
        /// <returns>The contents of the URL as a <see cref="String"/>.</returns>
        public string GetHTML(Encoding encoding, out URL readURL)
        {
            byte[] buffer;
            using (MemoryStream ms = new MemoryStream())
            {
                LoadToMemoryStream(ms, out readURL);
                buffer = ms.GetBuffer();
            }

            return encoding.GetString(buffer);
        }

        public static Encoding DetermineEncoding(string html)
        {
            Match m = Regex.Match(html, "charset=\"(?<charset>.*)\"");
            if (!m.Success)
                m = Regex.Match(html, "<meta .*?content=\".*?charset=(?<charset>.*)\"");
            if (m.Success)
            {
                string encodingName = m.Groups["charset"].Value;
                if (encodingName.Contains(" "))
                    encodingName = encodingName.Substring(0, encodingName.IndexOf(' '));

                Encoding enc;
                try
                {
                    enc = Encoding.GetEncoding(encodingName);
                }
                catch
                {
                    enc = null;
                }
                return enc;
            }
            return null;
        }

        private void LoadToMemoryStream(MemoryStream ms)
        {
            URL url;
            LoadToMemoryStream(ms, out url);
        }
        private void LoadToMemoryStream(MemoryStream ms, out URL readURL)
        {
            byte[] buf;

            HttpWebRequest request;
            HttpWebResponse response;
            Stream resStream;

            int attempt = 0;
            int maxattempt = 3;

        jan:
            try
            {
                attempt++;
                buf = new byte[8192];

                request = (HttpWebRequest)WebRequest.Create(this.url);
                response = (HttpWebResponse)request.GetResponse();

                readURL = new URL(request.Address.AbsoluteUri);

                resStream = response.GetResponseStream();
            }
            catch
            {
                System.Threading.Thread.Sleep(2000);
                if (attempt < maxattempt)
                    goto jan;
                else
                    throw new Exception("File could not be loaded after " + maxattempt + " attempts.");
            }

            if (response.ContentLength > int.MaxValue)
                throw new InvalidOperationException("Cannot read files larger than 2gb (UINT32 max)");

            int count = 0;
            do
            {
                count = resStream.Read(buf, 0, buf.Length);
                if (count > 0)
                    ms.Write(buf, 0, count);
            }
            while (count > 0);
            resStream.Dispose();
        }

        /// <summary>
        /// Creates a webrequest and returns the response stream converted to a <see cref="Image"/>.
        /// </summary>
        /// <returns>The contents of the URL as a <see cref="Image"/>.</returns>
        public Image GetImage()
        {
            Image image;
            MemoryStream ms = new MemoryStream();
            LoadToMemoryStream(ms);
            image = Image.FromStream(ms);
            //The MemoryStream should not be disposed, as the Image is bound to this stream.
            //The stream will be disposed by disposing the Image.
            //See http://stackoverflow.com/questions/336387/image-save-throws-a-gdi-exception-because-the-memory-stream-is-closed

            return image;
        }
        /// <summary>
        /// Creates a webrequest and writes the response stream to a file.
        /// </summary>
        /// <param name="localFile">The local path where the contents of the url is stored.</param>
        public void GetFile(string localFile)
        {
            byte[] buf = new byte[8192];

            HttpWebRequest request = (HttpWebRequest)
                WebRequest.Create(this.url);

            HttpWebResponse response = (HttpWebResponse)
                request.GetResponse();

            Stream resStream = response.GetResponseStream();

            int count = 0;
            using (FileStream fs = new FileStream(localFile, FileMode.Create, FileAccess.Write))
            {
                do
                {
                    count = resStream.Read(buf, 0, buf.Length);
                    if (count != 0)
                    {
                        fs.Write(buf, 0, count);
                    }
                }
                while (count > 0);
            }
        }

        /// <summary>
        /// Gets the <see cref="URL"/> to which this <see cref="URL"/> redirects.
        /// </summary>
        /// <returns>An instance of <see cref="URL"/> to which this <see cref="URL"/> redirects.</returns>
        public URL GetURL()
        {
            HttpWebRequest request;
            HttpWebResponse response = null;

            int attempt = 0;
            int maxattempt = 3;

        jan:
            try
            {
                attempt++;

                request = (HttpWebRequest)WebRequest.Create(this.url);
                response = (HttpWebResponse)request.GetResponse();

                string uri = request.Address.AbsoluteUri.ToString();

                response.Close();

                return new URL(uri);
            }
            catch
            {
                System.Threading.Thread.Sleep(2000);
                if (attempt < maxattempt)
                    goto jan;
                else
                {
                    response.Close();
                    throw new Exception("File could not be loaded after " + maxattempt + " attempts.");
                }
            }
        }

        /// <summary>
        /// Gets the url-address associated with this instance.
        /// </summary>
        public string Address
        {
            get { return url; }
        }

        /// <summary>
        /// Specifies whether this <see cref="URL"/> contains the same address as the specified <see cref="Object"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to test.</param>
        /// <returns>true, if <paramref name="obj"/> is an <see cref="URL"/> and has the same address as this <see cref="URL"/>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is URL)
                return this.Equals(obj as URL);
            else
                return base.Equals(obj);
        }
        /// <summary>
        /// Specifies whether this <see cref="URL"/> contains the same address as the specified <see cref="URL"/>.
        /// </summary>
        /// <param name="other">The <see cref="URL"/> to test.</param>
        /// <returns>true, if <paramref name="other"/> has the same address as this <see cref="URL"/>.</returns>
        public bool Equals(URL other)
        {
            if (ReferenceEquals(other, null))
                return false;

            return this.url.Equals(other.url);
        }
        /// <summary>
        /// Returns a hash code for this <see cref="URL"/>.
        /// </summary>
        /// <returns>An integer value that specifies a hash value for this <see cref="URL"/>.</returns>
        public override int GetHashCode()
        {
            return url.GetHashCode();
        }

        /// <summary>
        /// Compares two <see cref="URL"/> objects for value-equality.
        /// </summary>
        /// <param name="a">An <see cref="URL"/> to compare.</param>
        /// <param name="b">An <see cref="URL"/> to compare.</param>
        /// <returns>true, if <paramref name="a"/> and <paramref name="b"/> are equal; otherwise, false.</returns>
        public static bool operator ==(URL a, URL b)
        {
            return a.Equals(b);
        }
        /// <summary>
        /// Compares two <see cref="URL"/> objects for value-inequality.
        /// </summary>
        /// <param name="a">An <see cref="URL"/> to compare.</param>
        /// <param name="b">An <see cref="URL"/> to compare.</param>
        /// <returns>false, if <paramref name="a"/> and <paramref name="b"/> are equal; otherwise, true.</returns>
        public static bool operator !=(URL a, URL b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Returns a <see cref="String"/> that represents the current <see cref="URL"/>.
        /// </summary>
        /// <returns>A <see cref="String"/> that represents the current <see cref="URL"/></returns>
        public override string ToString()
        {
            return "URL [" + url + "]";
        }
    }
}
