<%@ Page Language="C#" %>
<%@ Import Namespace="Sitecore" %>
<%@ Import Namespace="Sitecore.Data.DataProviders" %>
<%@ Import Namespace="Sitecore.Data.Mappings" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Sitecore.JsonDataProvider maintenance page</title>
  <script runat="server">

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
           %><div><h3><%= pair.Key %></h3>
             <table cellspacing="4px" cellpadding="8px" style="background: lightgrey">
               <thead>
               <tr style="font-weight: bold;">
                 <td>Type</td><td>Items</td><td>ReadOnly</td><td>FilePath</td><td>MediaFolder</td>
               </tr>
               </thead>
               <tbody>
        <% foreach (var mapping in pair.Value.Mappings)
           {
             %><tr><td><%= mapping.GetType().Name %></td><td><%= mapping.ItemsCount %></td><td><%= mapping.ReadOnly ? "ReadOnly" : "ReadWrite" %></td><td><%= mapping is IFileMapping ? ((IFileMapping)mapping).FilePath : "" %></td><td><%= mapping is IFileMapping ? ((IFileMapping)mapping).MediaFolderPath : "" %></td></tr>
        <% } %>
      </tbody></table></div>
      <% } %>
      <br />
      <h3>Control Panel</h3>
      <asp:Button runat="server" OnClick="Reload" Text="Reload"/>
      <asp:Button runat="server" OnClick="Commit" Text="Commit" />
    </div>
    </form>
</body>
</html>
