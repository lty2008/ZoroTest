using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace example
{
    class HttpHelper
    {
        public static async Task<string> HttpGet(string url)
        {
            WebClient wc = new WebClient();
            return await wc.DownloadStringTaskAsync(url);
        }
        public static async Task<string> HttpPost(string url, byte[] data)
        {
            WebClient wc = new WebClient();
            wc.Headers["content-type"] = "text/plain;charset=UTF-8";
            byte[] retdata = await wc.UploadDataTaskAsync(url, "POST", data);
            return System.Text.Encoding.UTF8.GetString(retdata);
        }
        public static string MakeRpcUrlPost(string url, string method, out byte[] data, params MyJson.IJsonNode[] _params)
        {
            //if (url.Last() != '/')
            //    url = url + "/";
            var json = new MyJson.JsonNode_Object();
            json["id"] = new MyJson.JsonNode_ValueNumber(1);
            json["jsonrpc"] = new MyJson.JsonNode_ValueString("2.0");
            json["method"] = new MyJson.JsonNode_ValueString(method);
            StringBuilder sb = new StringBuilder();
            var array = new MyJson.JsonNode_Array();
            for (var i = 0; i < _params.Length; i++)
            {

                array.Add(_params[i]);
            }
            json["params"] = array;
            data = System.Text.Encoding.UTF8.GetBytes(json.ToString());
            return url;
        }
        public static string MakeRpcUrl(string url, string method, params MyJson.IJsonNode[] _params)
        {
            StringBuilder sb = new StringBuilder();
            if (url.Last() != '/')
                url = url + "/";

            sb.Append(url + "?jsonrpc=2.0&id=1&method=" + method + "&params=[");
            for (var i = 0; i < _params.Length; i++)
            {
                _params[i].ConvertToString(sb);
                if (i != _params.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");
            return sb.ToString();
        }
        public static async Task<Dictionary<string, List<UTXO>>> GetUTXOByAddress(string address)
        {
            Dictionary<string, List<UTXO>> dir = new Dictionary<string, List<UTXO>>();
            var path = "C:\\Users\\tlan\\NEO\\dumpdata";

            foreach (string filePath in Directory.GetFiles(path + "/utxo"))
            {
                if (filePath.Contains(address))
                {
                    JObject jObject = JObject.Parse(File.ReadAllText(filePath));

                    UTXO utxo = new UTXO(jObject["addr"].ToString(),
                        new ThinNeo.Hash256(jObject["txid"].ToString()),
                        jObject["asset"].ToString(),
                        decimal.Parse(jObject["value"].ToString()),
                        int.Parse(jObject["n"].ToString()));
                    if (dir.ContainsKey(jObject["asset"].ToString()))
                    {
                        if (jObject["used"].ToString() == "0")
                        {
                            dir[jObject["asset"].ToString()].Add(utxo);
                        }
                    }
                    else
                    {
                        List<UTXO> l = new List<UTXO>();
                        l.Add(utxo);
                        dir[jObject["asset"].ToString()] = l;
                    }
                }
            }
            return dir;
        }


    }


    class Helper
    {


    }
}
