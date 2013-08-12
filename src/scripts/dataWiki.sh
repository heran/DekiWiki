#!/bin/bash

# dataWiki.sh v1.0 - 02/2008 MindTouch
# Backup / restore of MindTouch Data
# Dekiwiki database, /etc/dekiwiki, $DEKIROOT/LocalSettings.php, $DEKIROOT/AdminSettings.php, 
# $DEKIROOT/bin/mindtouch.host.sh, $DEKIROOT/attachments/

#
# VARIABLES
#

# File used for paths and credentials
DEKICNF="/etc/dekiwiki/mindtouch.deki.startup.xml"
# Backup dir name
BKPDIR="/tmp/backupWiki"

#
# FUNCTIONS
#

function show_usage () {
	echo "USAGE: $0 [-b ] | [-r backup.tar.gz]" >>$MyTTY
	exit 1
}

function abort () {
	echo "ERROR: $1 - aborting" >>$MyTTY
	rm -rf ${BKPDIR}
	start_services
	exit 1
}

function stop_services () {
	echo "-- stopping services --" >>$MyTTY
	/etc/init.d/dekiwiki stop
	[ -f /etc/init.d/httpd ] && /etc/init.d/httpd stop
	[ -f /etc/init.d/apache2 ] && /etc/init.d/apache2 stop
}

function start_services () {
	echo "-- starting services --" >>$MyTTY
	[ -f /etc/init.d/httpd ] && /etc/init.d/httpd start
	[ -f /etc/init.d/apache2 ] && /etc/init.d/apache2 start
	/etc/init.d/dekiwiki start
}

function dekibackup () {

	# stop Apache et dekihost
	stop_services

	# Check if the configuration file exists
	[ ! -f ${DEKICNF} ] && abort "${DEKICNF} does not exist"

	# Removing trailing characters if needed
	sed -i -e 's/$//' ${DEKICNF}

	# Get the dekiwiki parameters
	DEKIROOT=`grep "</deki-path>" ${DEKICNF} | sed -e 's/<deki-path>//g' -e 's/<\/deki-path>//g' -e 's/^[ \t]*//'`
	DBSERVER=`grep "</db-server>" ${DEKICNF} | sed -e 's/<db-server>//g' -e 's/<\/db-server>//g' -e 's/^[ \t]*//'`
	DBPORT=`grep "</db-port>" ${DEKICNF} | sed -e 's/<db-port>//g' -e 's/<\/db-port>//g' -e 's/^[ \t]*//'`
	DBNAME=`grep "</db-catalog>" ${DEKICNF} | sed -e 's/<db-catalog>//g' -e 's/<\/db-catalog>//g' -e 's/^[ \t]*//'`
	DBUSER=`grep "</db-user>" ${DEKICNF} | sed -e 's/<db-user>//g' -e 's/<\/db-user>//g' -e 's/^[ \t]*//'`
	DBPASSWD=`grep "</db-password>" ${DEKICNF} | cut -d">" -f 2 | cut -d"<" -f 1`

	# Getting release information
	DEKIRELEASE=`grep "^.wgProductVersion" ${DEKIROOT}/includes/DefaultSettings.php | awk -F"\'" '{print $2}'`

	if [ -f ${DEKIROOT}/LocalSettings.php ]; then
		DBADMUSER=`grep "^.wgDBadminuser" ${DEKIROOT}/LocalSettings.php  | awk -F"\"" '{ print $2 }'`
		DBADMPASSWD=`grep "^.wgDBadminpassword" ${DEKIROOT}/LocalSettings.php  | awk -F"\"" '{ print $2 }'`
	else
		DBADMUSER=${DBUSER}
		DBADMPASSWD=${DBPASSWD}
	fi

	# Check if the document root exists
	[ ! -d ${DEKIROOT} ] && abort "${DEKIROOT} does not exist"

	# Creating the backup directory
	mkdir -p ${BKPDIR}

	# Dumping database
	echo "-- saving database --" >> $MyTTY
 	mysqldump -u ${DBUSER} -h ${DBSERVER} -P ${DBPORT} -p"${DBPASSWD}" ${DBNAME} > ${BKPDIR}/wikidb.sql
	[ $? -ne 0 ] && abort "mysqldump failed"

	# Saving license information if any
	echo "-- saving license information --" >> $MyTTY
	[ -f ${DEKIROOT}/config/enterprise.php ] && tar cpf ${BKPDIR}/deki-license.tar ${DEKIROOT}/config/enterprise.php
	#[ -d ${DEKIROOT}/bin/_x002F_deki ] && tar rpf ${BKPDIR}/deki-license.tar ${DEKIROOT}/bin/_x002F_deki
	tar rpf ${BKPDIR}/deki-license.tar ${DEKIROOT}/bin/_x002F_deki* ${DEKIROOT}/bin/storage.state.xml
	
	# Backup MindTouch configuration
	echo "-- saving configuration --" >> $MyTTY
	[ ! -d /etc/dekiwiki ] && abort "/etc/dekiwiki doesn't exist"
	cp -rp /etc/dekiwiki ${BKPDIR}/
	[ -f ${DEKIROOT}/LocalSettings.php ] && cp -rp ${DEKIROOT}/LocalSettings.php ${BKPDIR}/
	[ -f ${DEKIROOT}/bin/mindtouch.host.sh ] && cp -rp ${DEKIROOT}/bin/mindtouch.host.sh ${BKPDIR}/
	[ -f ${DEKIROOT}/AdminSettings.php ] && cp -rp ${DEKIROOT}/AdminSettings.php ${BKPDIR}/
	[ -d ${DEKIROOT}/attachments/ ] && cp -rp ${DEKIROOT}/attachments ${BKPDIR}/

	# Create a parameters file for restore
	echo -e "DEKIROOT=${DEKIROOT}\nDBSERVER=${DBSERVER}\nDBPORT=${DBPORT}\nDBNAME=${DBNAME}\nDBUSER=${DBUSER}\nDBPASSWD=${DBPASSWD}\nDBADMUSER=${DBADMUSER}\nDBADMPASSWD=${DBADMPASSWD}\nDEKIRELEASE=${DEKIRELEASE}" > ${BKPDIR}/config.sh

	# Create backup tarball
	BKPFILE="${BKPDIR}-${DEKIRELEASE:=?}-`date "+%F"`.tar.gz"
	cd ${BKPDIR}
	echo "-- creating ${BKPFILE} --" >>$MyTTY
	tar czpf ${BKPFILE} ./*
	[ $? -ne 0 ] && abort "tarball creation failed"
	cd ${OLDPWD}
	rm -rf ${BKPDIR}

	echo "-- backup completed --" >>$MyTTY
	echo "don't forget to copy ${BKPFILE} to a safe place" >>$MyTTY

	# start Apache et dekihost
	start_services

	exit 0
}

function dekirestore () {

	# stop Apache et dekihost
	stop_services

	[ ! -d ${BKPDIR} ] && mkdir -p ${BKPDIR}
	# uncompress the backup file
	tar xzpf $1 -C ${BKPDIR}
	[ $? -ne 0 ] && abort "unable to untar $1"
	source ${BKPDIR}/config.sh

	[ ! -d ${DEKIROOT} ] && abort "directory ${DEKIROOT} doesn't exist"

	# Getting release information
	DEKIRESTORERELEASE=`grep "^.wgProductVersion" ${DEKIROOT}/includes/DefaultSettings.php | awk -F"\'" '{print $2}'`

	if [ "${DEKIRESTORERELEASE}" != "${DEKIRELEASE:=?}" ]; then
		abort "This backup is intended for MindTouch ${DEKIRELEASE}, installed release is ${DEKIRESTORERELEASE}"
	fi

	# Get database credentials
	DB_ADM_USER="root"
	DB_ADM_PWD=""
	mysql -u ${DB_ADM_USER} ${DB_ADM_PWD} -e "show databases" >>/dev/null
	if [ $? -eq 0 ]; then
		echo "WARN: using empty root MySQL password" >>$MyTTY
	else
		DB_ADM_PWD="-p`hostid`"
		while [ true ]; do 
			mysql -u ${DB_ADM_USER} ${DB_ADM_PWD} -e "show databases" >>/dev/null
			[ $? -eq 0 ] && break
			echo "Failed to connect to MySQL database" >> $MyTTY
			echo -e "MySQL admin login : \c" >> $MyTTY
			read DB_ADM_USER
			echo -e "MySQL admin password : \c" >> $MyTTY
			stty -echo
			read DB_ADM_PWD
			stty echo
			echo
			if [ ! -z "${DB_ADM_PWD}" ]; then
				DB_ADM_PWD="-p${DB_ADM_PWD}"
			else
				DB_ADM_PWD=""
			fi
		done
	fi

	# cleaning database if needed
	mysql -u ${DB_ADM_USER} ${DB_ADM_PWD} -e "drop database ${DBNAME}"
	mysql -u ${DB_ADM_USER} ${DB_ADM_PWD} -e "create database ${DBNAME}"
	[ $? -ne 0 ] && abort "database creation failed"

	# creating database users
	USR_SQL_FILE="${DEKIROOT}/maintenance/users.sql"
	[ ! -f ${USR_SQL_FILE} ] && abort "cannot open ${USR_SQL_FILE}"
	sed -e 's/{$wgDBuser}/'"${DBUSER}"'/g' -e 's/{$wgDBname}/'"${DBNAME}"'/g' -e 's/{$wgDBpassword}/'"${DBPASSWD}"'/g' ${USR_SQL_FILE} | mysql -u ${DB_ADM_USER} ${DB_ADM_PWD}
	[ $? -ne 0 ] && abort "database user creation failed"

	mysql -u ${DB_ADM_USER} ${DB_ADM_PWD} -e "flush privileges"
	[ $? -ne 0 ] && abort "flush privileges failed"

	# Creating MySQL functions
	for SPROCFILE in ${DEKIROOT}/maintenance/archives/sprocs-*.sql 
	do
		sed -e 's/{$wgDBuser}/'"${DBUSER}"'/g' -e 's/{$wgDBname}/'"${DBNAME}"'/g' -e 's/{$wgDBpassword}/'"${DBPASSWD}"'/g' -e 's/{$wgDBadminuser}/'"${DBADMUSER}"'/g' -e 's/{$wgDBadminpassword}/'"${DBADMPASSWD}"'/g' ${SPROCFILE} | mysql -u ${DB_ADM_USER} ${DB_ADM_PWD} ${DBNAME}
		[ $? -ne 0 ] && abort "Problem creating database procedures"
	done

	for FUNCFILE in ${DEKIROOT}/maintenance/archives/funcs-*.sql
	do
		sed -e 's/{$wgDBuser}/'"${DBUSER}"'/g' -e 's/{$wgDBname}/'"${DBNAME}"'/g' -e 's/{$wgDBpassword}/'"${DBPASSWD}"'/g' -e 's/{$wgDBadminuser}/'"${DBADMUSER}"'/g' -e 's/{$wgDBadminpassword}/'"${DBADMPASSWD}"'/g' ${FUNCFILE} | mysql -u ${DB_ADM_USER} ${DB_ADM_PWD} ${DBNAME}
		[ $? -ne 0 ] && abort "Problem creating database functions"
	done
	
	# restoring MindTouch database
	echo "-- restoring MindTouch database --" >> $MyTTY
	mysql -u ${DBUSER} -h ${DBSERVER} -P ${DBPORT} -p"${DBPASSWD}" ${DBNAME} < ${BKPDIR}/wikidb.sql
        [ $? -ne 0 ] && abort "database restoration failed"

	# Restoring license information if any
	echo "-- restoring license information --" >> $MyTTY
	[ -f ${BKPDIR}/deki-license.tar ] && tar xpf ${BKPDIR}/deki-license.tar -C /
	
	# restoring MindTouch files
	echo "-- restoring MindTouch configuration --" >> $MyTTY
        [ -d /etc/dekiwiki ] && mv /etc/dekiwiki /etc/dekiwiki.old
	cp -rp ${BKPDIR}/dekiwiki /etc/
	[ -f ${BKPDIR}/LocalSettings.php ] && cp -rp ${BKPDIR}/LocalSettings.php ${DEKIROOT}/
	[ -f ${BKPDIR}/AdminSettings.php ] && cp -rp ${BKPDIR}/AdminSettings.php ${DEKIROOT}/
	[ -f ${BKPDIR}/mindtouch.host.sh ] && cp -rp ${BKPDIR}/mindtouch.host.sh ${DEKIROOT}/bin/

        if [ -d ${DEKIROOT}/attachments/ ]; then
		mv ${DEKIROOT}/attachments ${DEKIROOT}/attachments.old
	fi
	cp -rp ${BKPDIR}/attachments ${DEKIROOT}/

	rm -rf ${BKPDIR}

	echo "-- restore completed --" >>$MyTTY

	# start Apache et dekihost
	start_services

	exit 0
}

#
# MAIN
#

# root check
if [ "$(id -u)" != "0" ]; then
	echo "ERROR: This script should run as superuser"
	exit 1
fi

# Output and log settings
exec >/var/log/dekiwiki/dataWiki`date "+%F-%T"` 2>&1
set -x
MyTTY=`tty`

# Parsing options
while getopts "br" options; do
        case $options in 
                b)
                        echo "-- Dekiwiki data backup --" >>$MyTTY
			dekibackup
                        ;;
                r)
			[ $# -ne 2 ] && abort "-r option needs an argument"
			[ ! -f $2 ] && abort "$2 is not a backup file"
                        echo "-- Dekiwiki data restore from $2 --" >>$MyTTY
                        dekirestore $2
                        ;;
#                *)
#			show_usage
#                        ;;
        esac
done

show_usage

#EOF
