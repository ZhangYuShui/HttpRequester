using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Threading;
using System.Diagnostics;

namespace ScorpioHttpRequester {
    public partial class FormMain : Form {
        private static readonly Encoding Encode = Encoding.UTF8;
        private const string urls = "urls";                     //访问列表
        private const string types = "types";                   //content 类型
        private const string encodeRequest = "encodeRequest";   //post请求数据编码
        private const string encodeResult = "encodeResult";     //返回数据编码
        private List<string> m_Urls = new List<string>();
        private List<string> m_ContentBaseTypes = new List<string>() {
            "text/plain",
            "text/html",
            "application/json",
            "application/xml",
            "application/rdf+xml",
            "application/x-www-form-urlencoded",
        };
        class EncodeData {
            private string encodingName;
            private Encoding _encoding;
            public EncodeData(string name) {
                _encoding = Encoding.GetEncoding(name);
                encodingName = encoding.EncodingName;
            }
            public Encoding encoding { get { return _encoding; } }
            public override string ToString() {
                return encodingName;
            }
        }
        private List<EncodeData> m_Encoders = new List<EncodeData>() {
            new EncodeData("UTF-8"),
            new EncodeData("US-ASCII"),
            new EncodeData("unicodeFFFE"),
            new EncodeData("GB2312"),
            new EncodeData("UTF-7"),
            new EncodeData("UTF-16"),
            new EncodeData("UTF-32"),
        };


        private List<string> m_ContentTypes = new List<string>();
        private Encoding m_EncodingRequest;
        private Encoding m_EncodingResult;

        public FormMain() {
            InitializeComponent();
            Util.Init(this);
        }
        private void Form1_Load(object sender, EventArgs e) {
            comboEncodeRequest.Items.AddRange(m_Encoders.ToArray());
            comboEncodeRequest.SelectedIndex = localStroage.has(encodeRequest) ? Convert.ToInt32(localStroage.get(encodeRequest)) : 0;
            comboEncodeResult.Items.AddRange(m_Encoders.ToArray());
            comboEncodeResult.SelectedIndex = localStroage.has(encodeResult) ? Convert.ToInt32(localStroage.get(encodeResult)) : 0;

            tabControl1.TabPages[0].Text = "Content";
            if (localStroage.has(urls)) m_Urls.AddRange(localStroage.get(urls).Split(';'));
            foreach (var url in m_Urls) { urlText.Items.Add(url); }
            if (urlText.Items.Count > 0) urlText.Text = (string)urlText.Items[0];
            if (localStroage.has(types)) {
                m_ContentTypes.AddRange(localStroage.get(types).Split(';'));
            } else {
                m_ContentTypes.AddRange(m_ContentBaseTypes);
            }
            foreach (var contenttype in m_ContentTypes) { contentTypeText.Items.Add(contenttype); }
            if (contentTypeText.Items.Count > 0) contentTypeText.Text = (string)contentTypeText.Items[0];
            if (File.Exists("./content")) {
                contentText.Text = Encode.GetString(File.ReadAllBytes("./content"));
            }
        }
        private void Save() {
            string url = urlText.Text;
            if (!urlText.Items.Contains(url)) { urlText.Items.Insert(0, url); }
            m_Urls.Remove(url);
            m_Urls.Insert(0, url);
            string contentType = contentTypeText.Text;
            if (!contentTypeText.Items.Contains(contentType)) { contentTypeText.Items.Insert(0, contentType); }
            m_ContentTypes.Remove(contentType);
            m_ContentTypes.Insert(0, contentType);
            localStroage.set(urls, string.Join(";", m_Urls.ToArray()));
            localStroage.set(types, string.Join(";", m_ContentTypes.ToArray()));
            File.WriteAllBytes("./content", Encode.GetBytes(contentText.Text));
        }
        private void SetData(HttpWebResponse response) {
            statusLabel.Text = "Response Status :" + (int)response.StatusCode + " " + response.StatusDescription;
            
            if (response.StatusCode >= HttpStatusCode.OK && response.StatusCode < HttpStatusCode.Ambiguous) {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("State : " + (int)response.StatusCode);
                builder.AppendLine("        Method : " + response.Method);
                builder.AppendLine("        Url : " + response.ResponseUri.ToString());
                builder.AppendLine("        CharacterSet : " + response.CharacterSet);
                builder.AppendLine("        ContentEncoding : " + response.ContentEncoding);
                builder.AppendLine("Headers :");
                var heads = response.Headers.AllKeys;
                foreach (var head in heads) {
                    builder.AppendLine("        " + head + " : " + response.Headers.Get(head));
                    if(head.Equals("Set-Cookie"))
                    {
                        this.cookieText.Text = response.Headers.Get(head);
                    }
                }
                statusText.Text = builder.ToString();
                resultText.Text = Util.toString(response.GetResponseStream(), m_EncodingResult);
            } else {
                statusText.Text = "请求失败  "+ (int)response.StatusCode;
                resultText.Text = "";
                resultText.Text = Util.toString(response.GetResponseStream(), m_EncodingResult);
            }

        }

        private void SetReqData(HttpWebRequest request)
        {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("State :");
                builder.AppendLine("        Method : " + request.Method);
                builder.AppendLine("        Url : " + request.RequestUri.ToString());
                builder.AppendLine("Headers :");
                var heads = request.Headers.AllKeys;
                foreach (var head in heads)
                {
                    builder.AppendLine("        " + head + " : " + request.Headers.Get(head));                   
                }
                this.reqText.Text = builder.ToString();
        }

       

        private void buttonGet_Click(object sender, EventArgs e) {
            string url = urlText.Text;
            if (string.IsNullOrEmpty(url)) {
                MessageBox.Show("请输入url");
                return;
            }
            if (!url.StartsWith("http"))
            {
                url = "http://" + url;
                urlText.Text = url;
            }
            Save();

            string cookie = this.cookieText.Text;
            string[] headers = this.reqHeaders.Lines;

            statusText.Text = "";
            resultText.Text = "";
            Thread thread = new Thread(() => {
                try {
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                    request.Timeout = 30000;                    //设定超时时间30秒

                    //添加cookie
                    request.Headers.Add("Cookie", cookie);
                  
                    //添加header
                    foreach (var header in headers)
                    {
                        string[] keyVals = header.Split('=');
                        if (keyVals != null && keyVals.Length == 2)
                        {
                            request.Headers.Add(keyVals[0], keyVals[1]);
                        }

                    }

                    HttpWebResponse response = null;
                    try {
                        response = (HttpWebResponse)request.GetResponse(); ;
                    } catch (WebException ex) {
                        response = (HttpWebResponse)ex.Response;
                    }
                    Util.Exec(() => { SetData(response); });
                    Util.Exec(() => { SetReqData(request); });
                    response.Close();
                } catch (Exception ex) {
                    Util.Exec(() => { resultText.Text = "请求出错 : " + ex.ToString(); });
                }
            });
            thread.Start();
        }
        private void buttonPost_Click(object sender, EventArgs e) {
            string url = urlText.Text;
            if (string.IsNullOrEmpty(url)) {
                MessageBox.Show("请输入url");
                return;
            }
            if (!url.StartsWith("http"))
            {
                url = "http://" + url;
                urlText.Text = url;
            }
            string content = contentText.Text; 
            string contentType = contentTypeText.Text;
            string cookie = this.cookieText.Text;
            string[] headers = this.reqHeaders.Lines;

            statusText.Text = "";
            resultText.Text = "";
            Save();
            Thread thread = new Thread(() => {
                try {
                    byte[] body = m_EncodingRequest.GetBytes(content);
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                    request.Timeout = 3000;                     //设定超时时间30秒
                    request.Method = "POST";                    //POST类型

                    //添加cookie
                    request.Headers.Add("Cookie", cookie);
                    /*
                    string[] cookies=cookie.Split(';');
                    foreach (var cook in cookies)
                    {
                        string[] keyVals =cook.Split('=');                 
                        if(keyVals!=null && keyVals.Length == 2)
                        {
                            request.CookieContainer.Add(new Cookie(keyVals[0],keyVals[1]));
                        }
                 
                    }*/
                    //添加header
                    foreach (var header in headers)
                    {
                        string[] keyVals = header.Split('=');
                        if (keyVals != null && keyVals.Length == 2)
                        {
                            request.Headers.Add(keyVals[0], keyVals[1]);
                        }

                    }
                    request.ContentType = contentType;          //ContentType
                    request.ContentLength = body.Length;        //发送数据长度
                    Stream stream = request.GetRequestStream(); //获得操作流
                    stream.Write(body, 0, body.Length);
                    stream.Close();
                    HttpWebResponse response = null;
                    try {
                        response = (HttpWebResponse)request.GetResponse();
                    } catch (WebException ex) {
                        response = (HttpWebResponse)ex.Response;
                    }
                    Util.Exec(() => { SetData(response); });
                    Util.Exec(() => { SetReqData(request); });
                    response.Close();
                } catch (Exception ex) {
                    Util.Exec(() => { resultText.Text = "请求出错 : " + ex.ToString(); });
                }
            });
            thread.Start();
        }

        private void buttonUrlEncode_Click(object sender, EventArgs e) {
            string text = resultText.Text;
            if (string.IsNullOrEmpty(text)) { return; }
            resultText.Text = Commons.Util.UriTranscoder.URLEncode(text, m_EncodingResult);
        }

        private void buttonUrlDecode_Click(object sender, EventArgs e) {
            string text = resultText.Text;
            if (string.IsNullOrEmpty(text)) { return; }
            resultText.Text = Commons.Util.UriTranscoder.URLDecode(text, m_EncodingResult);
        }

        private void MenuAbout_Click(object sender, EventArgs e) {
            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Process.Start("ScorpioUpdater.exe", version + " http://www.fengyuezhu.com/app.php?app=ScorpioHttpRequester http://www.fengyuezhu.com/project/ScorpioHttpRequester/");
        }
        private void comboEncodeRequest_SelectedIndexChanged(object sender, EventArgs e) {
            localStroage.set(encodeRequest, comboEncodeRequest.SelectedIndex.ToString());
            m_EncodingRequest = ((EncodeData)comboEncodeRequest.SelectedItem).encoding;
        }
        private void comboEncodeResult_SelectedIndexChanged(object sender, EventArgs e) {
            localStroage.set(encodeResult, comboEncodeResult.SelectedIndex.ToString());
            m_EncodingResult = ((EncodeData)comboEncodeResult.SelectedItem).encoding;
        }

        private void contentTypeText_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void contentText_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(this.resultText.Text);
        }
    }
}
