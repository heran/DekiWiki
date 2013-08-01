#!/bin/bash

# this script uses xmltoddl (http://xml2ddl.berlios.de) to reverse engineer a db schema.
# To generate this script do the following
# 1) do a clean datbase install
# 2) install xml2ddl
#     wget http://download.berlios.de/xml2ddl/xml2ddl-0.3.1.zip
#     unzip xml2ddl-0.3.1.zip
#     cd xml2ddl-0.3.1
#     apt-get install python-setuptools python-mysqldb
#     python setup.py install
#     downloadXml -b mysql --host=localhost -d wikidb -u root -p password  > db-schema.xml

HOST=localhost
DB=wikidb
USER=root
PASS=password
OUTPUT=mindtouch.deki.checkdb.schema.xml
downloadXml -b mysql --host=$HOST -d $DB -u $USER -p $PASS > $OUTPUT

