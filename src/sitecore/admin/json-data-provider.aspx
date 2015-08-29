<%@ Page Language="C#" %>
<%@ Import Namespace="Sitecore" %>
<%@ Import Namespace="Sitecore.Data.DataProviders" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Sitecore.JsonDataProvider maintenance page</title>
  <script runat="server">

    private void Reload([CanBeNull] object sender, [CanBeNull] EventArgs e)
    {
      foreach (var mapping in JsonDataProvider.Instances.SelectMany(x => x.FileMappings))
      {
        mapping.Initialize();
      }
    }

    private void Commit([CanBeNull] object sender, [CanBeNull] EventArgs e)
    {
      foreach (var mapping in JsonDataProvider.Instances.SelectMany(x => x.FileMappings))
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
      
      <span>JsonDataProvider.Instances: <%= JsonDataProvider.Instances.Count %></span>
      <asp:Button runat="server" OnClick="Reload" Text="Reload"/>
      <asp:Button runat="server" OnClick="Commit" Text="Commit" />
    </div>
    </form>
</body>
</html>
