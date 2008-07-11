﻿<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="LatestArticles.ascx.cs" Inherits="N2.Templates.Wiki.UI.Parts.LatestArticles" %>
<asp:Repeater runat="server" ID="rptArticles" runat="server">
    <ItemTemplate>
        <div class="item"><a href="<%# Eval("Url") %>">
            <span class="date"><%# Eval("Published") %></span>
            <%# Eval("Title") %>, <%# Eval("SavedBy") %>
        </a>
        </div>
    </ItemTemplate>
</asp:Repeater>