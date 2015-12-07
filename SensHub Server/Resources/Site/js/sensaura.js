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

function internalDispatch(topic, message) {
  subs = [ ];
  len = topic.length
  for (candidate in subscriptions) {
    if (candidate.slice(0, len) == topic) {
      for (subscriber in subscriptions[candidate])
        subs.push(subscriber);
      }
    }
  // Now dispatch the method
  for (var i=0; i<subs.length; i++) {
    if (subs[i] in subscribers)
      subscribers[subs[i]][1](topic, message);
    }
  }

//---------------------------------------------------------------------------
// Base API interface
//---------------------------------------------------------------------------

function shApiFailure(method, onFailure) {
  onFailure(method, "Network error.");
  }

function shApiSuccess(method, data, onSuccess, onFailure) {
  if(data.failed) {
    onFailure(method, data.failureMessage);
    return;
    }
  // If we have any messages, process them
  for (var i=0; i<data.messages.length; i++)
    internalDispatch(data.messages[i].topic, data.messages[i].message);
  // Let the caller know it worked and pass the response back
  onSuccess(method, data.result);
  }

function shAPI(method, args, onSuccess, onFailure) {
  $.ajax({
    type: "POST",
    url: "/api/",
    data: $.toJSON({
      methodName: method,
      parameters: args,
      }),
    success: function(data) { shApiSuccess(method, data, onSuccess, onFailure); },
    error: function(xhr, message, exception) { shApiFailure(method, onFailure); },
    dataType: "json",
    contentType: "application/json"
    });
  }

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

function subscribe(topic, subscriber, onSuccess, onFailure) {
  // Assume it is going to work and get a reference
  ref = internalSubscribe(topic, subscriber);
  // Make the call
  shAPI(
    "Subscribe",
    {
      topic: topic
    },
    onSuccess,
    function(method, message) {
      internalUnsubscribe(ref);
      onFailure(method, message);
      }
    );
  }

function unsubscribe(ref) {
  topic = internalUnsubscribe(ref);
  if (topic != null) {
    // No more subscribers, tell the server to stop sending messages
    shAPI(
      "Unsubscribe",
      {
        topic: topic
      },
      function(method, result) {
        // Do nothing
        },
      function(method, message) {
        // Do nothing
        }
      );
    }
  }

$(document).ready(function () {
  // Try anonymous connection first
  serverInit("");
  });

//---------------------------------------------------------------------------
// UI manipulation
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

function doTransition(target) {
  activePage = target;
  button = $("#" + activePage + "_button");
  if(button)
    button.addClass("active");
  $("#" + activePage).fadeIn("fast");
  }

function setActivePage(target) {
  // Are we already active?
  if(activePage==target)
    return;
  if(activePage!=="") {
    button = $("#" + activePage + "_button");
    if(button)
      button.removeClass("active");
    $("#" + activePage).fadeOut("fast", function() { doTransition(target) });
    }
  else
    doTransition(target);
  }
