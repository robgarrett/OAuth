
// Put the application ID here, or paste into the textbox.
var clientId = "";

clientId = clientId || localStorage.getItem("clientId");
document.getElementById("clientId").value = clientId;

if (document.location.hash && document.location.hash.length > 1) {
    parseHash();
}

function startAutodiscovery() {
    clientId = document.getElementById("clientId").value || clientId;
    localStorage.setItem("clientId", clientId);
    // Start with the initial hub url and get the real one to use.
    const initialHubUrl = "https://webdir.online.lync.com/autodiscover/autodiscoverservice.svc/root";
    get(initialHubUrl, function (xmlhttp) {
        localStorage.setItem("client_id", clientId);
        const nextUrl = JSON.parse(xmlhttp.responseText)._links.self.href;
        getHubUrl(nextUrl);
    });
}

function getHubUrl(link) {
    // Get the hub url from the passed link.
    const hubUrl = link.substr(0, link.indexOf("Autodiscover/"));
    localStorage.setItem("hubUrl", hubUrl);
    // Authenticate.
    const currentUrl = [location.protocol, "//", location.host, location.pathname].join("");
    const secret = Math.random().toString(36).substr(2, 10);
    localStorage.setItem("secret", secret);
    var url = "https://login.microsoftonline.com/common/oauth2/authorize?";
    url += "response_type=token&client_id=" + clientId;
    url += "&redirect_uri=" + currentUrl.toLowerCase();
    url += "&resource=" + hubUrl;
    url += "&state=" + secret;
    document.location.href = url;
}

// Issue http request.
function get(url, callback) {
    var xmlhttp = new XMLHttpRequest();
    xmlhttp.onreadystatechange = function() {
        if (xmlhttp.readyState === XMLHttpRequest.DONE) {
            callback(xmlhttp);
        }
    }
    xmlhttp.open("GET", url, true);
    xmlhttp.send();
}

function post(url, data, callback) {
    var xmlhttp = new XMLHttpRequest();
    xmlhttp.onreadystatechange = function () {
        if (xmlhttp.readyState === XMLHttpRequest.DONE) {
            callback(xmlhttp);
        }
    }
    xmlhttp.open("POST", url, true);
    xmlhttp.setRequestHeader("Content-type", "application/json");
    xmlhttp.send(JSON.stringify(data));
}

function parseHash() {
    // We've received the access token.
    var accessToken = "";
    var secretReturned = "";
    var error = "";
    var errorDescription = "";
    const parts = document.location.hash.substr(1).split("&");
    for (let i = 0; i < parts.length; i++) {
        const p = parts[i];
        if (p.indexOf("access_token=") === 0) {
            accessToken = p.substr("access_token=".length);
        } else if (p.indexOf("state=") === 0) {
            secretReturned = p.substr("state=".length);
        } else if (p.indexOf("error=") === 0) {
            error = p.substr("error=".length);
        } else if (p.indexOf("error_description=") === 0) {
            errorDescription = p.substr("error_description=".length);
        }
    }
    // Make sure secrets match.
    const secret = localStorage.getItem("secret");
    localStorage.removeItem("secret");
    if (!error && secret !== secretReturned) {
        error = "Secret mismatch!";
    } 
    if (error) {
        alert(error + ": " + decodeURIComponent(errorDescription));
    } else {
        continueAutodiscovery(accessToken);
    }
}

function continueAutodiscovery(accessToken) {
    const appUrl = localStorage.getItem("applicationsUrl");
    if (appUrl) {
        localStorage.removeItem("applicationsUrl");
        initializeSession(accessToken);
        return;
    }

    // We have the access token now, so let's get the skype applications URL.
    const hubUrl = localStorage.getItem("hubUrl");
    const url = hubUrl + "autodiscover/autodiscoverservice.svc/root/oauth/user";
    var proxyUrl = [location.protocol, "//", location.host, "/Proxy.aspx"].join("");
    proxyUrl += "?url=" + url + "&access_token=" + accessToken;
    get(proxyUrl, function(xmlhttp) {
        if (xmlhttp.status === 200) {
            const response = JSON.parse(xmlhttp.responseText);
            if (response._links.redirect) {
                getHubUrl(response._links.redirect.href);
            } else if (response._links.applications) {
                localStorage.setItem("applicationsUrl", response._links.applications.href);
                getHubUrl(response._links.self.href);
            } else {
                throw Error("Unexpected error");
            }
        } else {
            throw Error("Error " + xmlhttp.status);
        }
    });
}

function initializeSession(accessToken) {
    const hubUrl = localStorage.getItem("hubUrl");
    const url = hubUrl + "ucwa/oauth/v1/applications";
    const randomGuid = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g,
        function(c) {
            const r = Math.random() * 16 | 0;
            const v = c === "x" ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    const initData = {
        UserAgent: "SkypeTest",
        Culture: "en-US",
        EndpointId: randomGuid
    };

    // Post to the proxy.
    var proxyUrl = [location.protocol, "//", location.host, "/Proxy.aspx"].join("");
    proxyUrl += "?url=" + url + "&access_token=" + accessToken;
    post(proxyUrl, initData, function(xmlhttp) {
        if (xmlhttp.status === 403) {
            throw Error(xmlhttp.getResponseHeader("X-Ms-diagnostics"));
        } else if (xmlhttp.status === 200) {
            const response = JSON.parse(xmlhttp.responseText);
            console.log(response);
            alert("OAuth with Skype successful.");
        } else {
            throw Error(xmlhttp.status);
        }
    });
}
