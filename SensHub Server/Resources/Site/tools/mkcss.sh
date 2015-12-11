#!/bin/sh
#----------------------------------------------------------------------------
# Generate the Materialize CSS file from the SASS source.
#----------------------------------------------------------------------------
BASEDIR=`dirname $0`
BASEDIR=`readlink -f "${BASEDIR}"/..`

# Generate the CSS
cd "${BASEDIR}"/sass
sass materialize.scss "${BASEDIR}"/css/materialize.css
