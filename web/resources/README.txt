To add another localized resources.txt file: 

- Create a new text document with a name of the form resources.xxxx.txt, 
  where xxxx is <languagecode2>-<country/regioncode2>.  
  For example, use resources.en-us.txt to contain the resources for English US, 
  resources.en.txt to contain English resources. See table below for language codes.
  
- Use resources.custom.txt as a custom resources file.
  - Use resources.xxx.custom.txt for custom resource files per language.

- Final resources contents by first checking in custom resources, then in region 
  specific resources (e.g. en-us), then in language specific resources (e.g. en), 
  and finally the neutral resources file (i.e. resources.txt)

- Place one resource per line in the form name=value.
  IMPORTANT: make sure all values are terminated with a newline character, 
             including the last line!

- Use $1, $2, etc to specify string parameters.

- Use ';' on a separate line to include comments.


See the complete list of language codes at http://wiki.developer.mindtouch.com/MindTouch_Deki/Languages