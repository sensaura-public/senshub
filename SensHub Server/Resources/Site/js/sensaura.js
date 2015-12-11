//---------------------------------------------------------------------------
// Sensaura specific JS.
// NOTE: This should be minified before distribution
//---------------------------------------------------------------------------

//--- Globals
var activePage = "loader"
var debugMode = false;

//---------------------------------------------------------------------------
// Internal subscribe/unsubscribe for messages
//---------------------------------------------------------------------------

var nextIndex = 1;
var subscribers = { }
var subscriptions = { }

function internalSubscribe(topic, subscriber) {
  // Get a reference to use for unsubscribing
  ref = nextIndex.toString()
  ref = ref + 1;
  // Add it to the set
  subscribers[ref] = [ topic, subscriber ];
  if (topic in subscriptions)
    subscriptions[topic][ref] = true;
  else {
    subscriptions[topic] = { }
    subscriptions[topic][ref] = true;
    }
  return ref
  }

function internalUnsubscribe(ref) {
  if (ref in subscribers) {
    topic = subscribers[ref][0]
    delete subscribers[ref];
    if (topic in subscriptions) {
      if (ref in subscriptions[topic]) {
        delete subscriptions[topic][ref];
        if (Object.keys(subscriptions[topic]).length == 0)
          return topic
        }
      }
    }
  return null;
  }

//---------------------------------------------------------------------------
// Message handling and RPC processing
//---------------------------------------------------------------------------

var ws = null;
var seq = 0;
var pending = { };

function recv(data) {
  console.log("Received data (RPC or WS)");
  // Get the type of data
  if (!data.hasOwnProperty("type")) {
    console.log("Received untyped message, ignoring.");
    return;
    }
  if (data["type"] == "response") {
    // Method call response, find the matching callback and invoke it
    if (data.hasOwnProperty("sequence") && pending.hasOwnProperty(data["sequence"])) {
      callback = pending[data["sequence"]]
      if (callback)
        callback(data["success"], data["result"])
      delete pending[data["sequence"]]
      }
    }
  else if(data["type"] == "message") {
    // Incoming message
    if (!(data.hasOwnProperty("topic") && data.hasOwnProperty("message"))) {
      console.log("Message is missing a topic or a body, ignoring");
      return;
      }
    topic = data["topic"];
    message = data["message"];
    // Figure out who wants it
    subs = [ ];
    len = topic.length
    for (candidate in subscriptions) {
      if (candidate.slice(0, len) == topic) {
        for (subscriber in subscriptions[candidate])
          subs.push(subscriber);
        }
      }
    // Now dispatch the message
    for (var i=0; i<subs.length; i++) {
      if (subs[i] in subscribers)
        subscribers[subs[i]][1](topic, message);
      }
    }
  else
    console.log("Unsupported message type '" + data["type"] + "', ignoring.");
  }

function rpcResponse(data) {
  console.log("Got response via RPC");
  // Process method call responses first
  if (data.hasOwnProperty("response"))
    recv(data["response"]);
  // Process any messages
  for (msg in data["messages"])
    recv(msg);
  }

function rpcFailure(xhr, message, exception) {
  $("#server-message").text("Connection failure - " + message);
  $("#server-message").slideDown();
  }

function send(type, target, data, onComplete) {
  // Build up the actual object to send
  var value = { type: type };
  if (type == "request") {
    value['method'] = target;
    value['sequence'] = seq;
    value['arguments'] = data;
    pending[seq] = onComplete;
    seq = seq + 1;
    }
  else if (type == "message") {
    value['topic'] = target;
    value['payload'] = data;
    }
  // Now send it off
  if (ws != null)
    ws.send($.toJSON(value));
  else {
    $.ajax({
      type: "POST",
      url: "/api/",
      error: rpcFailure,
      data: $.toJSON(value),
      success: rpcResponse,
      dataType: "json",
      contentType: "application/json"
      });
    }
  }

// Publish a message
function message(topic, body, onComplete) {
  send("message", topic, body, onComplete);
  }

// Make a RPC call
function rpcCall(method, args, onComplete) {
  send("request", method, args, onComplete);
  }

function serverConnected(status, data) {
  if(!status)
    return;
  pageSetup()
  }

// Server initialisation
$(document).ready(
  function() {
    if (debugMode) {
      console.log("!! Debug mode is enabled, network setup will not occur.");
      return;
      }
// TODO: Reinstate websockets later
//    if ("WebSocket" in window) {
//      var wsURL = "ws://" + location.hostname + (location.port ? ':' + location.port : ' ') + "/api/";
//      var connection = new WebSocket(wsURL, [ 'senshub' ]);
//      connection.onopen = function() {
//        ws = connection;
//        ws.onmessage = function(e) { recv(e.data); };
//        $("#server-message").text("Connection established");
//        $("#server-message").slideDown();
//        }
//      connection.onerror = function(error) {
//        // Fall back to polling
//        rpcCall("Poll", { }, serverConnected);
//        }
//      }
//    else {
      // Fall back to polling
      rpcCall("Poll", { }, serverConnected);
//      }
    }
  );

//---------------------------------------------------------------------------
// Base API interface
//---------------------------------------------------------------------------

function startupError(message) {
  if (!$("#splash-error").is(":visible")) {
    $("#splash-error-message").text("An error occurred while connecting to the server.");
    $("#splash-loading").hide();
    $("#splash-error").show();
    }
  }

function subscribe(topic, subscriber, onComplete) {
  // Assume it is going to work and get a reference
  ref = internalSubscribe(topic, subscriber);
  // Make the call
  rpcCall(
    "Subscribe",
    {
      topic: topic
    },
    function(status, result) {
      if(!status)
        internalUnsubscribe(ref);
      if(onComplete)
        onComplete(status, result);
      }
    );
  }

function unsubscribe(ref) {
  topic = internalUnsubscribe(ref);
  if (topic != null) {
    // No more subscribers, tell the server to stop sending messages
    rpcCall("Unsubscribe", { topic: topic });
    }
  }

//---------------------------------------------------------------------------
// Utilities
//---------------------------------------------------------------------------

// jQuery extension for codemirror
(function($){$.fn.codemirror = function(options) {
  var result = this;
  var settings = $.extend( {
    'mode' : 'lua',
    'lineNumbers' : false,
    'runmode' : false
  }, options);
  this.each(function() {
    result = CodeMirror.fromTextArea(this, settings);
  });
  return result;
};})( jQuery );

// Perfom 'action' on every entry in obj
function foreach(obj, action) {
  for (var key in obj) {
    if (obj.hasOwnProperty(key))
      action(key, obj[key]);
    }
  }

function clone(obj) {
  result = { };
  foreach(obj, function(key, val) { result[key] = val });
  return result;
  }

//---------------------------------------------------------------------------
// Object updates
//---------------------------------------------------------------------------

function addPlugin(id, info) {
  // Rebuild the info object into something more suitable for the template
  plugin = clone(info);
  // Convert text fields to HTML with markdown
  if (plugin["Description"])
    plugin["Description"] = markdown.toHTML(plugin["Description"]);
  if (plugin["DetailedDescription"])
    plugin["DetailedDescription"] = markdown.toHTML(plugin["DetailedDescription"]);
  // TODO: Turn enabled from boolean to a checkbox style
  // Now add the plugin
  $widget = copyTemplate("template-plugin", id, plugin);
  $("#plugins").prepend($widget);
  }

function updatePlugin(id, info) {
  }

//---------------------------------------------------------------------------
// Initial page setup
//---------------------------------------------------------------------------

var serverState = null;

function updateState(state) {
  // Update the UI with the state information
  serverState = state;
  // Build the plugins page
  foreach(serverState["Plugin"], function(id, description) {
    console.log("Setting up plugin " + id);
    addPlugin(id, description);
    });
  }

function onServerNotification(topic, message) {
  // TODO: Display new notifications
  }

function onServerStateChange(topic, message) {
  // TODO: Update UI with new state information
  }

function pageSetup() {
  // What we need to do:
  //   Authenticate
  //   Get the server state (all actions, action types, etc)
  //   Register for events giving changes to the state
  //   Register for errors and warnings
  //   Display the home page
  var callCount = 3;
  var onComplete = function(status, result) {
    if(status) {
      callCount = callCount - 1;
      if (callCount == 0)
        setActivePage("home");
      }
    else {
      if (!$("#splash-error").is(":visible")) {
        startupError(result);
        }
      }
    }
  // First, try and authenticate
  password = $("#password").val();
  rpcCall(
    "Authenticate", { password: password },
    function(status, result) {
      if(!status) {
        // Authentication failed, show the login screen
        if(!$("#splash-login").is(":visible"))
          $("#splash-login").slideDown();
        if(password!=="")
          $("#login-message").text("Authentication failed. Please check your password.");
        }
      else {
        // Get the server state
        rpcCall("GetState", { },
          function(status, result) {
            onComplete(status, result);
            if(status)
              updateState(result);
            }
          );
        // Subscribe to state updates
        subscribe("private/server/state", onServerStateChange, onComplete);
        // Subscribe to notifications
        subscribe("private/server/notifications", onServerNotification, onComplete);
        }
      }
    );
  }

//---------------------------------------------------------------------------
// UI manipulation
//---------------------------------------------------------------------------

// Create a copy of the node with the given ID
//
// The copy will be given the ID 'newID' and any instances of ${oldID} in the
// content will be renamed as well.
//
// Strings with the format ${name} will be replaced with the value of 'name'
// in the 'vals' object.
function copyTemplate(oldID, newID, vals) {
  var copy = $("#" + oldID).clone();
  copy.attr("id", newID);
  var html = copy.html();
  // Replace variables
  foreach(vals, function(key, value) {
    var re = new RegExp("\\${" + key + "}", "g");
    html = html.replace(re, value);
    });
  // Also replace the ID
  var re = new RegExp("\\${" + oldID + "}", "g");
  html = html.replace(re, newID);
  // Update the copy and return it
  copy.html(html);
  return copy;
  }

// Activate the selected page if it is not already active
function setActivePage(target) {
  // Are we already active?
  if(activePage==target)
    return;
  // The transitioning function
  doTransition = function(target) {
    activePage = target;
    button = $("#" + activePage + "_button");
    if(button)
      button.addClass("active");
    $("#" + activePage).fadeIn("fast");
    }
  // Switch the page
  if(activePage!=="") {
    button = $("#" + activePage + "_button");
    if(button)
      button.removeClass("active");
    $("#" + activePage).fadeOut("fast", function() { doTransition(target) });
    }
  else
    doTransition(target);
  }
