using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.IO;
using System.IO.Compression;
using UnityEngine.Networking;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Threading;
using System.Text.RegularExpressions;


public class IPPreview : MonoBehaviour
{
    public MeshRenderer frame;    //Mesh for displaying video
    public bool draw;
    public MemoryStream ms;

    private string sourceURL = "http://24.172.4.142/mjpg/video.mjpg?COUNTER";
    private Texture2D texture; 
    private Stream stream;
 
    void Start(){
        draw = false;

        StartCoroutine(LoadFromWeb(sourceURL));
    }


    public void GetVideo(string url){
        texture = new Texture2D(2, 2); 
        // create HTTP request
        HttpWebRequest req = (HttpWebRequest) WebRequest.Create( url );
        //Optional (if authorization is Digest)
        req.ProtocolVersion = HttpVersion.Version10;
        draw = true;
        req.Timeout = 5000;
        req.Credentials = new NetworkCredential("username", "password");
        // get response
        WebResponse resp = req.GetResponse();
        
        // get response stream
        stream = resp.GetResponseStream();
        frame.material.color = Color.white;
        StartCoroutine (GetFrame ());
    }
 
    public void OnGUI() {
        if(draw == true)
            GUI.DrawTexture(new Rect(160, 10, 330, 300), texture);
     }

     IEnumerator GetFrame (){
        Byte [] JpegData = new Byte[65536];

        while(true) {
            int bytesToRead = FindLength(stream);
            if (bytesToRead == -1) {
                print("End of stream");
                yield break;
            }

            int leftToRead=bytesToRead;

            while (leftToRead > 0) {
                leftToRead -= stream.Read (JpegData, bytesToRead - leftToRead, leftToRead);
                yield return null;
            }

            ms = new MemoryStream(JpegData, 0, bytesToRead, false, true);

            texture.LoadImage (ms.GetBuffer ());
            frame.material.mainTexture = texture;
            frame.material.color = Color.white;
            stream.ReadByte(); // CR after bytes
            stream.ReadByte(); // LF after bytes
            ms.Close();
        }
    }

    int FindLength(Stream stream)  {
        int b;
        string line="";
        int result=-1;
        bool atEOL=false;

        while ((b=stream.ReadByte())!=-1) {
            if (b==10) continue; // ignore LF char
            if (b==13) { // CR
                if (atEOL) {  // two blank lines means end of header
                    stream.ReadByte(); // eat last LF
                    return result;
                }
                if (line.StartsWith("Content-Length:")) {
                    result=Convert.ToInt32(line.Substring("Content-Length:".Length).Trim());
                } else {
                    line="";
                }
                atEOL=true;
            } else {
                atEOL=false;
                line+=(char)b;
            }
        }
        return -1;
    }

    IEnumerator LoadFromWeb(string url){
        string urlBackup = "";
        Dictionary<string, string> content_info = null;

         if(isIP(url)){
            urlBackup = String.Copy(url);
            url = "https://this-page-intentionally-left-blank.org/";
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();
            if (webRequest.isNetworkError)
                Debug.Log("A network error happened");
            else {
                content_info = webRequest.GetResponseHeaders();
                // foreach (KeyValuePair<string, string> pair in content_info)
                // {
                //     Debug.Log("key : " + pair.Key + " and Value : " + pair.Value);
                // }
            }
        }

        DownloadHandlerTexture texDl = new DownloadHandlerTexture(true);

        UnityWebRequest wr = new UnityWebRequest(url);
        wr.downloadHandler = texDl;
        yield return wr.SendWebRequest();

        if (!(wr.isNetworkError || wr.isHttpError))
        {
            bool isIPCamera = isIP(url);
            if (url.Contains("https://this-page-intentionally-left-blank.org/")){

                url = "";
                url = String.Copy(urlBackup);
                GetVideo(url);

            }

        }


     }
     bool isIP( string url ){ 
        var checkURL = url.Replace("http://", "");
        checkURL = checkURL.Replace("https://", "");
        if(checkURL.Contains("mjpg") || checkURL.Contains("mpg")) return true;
        checkURL = checkURL.Split('/')[0];
        checkURL = checkURL.Split(':')[0];
        var match = Regex.Match(checkURL, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
        if(match.Success) return true;
        else return false;
    }
}
