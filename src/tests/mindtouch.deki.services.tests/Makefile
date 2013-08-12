include $(root_dir)/Makefile.include
ASSEMBLY_NAME=mindtouch.deki.services.tests
ASSEMBLY=$(ASSEMBLY_NAME).dll
TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = \
                WebCacheTests.cs

references = \
                -r:System \
                -r:System.Data \
                -r:System.Xml \
                -r:System.Web \
                -r:System.Drawing \
                -r:System.Configuration \
                -r:$(addprefix $(build_prefix)/, mindtouch.deki.data.dll)		\
                -r:$(addprefix $(build_prefix)/, mindtouch.deki.dll)			\
                -r:$(addprefix $(build_prefix)/, mindtouch.deki.util.dll)		\
                -r:$(addprefix $(build_prefix)/, mindtouch.deki.script.dll)		\
                -r:$(addprefix $(build_prefix)/, mindtouch.deki.services.dll)	\
                -r:$(addprefix $(root_dir)/, redist/mindtouch.core.dll)         \
                -r:$(addprefix $(root_dir)/, redist/mindtouch.dream.dll)        \
                -r:$(addprefix $(root_dir)/, redist/mindtouch.dream.test.dll)   \
                -r:$(addprefix $(root_dir)/, redist/nunit/nunit.framework.dll)  \
        		-r:$(addprefix $(root_dir)/, redist/Autofac.dll)					\
                -r:$(addprefix $(root_dir)/, redist/log4net.dll)    \
                -r:$(addprefix $(root_dir)/, redist/Moq.dll)    \
                -r:$(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)  \
                -r:$(addprefix $(root_dir)/, redist/Lucene.Net.dll) 

$(TARGET): $(sources)
	$(MCS) $(MCS_FLAGS) /out:$(TARGET) /target:library $(references) $(sources) 

all: $(TARGET)
