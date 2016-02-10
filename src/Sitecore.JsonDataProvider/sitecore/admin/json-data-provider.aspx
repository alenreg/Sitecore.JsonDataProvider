<%@ Page Language="C#" %>

<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="Sitecore" %>
<%@ Import Namespace="Sitecore.Data.DataProviders" %>
<%@ Import Namespace="Sitecore.Data.Mappings" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>Sitecore.JsonDataProvider maintenance page</title>
  <script runat="server">
    public void Page_Init(object o, EventArgs e)
    {
      if (!Sitecore.Context.User.IsAdministrator)
      {
        Response.Redirect("/sitecore/admin/login.aspx");

        return;
      }

      var downloadEncoded = this.Request.QueryString["download"];
      if (!string.IsNullOrEmpty(downloadEncoded))
      {
        var download = this.Server.UrlDecode(downloadEncoded);
        foreach (var fileMapping in JsonDataProvider.Instances.Values.SelectMany(x => x.Mappings).OfType<IFileMapping>())
        {
          if (fileMapping == null)
          {
            continue;
          }

          var filePath = fileMapping.FilePath;
          if (!filePath.Equals(download, StringComparison.OrdinalIgnoreCase))
          {
            continue;
          }

          this.Response.ContentType = "text/json";
          this.Response.TransmitFile(filePath);
          this.Response.End();
        }
      }
    }

    private void Reload([CanBeNull] object sender, [CanBeNull] EventArgs e)
    {
      foreach (var mapping in JsonDataProvider.Instances.Values.SelectMany(x => x.Mappings))
      {
        mapping.Initialize();
      }
    }

    private void Commit([CanBeNull] object sender, [CanBeNull] EventArgs e)
    {
      foreach (var mapping in JsonDataProvider.Instances.Values.SelectMany(x => x.Mappings))
      {
        mapping.Commit();
      }
    }
  </script>
</head>
<body>
  <form id="form1" runat="server">
    <div>
      <h1>Sitecore.JsonDataProvider</h1>

      <% foreach (var pair in JsonDataProvider.Instances)
        {
      %><div>
        <h3><%= pair.Key %></h3>
        <table cellspacing="4px" cellpadding="8px" style="background: lightgrey">
          <thead>
            <tr style="font-weight: bold;">
              <td>Type</td>
              <td>Items</td>
              <td>ReadOnly</td>
              <td>FilePath</td>
              <td>MediaFolder</td>
            </tr>
          </thead>
          <tbody>
            <% foreach (var mapping in pair.Value.Mappings)
              {
            %><tr>
              <td><%= mapping.GetType().Name %></td>
              <td><%= mapping.ItemsCount %></td>
              <td><%= mapping.ReadOnly ? "ReadOnly" : "ReadWrite" %></td>
              <td><%= mapping is IFileMapping ? (File.Exists(((IFileMapping)mapping).FilePath) ? string.Format("<a href=\"json-data-provider.aspx?download={0}\">{1}</a>", this.Server.UrlEncode(((IFileMapping)mapping).FilePath), ((IFileMapping)mapping).FilePath) : ((IFileMapping)mapping).FilePath) : "" %></td>
              <td><%= mapping is IFileMapping ? ((IFileMapping)mapping).MediaFolderPath : "" %></td>
            </tr>
            <% } %>
          </tbody>
        </table>
      </div>
      <% } %>
      <br />
      <h3>Control Panel</h3>
      <asp:Button runat="server" OnClick="Reload" Text="Reload" />
      <asp:Button runat="server" OnClick="Commit" Text="Commit" />
    </div>
  </form>
</body>
</html>
