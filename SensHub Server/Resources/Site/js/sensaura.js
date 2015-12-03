var activePage = "loader"

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