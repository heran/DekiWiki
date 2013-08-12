#!/bin/bash

# Output and log settings
LOGFILE=/var/log/dekiwiki/updateWiki-`date "+%F-%H:%M"`
exec >${LOGFILE} 2>&1
set -x
MyTTY=`tty`

# Setting Variables
SVN_CMD="svn --non-interactive"
SRC_BASE=/opt/deki
BUILDLOG="/var/log/dekiwiki/buildlog"
BIN_URL="http://binaries.mindtouch.com/"
REQUIRED_MONO="2.10"
DOWNLOADBIN=0
RECOMPILE=0
VERBOSE=0

function show_verbose_output() {
if [ ${VERBOSE} -eq 1 ]; then
	echo -e "\n ---------------- START OF VERBOSE OUTPUT ---------------- \n" >>$MyTTY
	cat ${LOGFILE} >>$MyTTY
	echo -e "\n ---------------- END OF VERBOSE OUTPUT ---------------- \n" >>$MyTTY
fi
}

function check_for_conflicts() {
        ${SVN_CMD} st $1 | grep -e "^[C]"  >>$MyTTY
        if [ $? -eq 0 ]; then
                echo "there was a merge conflict (see above) that may cause issues with your upgrade" >>$MyTTY
                echo "press enter to continue or Ctrl-C to quit ..." >>$MyTTY
                read WAIT
        fi
}

function update_base() {
	echo "-- updating wiki at $BASE --" >>$MyTTY
	pushd $BASE > /dev/null
	svn cleanup
	cd bin
	rm *.dll *.exe *.pdb *.config *.mdb services/*.dll 2>&1 > /dev/null
	cd ..
	${SVN_CMD} up $REVISION
	if [ $? -ne 0 ]; then
		echo "WARNING : SVN returned an code $SVN_RETURN_CODE updating $BASE" >>$MyTTY
		echo "Check the log for details : $LOGFILE" >>$MyTTY
		echo "WARNING : SVN returned an code $SVN_RETURN_CODE updating $BASE" 
	fi
	${SVN_CMD} st | grep -e "^[M|C]"
	check_for_conflicts $PWD
	${SVN_CMD} info | egrep -e "(URL|Last Changed (Rev|Date))" >>$MyTTY
	popd > /dev/null
}

function display_mono_version_error() {
	echo >> $MyTTY
	echo "ERROR: MindTouch requires mono version 2.10.1 or higher" >> $MyTTY
	echo "If you do not have mono 2.10.1 or higher installed please see this guide:" >> $MyTTY
	echo "http://developer.mindtouch.com/en/docs/mindtouch_setup/010Installation/020Installing_on_Debian/Installing%2F%2FUpgrading_Mono_on_Debian" >> $MyTTY
	echo >> $MyTTY
}

# Parse options
while getopts "r:dvb:" options; do
 	case $options in 
		r)
			REVISION=$OPTARG
			;;
		d)	
			RECOMPILE=1
			;;
		v)	
			VERBOSE=1
			;;
		b)	
			DOWNLOADBIN=1
			BIN_BRANCH=`echo $OPTARG | tr ['A-Z'] ['a-z']`
			if [[ "$BIN_BRANCH" != "trunk" ]] && [[ "$BIN_BRANCH" != "10.1" ]] && [[ "$BIN_BRANCH" != "10.0" ]]; then
				echo "ERROR: Only trunk/10.1/10.0 binaries exist" >>$MyTTY
				echo "ERROR: Only trunk/10.1/10.0 binaries exist"
				show_verbose_output ; exit
			fi
			;;
		?)
			echo "Usage: updateWiki.sh [-r (revision)][-d][-v][-b (trunk/10.1/10.0)]" >>$MyTTY; exit
			;;
		*)
			;;
	esac
done

if [ -f /etc/redhat-release ]; then
	DISTRIB=redhat
	USER=apache:apache
elif ( [ -f /etc/debian_version ] || [ -f /etc/lsb-release ] ); then
	DISTRIB=debian
	USER=www-data:www-data
elif [ -f /etc/SuSE-release ]; then
	DISTRIB=opensuse
	USER=wwwrun:wwwrun
else
	echo "WARN: unsupported Linux flavor." >>$MyTTY
	echo "WARN: unsupported Linux flavor."
fi

if [ -d /var/www/deki-hayes ]; then
	BASE=/var/www/deki-hayes
elif [ -d /var/www/dekiwiki ]; then
	BASE=/var/www/dekiwiki
else
	echo "ERROR: Base directory does not exist." >>$MyTTY
	echo "ERROR: Base directory does not exist."
	show_verbose_output ; exit 1
fi

#BEGIN  Check Mono version
if [ -f /etc/dekiwiki/mindtouch.host.conf ]
then
	. /etc/dekiwiki/mindtouch.host.conf
else
	MONO=`which mono`
fi

if [ ! -z "$MONO" ]
then
	MONO_VERSION=`$MONO --version | head -1 | awk '{ print $5 }'`
  if [ `expr match "$MONO_VERSION" "$REQUIRED_MONO"` == 0 ]
	then
		display_mono_version_error; exit 1;
	fi
else
	echo "WARNING: could not determine installed version of Mono ( requires mono $REQUIRED_MONO or above )" >>$MyTTY
	echo "WARNING: could not determine installed version of Mono ( requires mono $REQUIRED_MONO or above )" 
fi
#END  Check Mono version

if ( [ $DOWNLOADBIN -eq 1 ] && [ $RECOMPILE -eq 1 ] )
then
	echo "ERROR: options \"-b\" and \"-d\" are not compatible" >>$MyTTY
	echo "ERROR: options \"-b\" and \"-d\" are not compatible"
	show_verbose_output ; exit 1
fi

if ( [ $DOWNLOADBIN -eq 1 ] && [ ! -z $REVISION ] )
then
	echo "ERROR: options \"-b\" and \"-r\" are not compatible" >>$MyTTY
	echo "ERROR: options \"-b\" and \"-r\" are not compatible"
	show_verbose_output ; exit 1
fi

if [ ! -z $REVISION ]; then
	expr match "$REVISION" "^[0-9]*$" >>/dev/null 2>&1
	if [ $? -eq 0 ]
        then
		REVISION="-r $REVISION";
	else
		echo "ERROR: $REVISION is not a valid revision number" >>$MyTTY
		echo "ERROR: $REVISION is not a valid revision number" 
		show_verbose_output ; exit 1
	fi
fi

# make sure to update ourself first
CURRVER=`${SVN_CMD} info $SRC_BASE/src/scripts | grep "^Revision:" | awk '{ print $NF }'`

${SVN_CMD} up $REVISION $SRC_BASE/src/scripts
if [ $? != 0 ]; then
    echo "ERROR: We were unable to connect to the update server.  No SVN updates have been applied." >>$MyTTY
    echo "ERROR: We were unable to connect to the update server.  No SVN updates have been applied." 
    #show_verbose_output ; exit
else
    check_for_conflicts $SRC_BASE/src/scripts
    NET_CNX="TRUE"
fi

# if script has been updated, restart it !
NEWVER=`${SVN_CMD} info $SRC_BASE/src/scripts | grep "^Revision:" | awk '{ print $NF }'`
if [ ${NEWVER} != ${CURRVER} ]; then
	echo "NOTICE: script $0 updated - restarting"
	echo "-- script $0 updated - restarting --" >>$MyTTY
	exec $0 $* >>${LOGFILE} 2>&1
	exit 0
fi

if [ $RECOMPILE -eq 1 ]
then
	cd $SRC_BASE/src
	if [ $NET_CNX == "TRUE" ] ; then
	  echo "-- updating sources --" >>$MyTTY
	  svn cleanup
	  ${SVN_CMD} $REVISION up 
	  ${SVN_CMD} st | grep -e "^[M|C]"
	  check_for_conflicts $PWD
	  ${SVN_CMD} info | egrep -e "(URL|Last Changed (Rev|Date))" >>$MyTTY
	fi
	chown -R $USER $SRC_BASE
	gmcs --version >>/dev/null 2>&1
	if [ $? != 0 ]; then
		echo "ERROR: can't find gmcs, gmcs is needed to build MindTouch" >>$MyTTY
		echo "ERROR: can't find gmcs, gmcs is needed to build MindTouch" 
		show_verbose_output ; exit
	fi
	echo "-- building MindTouch API --" >>$MyTTY
	BUILDFAILED=0
	./build.sh >$BUILDLOG 2>&1
	[ $? -ne 0 ] && BUILDFAILED=1
	cat $BUILDLOG >>$MyTTY 
	cat $BUILDLOG 
	rm -f $BUILDLOG
	if [ $BUILDFAILED -eq 1 ]; then
		echo "#####################################" >>$MyTTY
		echo "ERROR: build failed - nothing changed" >>$MyTTY
		echo "       Log file : $LOGFILE" >>$MyTTY
		echo "       please contact support@mindtouch.com" >>$MyTTY
		echo "#####################################" >>$MyTTY
		show_verbose_output ; exit 1
	fi

	[ $NET_CNX == "TRUE" ] && update_base
	cd $SRC_BASE/src/bin 
	cp -r *.dll *.exe *.config $BASE/bin/
else
	[ $NET_CNX == "TRUE" ] && update_base
fi

if ( [ $DOWNLOADBIN -eq 1 ] && [ $NET_CNX == "TRUE" ] )
then
	wget -q $BIN_URL$BIN_BRANCH/_FILELIST.TXT -O /tmp/.filelist.tmp >>/dev/null 2>&1
	if [ $? -ne 0 ]; then
		echo "ERROR: Failure while fetching file list $BIN_URL$BIN_BRANCH/_FILELIST.TXT" >>$MyTTY
		echo "ERROR: Failure while fetching file list $BIN_URL$BIN_BRANCH/_FILELIST.TXT" 
		show_verbose_output ; exit
	fi

	echo "-- Downloading MindTouch binaries from $BIN_URL$BIN_BRANCH" >>$MyTTY
	cd $BASE/bin/
	DOWNLOADFAILED=0
	for FILE in `cat /tmp/.filelist.tmp`
	do 
		echo -e "$FILE \c " >>$MyTTY
		wget -q $BIN_URL$BIN_BRANCH/$FILE -O $FILE
		if [ $? -eq 0 ]
		then 
			echo "OK" >>$MyTTY
		else
			echo "FAILED" >>$MyTTY
			DOWNLOADFAILED=1
		fi
	done

	#wget -e robots=off -nv -r -nd -np -N --no-cache --accept=dll,exe,config --reject=html $BIN_URL
	rm -f /tmp/.filelist.tmp

	if [ $DOWNLOADFAILED -eq 0 ]
	then
		echo "-- downloading complete" >>$MyTTY
	else
		echo "ERROR: download failed" >>$MyTTY
		echo "ERROR: download failed" 
		show_verbose_output ; exit
	fi
fi

echo "-- updating database --" >>$MyTTY
pushd $BASE/maintenance  > /dev/null
php update-db.php
if [ $? != 0 ]; then
	echo "ERROR: Database updates failed.  Please run update-db.php manually." >>$MyTTY
	echo "ERROR: Database updates failed.  Please run update-db.php manually."
	show_verbose_output;
fi  

popd > /dev/null

# cleaning skins cache
rm -r $BASE/skins/common/cache/*

# run VM updates
source $SRC_BASE/src/scripts/patchVM.sh
do_vm_updates

echo "-- restarting MindTouch --" >>$MyTTY
if [ -f /etc/init.d/dekiwiki ]; then
    /etc/init.d/dekiwiki restart
else
    /etc/init.d/dekihost restart
fi

chown -R $USER $BASE

echo "-- update complete --" >>$MyTTY

[ ${VERBOSE} -eq 1 ] && show_verbose_output

exit 0

#EOF
