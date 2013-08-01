include $(root_dir)/Makefile.include

ASSEMBLY_NAME=mindtouch.mediawiki
ASSEMBLY=$(ASSEMBLY_NAME).dll

TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = 																	\
		MediaWikiService.cs													\
		AssemblyInfo.cs

references = 																\
		-r:System															\
		-r:System.Data														\
		-r:System.Xml														\
		-r:System.Web														\
		-r:$(addprefix $(root_dir)/, redist/mindtouch.dream.dll)							\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.script.dll)							\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.ext.dll)							\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.data.dll)							\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.util.dll)							\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.dll)								\
		-resource:'mediawiki-logo.png,MindTouch.MediaWiki.mediawiki-logo.png'				\
		-resource:'../../../web/resources/namespaces.txt,MindTouch.MediaWiki.namespaces.txt'

$(addprefix $(build_prefix)/, mindtouch.dream.dll): $(addprefix $(root_dir)/, redist/mindtouch.dream.dll)
	cp $(addprefix $(root_dir)/, redist/mindtouch.dream.dll) $(addprefix $(build_prefix)/, mindtouch.dream.dll)

$(TARGET): $(sources) $(addprefix $(build_prefix)/, mindtouch.dream.dll) $(addprefix $(build_prefix)/, mindtouch.deki.script.dll) $(addprefix $(build_prefix)/, mindtouch.deki.ext.dll) $(addprefix $(build_prefix)/, mindtouch.deki.data.dll) $(addprefix $(build_prefix)/, mindtouch.deki.dll) ../../../web/resources/namespaces.txt
	$(MCS) $(MCS_FLAGS) /out:$(TARGET) /target:library $(references) $(sources)

all: $(TARGET)

        
