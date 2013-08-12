include $(root_dir)/Makefile.include

ASSEMBLY_NAME=mindtouch.doc.util
ASSEMBLY=$(ASSEMBLY_NAME).dll

TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = 							\
		HtmlDocumentationBuilder.cs \
		HtmlXDocExtensions.cs

references = 																\
		-r:System															\
		-r:System.Core														\
		-r:System.Data														\
		-r:System.Xml														\
		-r:$(addprefix $(build_prefix)/, mindtouch.dream.dll)				\
		-r:$(addprefix $(build_prefix)/, mindtouch.reflection.dll)          \
		-r:$(addprefix $(root_dir)/, redist/log4net.dll)					\
		-r:$(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)				
	
$(addprefix $(build_prefix)/, mindtouch.dream.dll): $(addprefix $(root_dir)/, redist/mindtouch.dream.dll)
	cp $(addprefix $(root_dir)/, redist/mindtouch.dream.dll) $(addprefix $(build_prefix)/, mindtouch.dream.dll)

$(addprefix $(build_prefix)/, mindtouch.reflection.dll): $(addprefix $(root_dir)/, redist/mindtouch.reflection.dll)
	cp $(addprefix $(root_dir)/, redist/mindtouch.reflection.dll) $(addprefix $(build_prefix)/, mindtouch.reflection.dll)

$(addprefix $(build_prefix)/, SgmlReaderDll.dll): $(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)
	cp $(addprefix $(root_dir)/, redist/SgmlReaderDll.dll) $(addprefix $(build_prefix)/, SgmlReaderDll.dll)

$(TARGET): $(sources) $(addprefix $(build_prefix)/, mindtouch.dream.dll) $(addprefix $(build_prefix)/, SgmlReaderDll.dll) $(addprefix $(build_prefix)/, mindtouch.reflection.dll)
	$(MCS) $(MCS_FLAGS) /out:$(TARGET) /target:library $(references) $(sources)

all: $(TARGET)
