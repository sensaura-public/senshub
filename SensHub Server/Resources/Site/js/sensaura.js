//---------------------------------------------------------------------------
// Sensaura specific JS.
// NOTE: This should be minified before distribution
//---------------------------------------------------------------------------

//--- Globals
var activePage = "loader"

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
var serverOK = false;

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
  for (msg in response["messages"])
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
  serverOK = true;
  pageSetup()
  }

// Server initialisation
$(document).ready(
  function() {
    if ("WebSocket" in window) {
      var wsURL = "ws://" + location.hostname + (location.port ? ':' + location.port : ' ') + "/api/";
      var connection = new WebSocket(wsURL, [ 'rpc' ]);
      connection.onopen = function() {
        ws = connection;
        ws.onmessage = function(e) { recv(e.data); };
        $("#server-message").text("Connection established");
        $("#server-message").slideDown();
        }
      connection.onerror = function(error) {
        // Fall back to polling
        rpcCall("Poll", { }, serverConnected);
        }
      }
    else {
      // Fall back to polling
      rpcCall("Poll", { }, serverConnected);
      }
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

function getServerState() {
  // What we need to do:
  //   Get the server state (all actions, action types, etc)
  //   Register for events giving changes to the state
  //   Register for errors and warnings
  //   Display the home page
  var callCount = 3;
  var onSuccess = function(method, data) {
    callCount = callCount - 1;
    if (callCount == 0)
      setActivePage("home");
    }
  var onFailure = function(method, message) {
    if (!$("#splash-error").is(":visible")) {
      startupError(message);
      }
    }
  // Get our server state
  shAPI(
    "GetServerState",
    { },
    function(method, data) { onSuccess(method, data); updateState(data); },
    onFailure
    );
  // Subscribe to state updates
  subscribe(
    "private/server/state",
    onServerStateChange,
    onSuccess,
    onFailure
    );
  // Subscribe to notifications
  subscribe(
    "private/server/notifications",
    onServerNotification,
    onSuccess,
    onFailure
    );
  }

function serverInit(password) {
  shAPI(
    "Authenticate",
    {
      password: password
    },
    function(method, result) {
      if(!result) {
        $("#splash-login").slideDown();
        if(password!=="")
          $("#login-message").text("Authentication failed. Please check your password.");
        }
      else
        getServerState();
      },
    function(method, message) {
      startupError(message);
      }
    );
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
// Initial page setup
//---------------------------------------------------------------------------

function updateState(state) {
  // TODO: Update the UI with the state information
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
  password = ${"#password"}.val();
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
        rpcCall("GetServerState", { },
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
  for (var key in vals) {
    if (vals.hasOwnProperty(key)) {
      var re = new RegExp("\\${" + key + "}", "g");
      html = html.replace(re, vals[key]);
      }
    }
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
