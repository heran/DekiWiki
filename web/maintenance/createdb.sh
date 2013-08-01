#!/bin/bash

set -e 

# exit status codes
ERR_USAGE=1
ERR_MYSQL=2
ERR_ARGS=3
MYSQL=mysql

function usage {
        printf "Usage: %s: --dbName database_name --dbAdminUser database_admin_user --dbAdminPassword database_admin_password --dbServer database_server --dbWikiUser database_wiki_user --wikiAdmin admin_login --wikiAdminPassword admin_password --wikiAdminEmail admin_email [--maintenanceDir wiki_maintenance_directory]\n" $0
        echo
        echo "Parameters:"
        echo "--dbName:  Name of the database catalog to create."
		echo "--dbAdminUser: The database admin user used to create the database."
		echo "--dbAdminPassword: The password for the database admin user."
        echo "--dbServer: The MySQL host where the database will be created. "
       	echo "--dbWikiUser: The non-admin mysql account for the wiki database."
        echo "--wikiAdmin: The name of the Deki Wiki administrator account."
        echo "--wikiAdminPassword: The password for the Deki Wiki administrator account."
		echo "--wikiAdminEmail: The email address for the Deki Wiki administrator account."
	echo "--help: show usage."
        echo
		echo "OPTIONAL Parameters:"
		echo "--maintenanceDir: Deki Wiki maintenance directory"
		echo "--noStorage: do not specify storage settings"
		echo "--storageDir: The path to the file storage directory"
		echo "--s3PublicKey: S3 public key"
		echo "--s3PrivateKey: S3 private key"
		echo "--s3Bucket: S3 bucket"
		echo "--s3Prefix: prefix for all files stored on S3.  Allows multiple wikis to share the same bucket"
		echo "--s3Timeout: timeout for S3 requests"
		echo "--tableFile: file to create the database tables."
		echo "--dataFile: file to populate the database tables."
		
        exit ${ERR_USAGE}
}
function error {
		if [[ ! -z $2 ]]; then
			echo "Error: $2" 1>&2
		fi
		if [[ ! -z $1 ]]; then
			exit $1
		else
			exit $ERR_UNKNOWN 
		fi
}

function generateApiKey {
       matrix='0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ'
       length="32"

        while [ "${n:=1}" -le "$length" ]
        do
                apikey="$apikey${matrix:$(($RANDOM%${#matrix})):1}"
                let n+=1
        done
        echo $apikey
}

if [ -z $1 ]; then
        usage
fi

ARGS=`getopt --longoptions dbName:,dbAdminUser:,dbAdminPassword:,dbServer:,dbWikiUser:,wikiAdmin:,wikiAdminPassword:,wikiAdminEmail:,wikiApiKey:,maintenanceDir:,noStorage,storageDir:,s3PublicKey:,s3PrivateKey:,s3Bucket:,s3Prefix:,s3Timeout:,help,tableFile:,dataFile: \
        -- "$0" "$@"`

if [ $? != 0 ]; then usage; fi

eval set -- "$ARGS"

while true ; do
        case "$1" in
                --)
                        shift
                        break
                        ;;
                --dbName)
                        shift
                        db_name="$1"
                        shift
                        ;;
                --dbAdminUser)
                        shift
                        db_admin_user="$1"
                        shift
                        ;;
                --dbAdminPassword)
                        shift
                        db_admin_password="$1"
                        shift
                        ;;
                --dbServer)
                        shift
                        db_server="$1"
                        shift
                        ;;
                --dbWikiUser)
                        shift
                        db_wiki_user="$1"
                        shift
                        ;;
                --wikiAdmin)
						shift
                        wiki_admin="$1"
                        shift
                        ;;
                --wikiAdminPassword)
						shift
                        wiki_admin_password="$1"
                        shift
                        ;;
                --wikiAdminEmail)
						shift
                        wiki_admin_email="$1"
                        shift
                        ;;
				--wikiApiKey)
						shift
						wiki_api_key="$1"
						shift
						;;
				--maintenanceDir)
						shift
						maintenance_dir="$1"
						shift
						;;
				--noStorage)
						shift
						no_storage=1
						;;
				--storageDir)
						shift
						storage_dir="$1"
						shift
						;;
				--s3PublicKey)
						shift
						s3_public_key="$1"
						shift
						;;
				--s3PrivateKey)
						shift
						s3_private_key="$1"
						shift
						;;
				--s3Bucket)
						shift
						s3_bucket="$1"
						shift
						;;
				--s3Prefix)
						shift
						s3_prefix="$1"
						shift
						;;
				--s3Timeout)
						shift
						s3_timeout="$1"
						shift
						;;
				--tableFile)
						shift
						table_file="$1"
						shift
						;;
				--dataFile)
						shift
						data_file="$1"
						shift
						;;
                *|--help)
                        usage
                        exit
                        ;;
        esac
done;

# make sure we have all the required params
if [[ -z $db_name || -z $db_admin_user || -z $db_admin_password || -z $db_server || -z $db_wiki_user || \
	  -z $wiki_admin || -z $wiki_admin_password || -z $wiki_admin_email ]] ; then usage; fi;

# make sure we set defaults
if [[ -z $wiki_api_key ]]; then
	wiki_api_key=`generateApiKey`
fi
if [[ -z $maintenance_dir ]]; then
	maintenance_dir=`dirname $0`
fi

# determine where to store files
if [[ ! -z $storage_dir && -z $s3_public_key && -z $s3_private_key && -z $s3_bucket && -z $s3_prefix ]]; then
	use_s3=0
elif [[ -z $storage_dir && ! -z $s3_public_key && ! -z $s3_private_key && ! -z $s3_bucket && ! -z $s3_prefix ]]; then
	use_s3=1
elif [[ "$no_storage" != "1" ]]; then
	echo
	echo "Please specify --storageDir or s3 settings";
	usage

fi

if [ -z "$table_file" ]; then
	table_file="tables.sql"
fi
if [ -z "$data_file" ]; then
	data_file="data/source.sql"
fi

echo "Creating DB:  ${db_name}"

TIMESTAMP=`date -u +%Y%m%d%H%M%S` 
INVERSE_TIMESTAMP=$(echo ${TIMESTAMP} | perl -n -e "\$_ =~ tr/0123456789/9876543210/; print \$_;")

${MYSQL} -u ${db_admin_user} -p${db_admin_password} -h ${db_server} -e "CREATE DATABASE \`${db_name}\` DEFAULT CHARACTER SET utf8 DEFAULT COLLATE utf8_general_ci"

cd ${maintenance_dir}

# create database schema
${MYSQL} -u ${db_admin_user} -p${db_admin_password} -h ${db_server} ${db_name} < $table_file

# populate the database
${MYSQL} -u ${db_admin_user} -p${db_admin_password} -h ${db_server} ${db_name} < $data_file

# set the Admin/Sysop name, email, password
${MYSQL} -u ${db_admin_user} -p${db_admin_password} -h ${db_server} ${db_name} -e "UPDATE users set user_name='${wiki_admin}', user_email='${wiki_admin_email}', user_password=md5(concat('1-', md5('${wiki_admin_password}'))) where user_id=1"

# insert the Apikey into the config table
${MYSQL} -u ${db_admin_user} -p${db_admin_password} -h ${db_server} ${db_name} -e "INSERT INTO config (config_key, config_value) VALUES ('security/api-key', '${wiki_api_key}')"

if [ "$no_storage" != "1" ]; then
	if [ $use_s3 -eq 0 ]; then
		# insert the storage path into the config table
		${MYSQL} -u ${db_admin_user} -p${db_admin_password} -h ${db_server} ${db_name} -e "INSERT INTO config (config_key, config_value) VALUES ('storage/type','fs'); INSERT INTO config (config_key, config_value) VALUES ('storage/fs/path', '${storage_dir}');"
	elif [ $use_s3 -eq 1 ]; then
		${MYSQL} -u ${db_admin_user} -p${db_admin_password} -h ${db_server} ${db_name} -e "INSERT INTO config (config_key, config_value) VALUES ('storage/type','s3'); INSERT INTO config (config_key, config_value) VALUES ('storage/s3/publickey','${s3_public_key}'); INSERT INTO config (config_key, config_value) VALUES ('storage/s3/privatekey','${s3_private_key}'); INSERT INTO config (config_key, config_value) VALUES ('storage/s3/bucket','${s3_bucket}'); INSERT INTO config (config_key, config_value) VALUES ('storage/s3/prefix','${s3_prefix}');" 
		if [ ! -z $s3_timeout ]; then
			${MYSQL} -u ${db_admin_user} -p${db_admin_password} -h ${db_server} ${db_name} -e "INSERT INTO config (config_key, config_value) VALUES ('storage/s3/timeout','${s3_timeout}')"
		fi
	fi
fi

# grant permissions on the new db
${MYSQL} -u ${db_admin_user} -p${db_admin_password} -h ${db_server} -e "
    GRANT ALL ON \`${db_name}\`.* TO '${db_admin_user}'@'%'; 
    GRANT ALL ON \`${db_name}\`.* TO '${db_admin_user}'@localhost; 
    GRANT ALL ON \`${db_name}\`.* TO '${db_admin_user}'@localhost.localdomain; 
    GRANT ALL on \`${db_name}\`.* to ${db_wiki_user}@'%'; 
    GRANT ALL on \`${db_name}\`.* to ${db_wiki_user}@localhost; 
    GRANT ALL on \`${db_name}\`.* to ${db_wiki_user}@localhost.localdomain; 
    GRANT CREATE ROUTINE, ALTER ROUTINE ON \`${db_name}\`.* TO '${db_admin_user}'@'%'; 
    GRANT CREATE ROUTINE, ALTER ROUTINE ON \`${db_name}\`.* TO '${db_admin_user}'@localhost; 
    GRANT CREATE ROUTINE, ALTER ROUTINE ON \`${db_name}\`.* TO '${db_admin_user}'@localhost.localdomain; 
    GRANT DELETE,INSERT,SELECT,UPDATE,EXECUTE ON \`${db_name}\`.* TO '${db_wiki_user}'@'%'; 
    GRANT DELETE,INSERT,SELECT,UPDATE,EXECUTE ON \`${db_name}\`.* TO '${db_wiki_user}'@'localhost'; 
    GRANT DELETE,INSERT,SELECT,UPDATE,EXECUTE ON \`${db_name}\`.* TO '${db_wiki_user}'@'localhost.localdomain'; 
    GRANT SELECT ON mysql.proc TO '${db_wiki_user}'@'%'; 
    GRANT SELECT ON mysql.proc TO '${db_wiki_user}'@'localhost'; 
    GRANT SELECT ON mysql.proc TO '${db_wiki_user}'@'localhost.localdomain'; 
    "
	

