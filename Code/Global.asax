<%@ Application Language="C#" %>
<script runat="server">
    void Application_Start(object sender, EventArgs e) 
    {
    }
    void Application_End(object sender, EventArgs e) 
    {
    }
    void Application_Error(object sender, EventArgs e) 
    {
    }
    void Application_BeginRequest(object sender, EventArgs e)
    {
#if !APACHE
		string path = System.Web.HttpContext.Current.Request.Path;
		// Check for excluded directories
		if(path.StartsWith("/content") || path.StartsWith("/mirror") || path.StartsWith("/install") || path.StartsWith("favicon.ico"))
			return;
        // Rewrite path to Default.aspx
        System.Web.HttpContext.Current.RewritePath("Default.aspx?path=" + path, true);
#endif
    }
</script>
