#!/bin/sh

# This script was designed to help developers set up a development environment
# using the MindTouch Deki Wiki Virtual Appliance for the PHP and database 
# layers while running the API in Microsoft Visual Studios. Doing so will allow 
# developers to test their development live against the latest source code from 
# MindTouch.

# Usage developerWiki.sh DREAM-IP-ADDRESS [MYSQL-ROOT-PASSWORD]

# http://developer.mindtouch.com/en/Contributing/Setting_up_an_API_development_environment

# Make sure we are running as root
if [ "$UID" != "0" ]; then
	echo "Sorry, must be root. Exiting..."
	exit 1
fi

if [ $# -lt 1 ] || [ $1 == "--help" ]; then
	echo "Usage: $0 DREAM-IP-ADDRESS [MYSQL-ROOT-PASSWORD]"
	echo 'Mysql Root password is assumed to me "password" unless provided' 
	exit 1;
fi

mysql_password="password"
if [ $# -eq 2 ]; then
	mysql_password=$2
fi

# Make sure LocalSettings.php exists
if [ ! -f /var/www/dekiwiki/LocalSettings.php ]; then
	echo "Please complete installation by going to http://VM-IPADDRESS/config/index.php."
	echo "Exiting."
	exit 1
fi

# Checkout trunk

echo "Removing old directories ..."
if [ -d /var/www/dekiwiki-sf ]; then
	rm -r -f /var/www/dekiwiki-sf
fi
mv /var/www/dekiwiki /var/www/dekiwiki-sf

echo "Checking out trunk ..."
svn co https://svn.mindtouch.com/source/public/dekiwiki/trunk/web/ /var/www/dekiwiki
cp /var/www/dekiwiki-sf/LocalSettings.php /var/www/dekiwiki/
if [ -d /opt/deki ]; then
	if [ -d /opt/deki-sf ]; then
	   rm -r -f /opt/deki-sf
	fi
	mv /opt/deki /opt/deki-sf
fi
svn co https://svn.mindtouch.com/source/public/dekiwiki/trunk /opt/deki
chown -R www-data /var/www/dekiwiki 
chmod a+w /var/www/dekiwiki/config

updateWiki.sh

#### Edit config files ####
echo "Editing config files ..."

# LocalSettings.php
if [ -z "`cat /var/www/dekiwiki/LocalSettings.php | grep wgDreamServer`" ]; then
	echo '$wgDreamServer = "http://dream:8081";' >> /var/www/dekiwiki/LocalSettings.php
fi

# /etc/apache2/sites-available/dekiwiki
sed -i.old "s/localhost:8081/dream:8081/" /etc/apache2/sites-available/dekiwiki

# /etc/hosts
if [ -z "`cat /etc/hosts | grep dream`" ]; then
	echo "$1 dream" > /etc/hosts
else
	echo 'WARNING: "dream" entry already present in /etc/hosts: did not change it.'
fi

# /etc/mysql/my.cnf
sed -i.old "s/bind-address/#bind-address/" /etc/mysql/my.cnf

# /etc/ssh/sshd_config
sed -i.old "s/PermitRootLogin no/PermitRootLogin yes/" /etc/ssh/sshd_config

# set mysql permission
 mysql --user=root --password=$mysql_password --execute="GRANT ALL PRIVILEGES on *.* TO wikiuser@'%' IDENTIFIED BY 'password';FLUSH PRIVILEGES;"

# restart mysql
/etc/init.d/mysql restart

# restart ssh
/etc/init.d/ssh restart



