using System;
using System.Diagnostics;
using System.Web;

namespace HttpModule
{
    public class TimingModule : IHttpModule
    {
        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;
            context.EndRequest += OnEndRequest;
        }

        void OnBeginRequest(object sender, System.EventArgs e)
        {
            if (HttpContext.Current.Request.IsLocal && HttpContext.Current.Response.ContentType == "text/html" && HttpContext.Current.IsDebuggingEnabled)
            {
                var stopwatch = new Stopwatch();
                HttpContext.Current.Items["Stopwatch"] = stopwatch;
                stopwatch.Start();
            }
        }

        void OnEndRequest(object sender, System.EventArgs e)
        {
            if (HttpContext.Current.Request.IsLocal && HttpContext.Current.Response.ContentType == "text/html" && HttpContext.Current.IsDebuggingEnabled)
            {
                Stopwatch stopwatch =
                  (Stopwatch)HttpContext.Current.Items["Stopwatch"];
                stopwatch.Stop();

                TimeSpan ts = stopwatch.Elapsed;
                string elapsedTime = String.Format("{0}ms", ts.TotalMilliseconds);

                var httpContext = ((System.Web.HttpApplication)sender).Context;
            
                httpContext.Response.Write("<p><b>" + elapsedTime + "</b></p>");
            }
        }
    }
}