The templates in this directory can be used as resource strings, similar to the parent resources folder.

A file named "email.html" can be customized/overridden by creating "email.html.custom". Parameters are replaced with $1, $2, and so on -- just like a regular resource string.

They are invoked using wfMsgFromTemplate('email.html', $param1, $param2...);

