README

1. What is Deki-Upload?
2. Installation & Usage
3. Notes
______________________________________________________________________________


                          .;ok0KXXXXX0dl,
                       .:xkd:,..  ..';okKKx,
                     .l0o.    ....       ,kW0:
                    .0x.  .cxkOOO0K0d,     ,0WO.
                   .0d  .xOl.     .,dXKl.   .xMX,
                   oK. ;Kl           .oNX,    xMX'
                   Ox .Kc              .ON,   .OM0.
                   0k c0                'N0.   ,NMo
                   dN':k            ,cldkNN,    xMX.
                   .K0,x'           dMO:,..     lMMc
                    .OXd:           .XO.   ,oxOXX0d'
                      :0Kd;.         oWk.  :WK:'.
                        .:dOK0kxxxkOOKNXc   kN,
                            'oOOxl;'..      ,Nk
                               .cdkOkxddddddkNW,
                                    .,:cloooool'

                                 .   .                                 .
                                ;X' ;X'                               .Kc
l;coo,.coo'  .l. .l,coo;   .:olcoN' ;Wxlc   ,looc.  .l.   ;,   ,lolc. .Kd:ooc.
MO,.oWk'.oX' ;W, ;Wk'.:Nc .0k..'ON' ;Nc..  x0,..oX, ,N;   Od  x0,..c; .K0;.,Kd
M:  ,W;  ;N, ;W, ;W;  .Kl ;W;   :N' ;N'   .Nl   .Kd ,N;   Od .Xl      .Kl   kk
M:  ,N;  ;N, ;W, ;N,  .Kl .0d. .kN' ,Nc    kO. .lX; .Xo..cNd  kO.  ;; .Kc   kk
M:  ,N;  ;N, ;W, ;N,  .Kl  'KKO0ON'  xN00: .xX0KK:   oNXXxKd  .kN0KK, .Kc   kk

______________________________________________________________________________


1. What is Deki-Upload?
   ----------------------
	Deki-upload.cmd is a batch file uploader for Windows, executable from the command line. 
	
2. Installation & Usage
   -----------------------
	Installation:
	   Unzip the entire ZIP archive to a permanent location (e.g. "C:\Program Files\MindTouchDekiUpload"). 
	   Add the unzipped folder location to your PATH variable (e.g. PATH=C:\Program Files\MindTouchDekiUpload;%PATH%). 
	
   	Usage:
		deki-upload [username:password] [file-pattern] [deki-page]

	Example:
		1) upload a file called "readme.txt"
		   deki-upload JohnSmith:MyPassword readme.txt http://dekipage.domain.com/User:JohnSmith

		2) upload multiple files ending in ".txt"
		   deki-upload JohnSmith:MyPassword *.txt http://dekipage.domain.com/User:JohnSmith

3. Notes
   -----------------------
   	1) 	When uploading, the target page will be created if it does not exist.
   
  	2) 	The included version of curl.exe does not support SSH. To get a version of curl that supports SSH 
  		please visit http://curl.haxx.se/download.html#Win32