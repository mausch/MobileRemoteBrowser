<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<FilesModel>" %>
<%@ Import Namespace="MobileRemoteBrowser"%>
<%@ Import Namespace="MvcContrib.UI.Pager"%>
<%@ Import Namespace="System.IO"%>
<%@ Import Namespace="MobileRemoteBrowser.Models" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html>
<head>
    <meta name="viewport" content="width=480; initial-scale=0.6666; maximum-scale=1.0; minimum-scale=0.6666" />
    <title><%= Html.Encode(Model.CurrentDirectory)%></title>
    <link href="/Content/Site.css" rel="Stylesheet" type="text/css"/>
</head>
<body>
    <h2><%= Html.Encode(Model.CurrentDirectory) %></h2>
    <a href="<%= Url.Action("KillVLC") %>">Kill VLC</a>
    <ul>
        <% if (Model.CurrentDirectory != null) { %>
        <li>
            <a class="dir" href="<%= Url.Action("Folder", new {path = Directory.GetParent(Model.CurrentDirectory)}) %>">..</a> 
        </li>
        <% } %>
        <% foreach (var d in Model.Files) { %>
            <li>
                <a class="<%= d.Type %>" href="<%= Url.FileAction(Model.CurrentDirectory, d) %>"><%= Html.Encode(d.Name) %></a> 
            </li>
        <% } %>
    </ul>
    <%= Html.Pager(Model.Files) %>
</body>
</html>
