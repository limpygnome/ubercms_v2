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
        // Rewrite path to Default.aspx
        System.Web.HttpContext.Current.RewritePath(Request.ApplicationPath + "Default.aspx?path=" + path, true);
#endif
    }
</script>
