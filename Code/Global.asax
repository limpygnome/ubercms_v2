<%@ Application Language="C#" %>
<script runat="server">
    void Application_Start(object sender, EventArgs e) 
    {
    	CMS.Base.Core.start();
    }
    void Application_End(object sender, EventArgs e) 
    {
    	CMS.Base.Core.stop();
    }
    void Application_BeginRequest(object sender, EventArgs e)
    {
#if IIS
		string path = System.Web.HttpContext.Current.Request.Path;
        // Check if it's just the main-page
        if(path == "/Default.aspx")
            path = "";
		// Check for excluded directories
		else if(path.StartsWith("/content") || path.StartsWith("/mirror") || path.StartsWith("/install") || path.StartsWith("favicon.ico"))
			return;
        // Append query-string data
        string qs = System.Web.HttpContext.Current.Request.QueryString.ToString();
        if(qs.Length > 0)
            path += "&" + qs;
        // Rewrite path to Default.aspx
        System.Web.HttpContext.Current.RewritePath(Request.ApplicationPath + "Default.aspx?path=" + path, true);
#endif
#if APACHE
        string path = System.Web.HttpContext.Current.Request.QueryString["path"];
        int cind = path.IndexOf('?');
        if (cind > 0 && cind < path.Length - 1)
            System.Web.HttpContext.Current.RewritePath("Default.aspx?path=" + path.Substring(0, cind) + "&" + path.Substring(cind+1), true);
#endif
    }
</script>
