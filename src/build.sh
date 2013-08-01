#!/bin/sh
if [ ! -z $1 ]; then
    make root_dir=`pwd` PREFIX=`pwd`/bin &> $1
else
    make root_dir=`pwd` PREFIX=`pwd`/bin
fi
exit $?
