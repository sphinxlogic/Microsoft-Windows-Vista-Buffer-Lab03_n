namespace System.Web { 
    using System;
    using System.Web;
    using System.Web.Util;
    using System.Web.Configuration; 

 
    // 
    // Module that implements the UrlMappings functionality
    // on IIS 7 in integrated mode, this takes the place of 
    // the UrlMappings execution step and is listed in <modules/>
    sealed internal class UrlMappingsModule : IHttpModule {

        internal UrlMappingsModule() {} 

        public void Init(HttpApplication application) { 
                bool urlMappingsEnabled = false; 
                UrlMappingsSection urlMappings = RuntimeConfig.GetConfig().UrlMappings;
                urlMappingsEnabled = urlMappings.IsEnabled && ( urlMappings.UrlMappings.Count > 0 ); 

                if (urlMappingsEnabled) {
                    application.BeginRequest += new EventHandler(OnEnter);
                } 
        }
 
        public void Dispose() {} 

        internal void OnEnter(Object source, EventArgs eventArgs) { 

            HttpApplication app = (HttpApplication) source;
            UrlMappingsSection urlMappings = RuntimeConfig.GetAppConfig().UrlMappings;
 
            // First check RawUrl
            string mappedUrl = urlMappings.HttpResolveMapping(app.Request.RawUrl); 
 
            // Check Path if not found
            if (mappedUrl == null) 
                mappedUrl = urlMappings.HttpResolveMapping(app.Request.Path);

            if (!string.IsNullOrEmpty(mappedUrl)) //&& IsDifferentFromCurrentUrl(mappedUrl, app.Context))
                app.Context.RewritePath(mappedUrl, false); 
        }
 
//         private static bool IsDifferentFromCurrentUrl(string url, HttpContext context) 
//         {
//             Uri absUri; 
//             if (!Uri.TryCreate(url, UriKind.Absolute, out absUri))
//             {
//                 if (url.StartsWith("~/"))
//                     url = UrlPath.Combine(context.Request.Path, url.Substring(2)); 
//                 if (!Uri.TryCreate(context.Request.Url, url, out absUri))
//                     return true; 
//             } 
//             return Uri.Compare(absUri, context.Request.Url, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) != 0;
//         } 
    }
}

 
namespace System.Web { 
    using System;
    using System.Web;
    using System.Web.Util;
    using System.Web.Configuration; 

 
    // 
    // Module that implements the UrlMappings functionality
    // on IIS 7 in integrated mode, this takes the place of 
    // the UrlMappings execution step and is listed in <modules/>
    sealed internal class UrlMappingsModule : IHttpModule {

        internal UrlMappingsModule() {} 

        public void Init(HttpApplication application) { 
                bool urlMappingsEnabled = false; 
                UrlMappingsSection urlMappings = RuntimeConfig.GetConfig().UrlMappings;
                urlMappingsEnabled = urlMappings.IsEnabled && ( urlMappings.UrlMappings.Count > 0 ); 

                if (urlMappingsEnabled) {
                    application.BeginRequest += new EventHandler(OnEnter);
                } 
        }
 
        public void Dispose() {} 

        internal void OnEnter(Object source, EventArgs eventArgs) { 

            HttpApplication app = (HttpApplication) source;
            UrlMappingsSection urlMappings = RuntimeConfig.GetAppConfig().UrlMappings;
 
            // First check RawUrl
            string mappedUrl = urlMappings.HttpResolveMapping(app.Request.RawUrl); 
 
            // Check Path if not found
            if (mappedUrl == null) 
                mappedUrl = urlMappings.HttpResolveMapping(app.Request.Path);

            if (!string.IsNullOrEmpty(mappedUrl)) //&& IsDifferentFromCurrentUrl(mappedUrl, app.Context))
                app.Context.RewritePath(mappedUrl, false); 
        }
 
//         private static bool IsDifferentFromCurrentUrl(string url, HttpContext context) 
//         {
//             Uri absUri; 
//             if (!Uri.TryCreate(url, UriKind.Absolute, out absUri))
//             {
//                 if (url.StartsWith("~/"))
//                     url = UrlPath.Combine(context.Request.Path, url.Substring(2)); 
//                 if (!Uri.TryCreate(context.Request.Url, url, out absUri))
//                     return true; 
//             } 
//             return Uri.Compare(absUri, context.Request.Url, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) != 0;
//         } 
    }
}

 
