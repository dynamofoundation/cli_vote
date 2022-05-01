using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace cli_vote
{
    public class Utility
    {

        static HttpWebRequest webRequest;


        public static string rpcExec(string command)
        {
            log(command);

            webRequest = (HttpWebRequest)WebRequest.Create("http://" + Global.server + ":" + Global.port);
            webRequest.KeepAlive = false;
            webRequest.Timeout = 300000;

            var data = Encoding.ASCII.GetBytes(command);

            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = data.Length;

            var username = Global.username;
            var password = Global.password;
            string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
            webRequest.Headers.Add("Authorization", "Basic " + encoded);


            using (var stream = webRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            string result;

            try
            {
                WebResponse webresponse = (HttpWebResponse)webRequest.GetResponse();
                result = new StreamReader(webresponse.GetResponseStream()).ReadToEnd();
                webresponse.Dispose();
            }
            catch (WebException ex)
            {
                log(ex.Message);
                WebResponse webresponse = ex.Response;
                result = new StreamReader(webresponse.GetResponseStream()).ReadToEnd();
            }

            log(result);

            return result;
        }

        public static void log(string data)
        {
            if (!Global.debug)
                return;
            File.AppendAllText("log.txt", data + "\r\n");
        }


        public static string ByteArrayToHexString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

    }
}
