##!/bin/bash
# script to apply patches to the VM or AMI

# VARIABLES
SRC_BASE=/opt/deki

do_apache_proxy_rewrite_update() {

# Paths are different between Linux flavors
    if [ "$DISTRIB" == "redhat" ]; then
		DW_HTTPD_CNF="/etc/httpd/conf.d/deki-apache.conf"
		HTTPD_INIT="/etc/init.d/httpd"
    elif [ "$DISTRIB" == "opensuse" ]; then
		DW_HTTPD_CNF="/etc/apache2/conf.d/deki-apache.conf"
		HTTPD_INIT="/etc/init.d/apache2"
    elif ( [ "$DISTRIB" == "debian" ] || [ -z "$DISTRIB" ] ); then
		if [ -f /etc/apache2/sites-available/deki ]; then
			rm -f /etc/apache2/sites-enabled/deki /etc/apache2/sites-enabled/001-deki
			mv /etc/apache2/sites-available/deki /etc/apache2/sites-available/dekiwiki
			ln -s /etc/apache2/sites-available/dekiwiki /etc/apache2/sites-enabled/001-dekiwiki
		fi
		DW_HTTPD_CNF="/etc/apache2/sites-available/dekiwiki"
		HTTPD_INIT="/etc/init.d/apache2"
    else
		echo "ERROR: unsupported Linux flavor." >>$MyTTY
		echo "ERROR: unsupported Linux flavor." 
		exit 1
    fi

# Rewrite update
    MATCH=$(grep 'RewriteCond %{REQUEST_URI} !^/(@api|editor|skins|config|@gui)/' $DW_HTTPD_CNF |wc -l)
    if [ $MATCH == 0 ];
    then
	    echo "-- patching $DW_HTTPD_CNF --"
	    cp $DW_HTTPD_CNF $DW_HTTPD_CNF.old
    	sed -i -e '
    	/RewriteRule \^\/\$ \/index\.php?title= \[L,NE\]/ a\
    	\
    RewriteCond %{REQUEST_URI} ^/@gui/[^.]+$\
    RewriteRule ^/@gui/(.*)$ /proxy.php?path=$1 [L,QSA,NE] 
    	'\
    	-e 's:RewriteCond %{REQUEST_URI} !^/(@api|editor|skins|config)/:RewriteCond %{REQUEST_URI} !^/(@api|editor|skins|config|@gui)/:' $DW_HTTPD_CNF

# Reload Apache configuration
        $HTTPD_INIT reload
    fi
}

do_install_checkdeki_cron_job() {
    if [[ -f /usr/bin/checkdeki && ! -L /usr/bin/checkdeki ]]; then
        # remove it and create a symlink instead
        rm /usr/bin/checkdeki
    fi
    if [ ! -L /usr/bin/checkdeki ]; then
        echo "creating checkdeki symlink" 
        ln -s $SRC_BASE/src/scripts/checkdeki /usr/bin/checkdeki
    fi
    # add the cron job if it's not already there
    grep "/usr/bin/checkdeki" /etc/crontab  > /dev/null
    if [ $? != 0 ]; then
        echo "adding checkdeki cron job to /etc/crontab"
        echo "*/5 *   * * *   root    /usr/bin/checkdeki" >> /etc/crontab
    fi
    # prevent emails from cron
    grep -q "^MAILTO" /etc/crontab
    [ $? -ne 0 ] && sed -i -e "/^PATH/p" -e "s/^PATH.*$/MAILTO=\"\"/" /etc/crontab
}

do_dekiwiki_init_script_symlink() {
    if [ ! -f /etc/init.d/dekiwiki ]; then
        ln -s /etc/init.d/dekihost /etc/init.d/dekiwiki
    fi
}

do_apache_control_panel_rewrite_update() {
    MATCH=$(grep '/(@api|editor|skins|config|@gui|deki-cp)/' $DW_HTTPD_CNF |wc -l)
    if [ $MATCH == 0 ];
    then
	    echo "-- patching $DW_HTTPD_CNF (adding deki-cp exclusion)--"
    	sed -i.old -e 's:/(@api|editor|skins|config|@gui)/:/(@api|editor|skins|config|@gui|deki-cp)/:' $DW_HTTPD_CNF
	# Reload Apache configuration
        $HTTPD_INIT reload
    fi
}

# consolidate @gui and deki-cp into /deki/cp,/deki/gui, etc
do_apache_deki_rewrite_update() {
     #remove these lines
     #RewriteCond %{REQUEST_URI} ^/@gui/[^.]+$
     #RewriteRule ^/@gui/(.*)$ /proxy.php?path=$1 [L,QSA,NE]
     sed -i.old -e '/\^\/@gui/ d' $DW_HTTPD_CNF

     # change this line:
     # RewriteCond %{REQUEST_URI} !/(@api|editor|skins|config|@gui|deki-cp)/
     # to this:
     # RewriteCond %{REQUEST_URI} !/(@api|editor|skins|config|deki)/
     sed -i.old -e 's/@gui|deki-cp/deki/' $DW_HTTPD_CNF
     # Reload Apache configuration
     $HTTPD_INIT reload
}

# Bug #6251 make sure /etc/network/interfaces has auth eth0
do_ensure_auto_eth0() {
    if [ -f /etc/network/interfaces ]; then
        MATCH=$(grep 'auto eth0' /etc/network/interfaces|wc -l)
	if [ $MATCH == 0 ]; then
	    echo "-- patching /etc/network/interfaces (adding auto eth0)"
	    sed -i.old '/allow\-hotplug eth0/ i\
auto eth0
' /etc/network/interfaces
        fi
    fi
}

do_vm_updates() {
    do_apache_proxy_rewrite_update
    do_install_checkdeki_cron_job
    do_dekiwiki_init_script_symlink
    do_apache_control_panel_rewrite_update
    do_apache_deki_rewrite_update
    do_ensure_auto_eth0
}
