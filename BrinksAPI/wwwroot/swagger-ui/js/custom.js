var callback = function() {
    var elem = document.createElement("div");
    elem.innerHTML =
        "<div style=\"font-family: Titillium Web,sans-serif; color:white;padding-top: 10px;padding-left:80px;float:left;font-size:30px;\">CENTRAD</div>";

    document.body.insertBefore(elem, document.body.firstChild);
};

if (document.readyState === "complete" || (document.readyState !== "loading" && !document.documentElement.doScroll)) {
    callback();
} else {
    document.addEventListener("DOMContentLoaded", callback);
}