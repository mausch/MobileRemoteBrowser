<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<FilesModel>" %>
<%@ Import Namespace="System.IO"%>

<%@ Import Namespace="ServerFileBrowser.Models" %>
<html>
<head>
    <meta name="viewport" content="width=480; initial-scale=0.6666; maximum-scale=1.0; minimum-scale=0.6666" />
    <title><%= Html.Encode(Model.CurrentDirectory)%></title>
    <style>
        body 
        {
            font-size: 150%;
        }
        ul 
        {
            list-style-type: none;
            width: 100%;
            padding: 0px;
        }
        ul li 
        {
            width: 100%;
            height: 50px;
        }
        a
        {
            display: block;
            padding: 15px;
            background-color: #bbb;
            border: solid 3px black;
        }
        .dir 
        {
            background-color: #E1BB7E;
        }
    </style>
</head>
<body>
    <h2><%= Html.Encode(Model.CurrentDirectory) %></h2>
    <ul>
        <li>
            <a class="dir" href="<%= Url.Action("Index", new {path = Directory.GetParent(Model.CurrentDirectory)}) %>">..</a> 
        </li>
        <% foreach (var d in Model.Directories) { %>
            <li>
                <a class="dir" href="<%= Url.Action("Index", new {path = Path.Combine(Model.CurrentDirectory, d)}) %>"><%= Html.Encode(d) %></a> 
            </li>
        <% } %>
        
        <% foreach (var f in Model.Files) { %>
            <li>
                <a class="file" href="<%= Url.Action("Run", new {path = Model.CurrentDirectory, file = f}) %>"><%= Html.Encode(f) %></a>
            </li>
        <% } %>
    </ul>
</body>
</html>
