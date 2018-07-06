<%@ Page Language="C#" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Skype Test</title>
    <style type="text/css">
        .btn {
            color: #ffffff;
            background-color: #14a7ed;
            border-color: #04a4ed;
            display: inline-block;
            padding: 6px 12px;
            margin: 0;
            font-size: 20px;
            font-weight: normal;
            line-height: 1.4;
            text-align: center;
            white-space: nowrap;
            vertical-align: middle;
            cursor: pointer;
            background-image: none;
            border: 1px solid transparent;
            border-radius: 4px;
            text-decoration: none;
        }
    </style>
</head>
<body>
    <p>Code in this demo influenced from <a href="https://github.com/andrei-markeev/skype4b">https://github.com/andrei-markeev/skype4b</a></p>
    <form id="MainForm" runat="server">
        <div>
            <p><b>Instructions:</b></p>
            <ol>
                <li>Create an application in Azure AD.</li>
                <li>Give the sign on URL as the page hosting the button, such as this one.</li>
                <li>Set the APP ID to your tenant URL to https://<i>tenant</i>.onmicrosoft.com/<i>uniqueName</i>.</li>
                <li>Set permissions for Skype for Business.</li>
                <li>Delegate permissions to allow read/write of Skype permissions.</li>
                <li>Set the return URLs to include the FQDN for this page.</li>
                <li>Edit the manifest file and set the oauth2AllowImplicitFlow to true.</li>
                <li>Set multi-tenancy if allowing users outside of your tenant to use this app.</li>
            </ol>
            <p>
                <label>Enter Client ID of app in AAD:</label>
                <input id="clientId" type="text" />
            </p>
            <a href="javascript:void(0)" class="btn" onclick="startAutodiscovery();">Log in with Skype for Business</a>
        </div>
    </form>
    <script src="auth.js" type="text/javascript"></script>
</body>
</html>
