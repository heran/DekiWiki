include $(root_dir)/Makefile.include

ASSEMBLY_NAME=mindtouch.deki.ext
ASSEMBLY=$(ASSEMBLY_NAME).dll

TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = 							\
		AssemblyInfo.cs				\
		AuthenticationService.cs	\
		DekiExtFunctionDescriptor.cs\
		DekiExtService.cs

references = 																\
		-r:System															\
		-r:System.Data														\
		-r:System.Xml														\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.script.dll)			\
		-r:$(addprefix $(root_dir)/, redist/mindtouch.dream.dll)			\
		-r:$(addprefix $(root_dir)/, redist/log4net.dll)					\
		-r:$(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)

$(addprefix $(build_prefix)/, mindtouch.dream.dll): $(addprefix $(root_dir)/, redist/mindtouch.dream.dll)
	cp $(addprefix $(root_dir)/, redist/mindtouch.dream.dll) $(addprefix $(build_prefix)/, mindtouch.dream.dll)

$(addprefix $(build_prefix)/, SgmlReaderDll.dll): $(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)
	cp $(addprefix $(root_dir)/, redist/SgmlReaderDll.dll) $(addprefix $(build_prefix)/, SgmlReaderDll.dll)

$(addprefix $(build_prefix)/, log4net.dll): $(addprefix $(root_dir)/, redist/log4net.dll)
	cp $(addprefix $(root_dir)/, redist/log4net.dll) $(addprefix $(build_prefix)/, log4net.dll)

$(TARGET): $(sources) $(addprefix $(build_prefix)/, mindtouch.dream.dll) $(addprefix $(build_prefix)/, SgmlReaderDll.dll) $(addprefix $(build_prefix)/, log4net.dll) $(addprefix $(build_prefix)/, mindtouch.deki.script.dll)
	$(MCS) $(MCS_FLAGS) /out:$(TARGET) /target:library $(references) $(sources)

all: $(TARGET) 
