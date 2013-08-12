#!/bin/sh

MONO="mono"
RESULTSPATH="nunit-results.xml"
NUNITCONSOLE="redist/nunit/nunit-console.exe"

chmod +x $NUNITCONSOLE

CMD="$MONO $NUNITCONSOLE -xml=$RESULTSPATH -nologo bin/mindtouch.deki.tests.dll"
echo Running nUnit Tests: $CMD
$CMD
exit $?
