dp.sh.Brushes.Perl = function()
{
	var funcs = 'abs accept alarm atan2 bind binmode bless caller chdir chmod chomp chop chown chr chroot close closedir connect cos crypt dbmclose dbmopen defined delete dump each endgrent endhostent endnetent endprotoent endpwent endservent eof exec exists exp fcntl fileno flock fork format formline getc getgrent getgrgid getgrnam gethostbyaddr gethostbyname gethostent getlogin getnetbyaddr getnetbyname getnetent getpeername getpgrp getppid getpriority getprotobyname getprotobynumber getprotoent getpwent getpwnam getpwuid getservbyname getservbyport getservent getsockname getsockopt glob gmtime grep hex import index int ioctl join keys kill lc lcfirst length link listen localtime lock log lstat m map mkdir msgctl msgget msgrcv msgsnd no oct open opendir ord pack pipe pop pos print printf prototype push q qq quotemeta qw qx rand read readdir readline readlink readpipe recv ref rename reset reverse rewinddir rindex rmdir scalar seek seekdir semctl semget semop send setgrent sethostent setnetent setpgrp setpriority setprotoent setpwent setservent setsockopt shift shmctl shmget shmread shmwrite shutdown sin sleep socket socketpair sort splice split sprintf sqrt srand stat study sub substr symlink syscall sysopen sysread sysseek system syswrite tell telldir tie tied time times tr truncate uc ucfirst umask undef unlink unpack unshift untie utime values vec waitpid wantarray warn write qr';

	var keywords =	's select goto die do package redo require return continue for foreach last next wait while use if else elsif eval exit unless switch case';

	var declarations = 'my our local';

	this.regexList = [
		{ regex: dp.sh.RegexLib.SingleLinePerlComments, css: 'comment' },			// one line comments
		{ regex: dp.sh.RegexLib.DoubleQuotedString, css: 'string' },				// double quoted strings
		{ regex: dp.sh.RegexLib.SingleQuotedString, css: 'string' },				// single quoted strings
		{ regex: new RegExp('(\\$|@|%)\\w+', 'g'), css: 'vars' },				// variables
		{ regex: new RegExp(this.GetKeywords(funcs), 'gmi'), css: 'func' },			// functions
		{ regex: new RegExp(this.GetKeywords(keywords), 'gm'), css: 'keyword' },			// keyword
		{ regex: new RegExp(this.GetKeywords(declarations), 'gm'), css: 'declarations' }	// declarations
	];

	this.CssClass = 'dp-perl';
}

dp.sh.Brushes.Perl.prototype	= new dp.sh.Highlighter();
dp.sh.Brushes.Perl.Aliases	= ['perl'];
