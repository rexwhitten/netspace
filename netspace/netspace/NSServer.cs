using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using netspace.AccessObjects;

namespace netspace
{
    public partial class NSServer
    {
        #region [ Properties ] 
        public String ListenPath { get; private set; }
        public Int32 Port { get; private set; }
        public String FilePath { get; private set; }  
        #endregion
        
        #region [ Constructors ]
        public NSServer()
        {

        }

        public NSServer(String ListenOnPath, Int32 Port, String FilePath)
        {
            this.ListenPath = ListenOnPath;
            this.Port = Port;
            this.FilePath = FilePath;
        }
        #endregion  

    }

     public static class NSServerExtensions
    {
        #region [ Basic Servers ]
        /// <summary>
        /// Basic Http Server (Static Content) 
        /// </summary>
        public static void CreateHttpServer(this NSServer ns)
        {
            Thread t = new Thread(new ThreadStart(() =>
            {
                Boolean active = true;
                HttpListener l = new HttpListener();
                l.Prefixes.Add(String.Format("http://{0}:{1}/", ns.ListenPath, ns.Port));
                l.Start();

                while (active)
                {
                    IAsyncResult result = l.BeginGetContext(new AsyncCallback((asyResult) =>
                    {
                        HttpListener listener = (HttpListener)asyResult.AsyncState;
                        HttpListenerContext context = listener.EndGetContext(asyResult);
                        HttpListenerRequest request = context.Request;
                        HttpListenerResponse response = context.Response;
                        String responseString = "{}";

                        var url_query = request.Url;
                        string dataset_name = url_query.AbsolutePath.Replace("/","");
                        string dataset_file_name = String.Format(@"Cache\{0}.data", dataset_name);

                        if (url_query.AbsolutePath.Contains(".ico"))
                        {
                            return;
                        }


                        if (File.Exists(dataset_file_name))
                        {
                            var dataset_file = new FileInfo(dataset_file_name);

                            if ((DateTime.Now - dataset_file.LastWriteTime).Minutes >= 10)
                            {
                                File.Delete(dataset_file.FullName);
                                DataAccess access = new DataAccess(true,"default");
                                var data_result = access.ExecuteSql(string.Format("SELECT * FROM {0}", dataset_name));
                                responseString = Newtonsoft.Json.JsonConvert.SerializeObject(data_result);
                                access.Dispose();
                            }
                            else
                            {
                                responseString = File.ReadAllText(dataset_file_name);
                            }
                        }
                        else
                        {
                            // Get the Data and Save it then return it 
                            DataAccess access = new DataAccess(true, "default");
                            var data_result = access.ExecuteSql(string.Format("SELECT * FROM {0}", dataset_name));
                            responseString = Newtonsoft.Json.JsonConvert.SerializeObject(data_result);
                            access.Dispose();
                            File.WriteAllText(dataset_file_name, responseString);
                        }

    
                        Byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                        response.ContentLength64 = buffer.Length;
                        response.ContentType = "application/json";
                        System.IO.Stream output = response.OutputStream;
                        output.Write(buffer, 0, buffer.Length);
                        output.Flush();
                        output.Close();

                    }), l);
                    result.AsyncWaitHandle.WaitOne();
                }
            }));
            t.Start();
        }
        #endregion


        #region [ Utilities ]
        public static void Apply(this String[] lines, Action<String> applicator)
        {
            foreach (String line in lines)
            {
                applicator.Invoke(line);
            }
        }
        /// <summary>
        /// Remove Blank Strings from a set of Strings
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static string[] Clean(this string[] lines)
        {
            List<String> result = new List<string>();

            foreach (string line in lines)
            {
                if (line.Length > 0)
                {
                    if (line.Replace(" ", "").Length > 0)
                    {
                        result.Add(line);
                    }
                }
            }

            return result.ToArray();
        }

        public static string Flatten(this string[] lines)
        {
            string result = "";

            foreach (string line in lines)
            {
                result = result + String.Format("{0}\n", line);
            }

            return result;
        }

        public static string Flatten(this IEnumerable<String> lines)
        {
            string result = "";

            foreach (string line in lines)
            {
                result = result + String.Format("{0}\n", line);
            }

            return result;
        }

        public static void ActOn<T>(this IEnumerable<T> items, Action<T> actor)
        {
            foreach (T item in items)
            {
                actor.Invoke(item);
            }
        }

        public static IEnumerable<R> Apply<T, R>(this IEnumerable<T> items, Func<T, R> functor)
        {
            List<R> results = new List<R>();

            foreach (T item in items)
            {
                results.Add(functor.Invoke(item));
            }

            return results;
        }

        public static Boolean Try<T>(this T parent, Action actor)
        {
            Boolean result = false;

            try
            {
                actor.Invoke();
                result = true;
            }
            catch (Exception x)
            {
                result = false;
            }

            return result;
        }
        #endregion

        #region [ Http Request ] 
        #endregion

        #region [ Http Response ] 
        public static void Set(this HttpListenerResponse response, string data, string mime_type = "application/json")
        {
            Byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);
            response.ContentLength64 = buffer.Length;
            response.ContentType = mime_type;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
        }

        public static void Set(this HttpListenerResponse response, Object data)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);

            Byte[] buffer = System.Text.Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
        }

        #endregion
    }
}
